using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Launcher;
using Runia.Services.Core.Infrastructure.Configuration;

namespace Runia.Services.Installers
{
    public class ForgeManager
    {
        private readonly JavaManager _javaManager;
        private readonly VersionsManager _versions;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<ForgeManager> _logger;
        private readonly IAppConfig _config;

        public ForgeManager(JavaManager javaManager, IAppConfig config, IHttpClientFactory httpFactory, ILogger<ForgeManager> logger)
        {
            _javaManager = javaManager ?? throw new ArgumentNullException(nameof(javaManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _versions = _config.Versions;
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> InstallForgeAsync(string baseVersion, string forgeVersion, IProgress<string>? log = null, CancellationToken ct = default)
        {
            string fullVersion = $"{baseVersion}-forge-{forgeVersion}";
            string rootPath = VersionsManager.Root;
            string forgePath = Path.Combine(rootPath, "versions", fullVersion);
            string jsonPath = Path.Combine(forgePath, $"{fullVersion}.json");

            if (Directory.Exists(forgePath) && File.Exists(jsonPath)) return fullVersion;

            string? javaPath = await _javaManager.GetJavaPathAsync() ?? throw new Exception("Java не найдена");
            string installerPath = Path.Combine(rootPath, $"forge_installer_{fullVersion}.jar");
            string forgeUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{baseVersion}-{forgeVersion}/forge-{baseVersion}-{forgeVersion}-installer.jar";

            log?.Report("[Forge] Загрузка инсталлятора...");
            try
            {
                var client = _httpFactory.CreateClient();
                var data = await client.GetByteArrayAsync(forgeUrl, ct);
                await File.WriteAllBytesAsync(installerPath, data, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download Forge installer");
                throw;
            }

            string dummyProfile = Path.Combine(rootPath, "launcher_profiles.json");
            if (!File.Exists(dummyProfile)) File.WriteAllText(dummyProfile, "{ \"profiles\": { } }");

            int ramMb = _config.RamAmount > 0 ? _config.RamAmount : 2048;

            ProcessStartInfo processInfo = new()
            {
                FileName = javaPath,
                Arguments = $"-Djava.net.preferIPv4Stack=true -Xmx{ramMb}M -jar \"{installerPath}\" --installClient",
                WorkingDirectory = rootPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            log?.Report("[Forge] Запуск процесса установки...");
            using var process = Process.Start(processInfo) ?? throw new Exception("Failed to start Forge installer process");

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await Task.WhenAll(outputTask, errorTask);

            string output = outputTask.Result;
            string errors = errorTask.Result;

            try
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "forge_install_log.txt");
                await File.WriteAllTextAsync(logFile, $"--- STDOUT ---\n{output}\n--- STDERR ---\n{errors}", ct);

                await process.WaitForExitAsync(ct);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Forge Install failed with code {Code}. Errors: {Errors}", process.ExitCode, errors);
                    throw new Exception($"Forge Error {process.ExitCode}. Подробности в forge_install_log.txt");
                }
            }
            finally
            {
                try { if (File.Exists(installerPath)) File.Delete(installerPath); } catch { }
            }

            return fullVersion;
        }
    }
}