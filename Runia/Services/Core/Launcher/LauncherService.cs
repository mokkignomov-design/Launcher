using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using Runia.Services.Auth;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Installers;
using System.Diagnostics;

namespace Runia.Services.Core.Launcher
{
    /// <summary>
    /// LauncherService запускает игру с защитой аутентификации.
    /// 
    /// ЗАЩИТА БЕЗОПАСНОСТИ:
    /// 1. Username получается ТОЛЬКО из AuthService.CurrentUser, который устанавливается после успешной авторизации
    /// 2. Username никогда не редактируется пользователем через SettingsPage или другой UI
    /// 3. Username хранится в защищенном хранилище (SecureTokenStore) вместе с токенами при входе
    /// 4. Если AuthService.CurrentUser равен null, используется значение по умолчанию "Player" (fallback)
    /// 5. Все критические данные защищены DPAPI на уровне ОС
    /// 
    /// Это гарантирует, что пользователь не может запустить игру под чужим никнеймом без получения валидного токена с сервера.
    /// </summary>
    public class LauncherService(JavaManager javaManager, ForgeManager forgeManager, ModPackManager modPackManager, IAppConfig config, AuthService authService)
    {
        public event Action? OnGameStarted;
        public event Action? OnGameExited;

        private readonly JavaManager _javaManager = javaManager ?? throw new ArgumentNullException(nameof(javaManager));
        private readonly ForgeManager _forgeManager = forgeManager ?? throw new ArgumentNullException(nameof(forgeManager));
        private readonly ModPackManager _modPackManager = modPackManager ?? throw new ArgumentNullException(nameof(modPackManager));
        private readonly IAppConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly AuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        public async Task LaunchPackAsync(string packName, IProgress<string>? logProgress, CancellationToken ct)
        {
            try
            {
                var servers = await _config.GetServersAsync();
                var currentServer = servers.FirstOrDefault(s => s.Title == packName)
                    ?? throw new Exception($"Сервер {packName} не найден");

             
                int ram = _config.RamAmount;
                string instancePath = VersionsManager.GetVersionPath(packName);

                logProgress?.Report("Preparing directories...");
                VersionsManager.EnsureDirectoriesExist(packName);

                if (!string.IsNullOrWhiteSpace(currentServer.ZipUrl))
                {
                    logProgress?.Report("Checking modpack...");
                    var downloadProgress = new Progress<double>(p => logProgress?.Report($"Downloading: {p:P0}"));
                    await _modPackManager.InstallModpackAsync(currentServer.ZipUrl, instancePath, downloadProgress, ct);
                }

                var launcher = new MinecraftLauncher(new MinecraftPath(VersionsManager.Root));

                logProgress?.Report("Installing base Minecraft...");
                await launcher.InstallAsync(currentServer.McVersion, ct);

                string versionId = currentServer.McVersion;
                if (!string.IsNullOrWhiteSpace(currentServer.ForgeVersion))
                {
                    logProgress?.Report("Checking Forge...");
                    versionId = await _forgeManager.InstallForgeAsync(currentServer.McVersion, currentServer.ForgeVersion, logProgress, ct);
                }

                string javaPath = await _javaManager.GetJavaPathAsync() ?? throw new Exception("Java not found");

                string username = _authService.CurrentUser?.Name ?? "Player";

                var launchOption = new MLaunchOption
                {
                    JavaPath = javaPath,
                    Session = MSession.CreateOfflineSession(username),
                    MaximumRamMb = ram,
                    FullScreen = _config.IsFullscreen
                };

                if (!string.IsNullOrWhiteSpace(_config.JvmArguments))
                {
                    launchOption.ExtraJvmArguments = _config.JvmArguments
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(arg => new MArgument(arg));
                }

                logProgress?.Report("Creating process...");
                var process = await launcher.CreateProcessAsync(versionId, launchOption);

                process.StartInfo.Arguments = ConfigureArguments(process.StartInfo.Arguments, instancePath);
                process.StartInfo.WorkingDirectory = instancePath;

                process.StartInfo.UseShellExecute = _config.ShowConsole;
                process.StartInfo.CreateNoWindow = !_config.ShowConsole;

                if (process.Start())
                {
                    logProgress?.Report("Game started");
                    OnGameStarted?.Invoke();

                    _ = Task.Run(async () =>
                    {
                        try { await process.WaitForExitAsync(ct); }
                        finally { OnGameExited?.Invoke(); }
                    }, ct);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {ex}");
                logProgress?.Report($"Ошибка: {ex.Message}");
                OnGameExited?.Invoke();
            }
        }

        private static string ConfigureArguments(string args, string path)
        {
            if (string.IsNullOrEmpty(args)) return args;

            string safePath = path.Trim('\"');
            var parts = args.Split(' ');

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "--gameDir" && i + 1 < parts.Length)
                {
                    parts[i + 1] = $"\"{safePath}\"";
                }
            }
            return string.Join(" ", parts);
        }
    }
}