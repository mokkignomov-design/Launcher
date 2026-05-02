using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Infrastructure.Configuration;

namespace Runia.Services.Installers
{
    public class ModPackManager(IHttpClientFactory httpFactory, ILogger<ModPackManager> logger, IAppConfig config)
    {
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly ILogger<ModPackManager> _logger = logger;
        private readonly IAppConfig _config = config;

        public async Task InstallModpackAsync(string url, string targetPath, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            string modsPath = Path.Combine(targetPath, "mods");
            if (Directory.Exists(modsPath) && Directory.EnumerateFiles(modsPath).Any())
            {
                _logger.LogInformation("Modpack already installed. Skipping.");
                return;
            }

            string zipPath = Path.Combine(Path.GetTempPath(), $"modpack_{Guid.NewGuid()}.zip");
            try
            {
                await DownloadFileAsync(url, zipPath, progress, ct);

                if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
                Directory.CreateDirectory(targetPath);

                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, targetPath), ct);

                FixDirectoryStructure(targetPath);
            }
            finally
            {
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath, IProgress<double>? progress, CancellationToken ct)
        {
            var client = _httpFactory.CreateClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            using var downloadStream = await response.Content.ReadAsStreamAsync(ct);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await downloadStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;
                if (totalBytes != -1)
                    progress?.Report((double)totalRead / totalBytes * 100);
            }
        }

        private void FixDirectoryStructure(string root)
        {
            try
            {
                var modsLocations = Directory.GetDirectories(root, "mods", SearchOption.AllDirectories);
                if (modsLocations.Length == 0) return;

                var realModsPath = modsLocations.OrderByDescending(d => d.Length).First();
                var expectedModsPath = Path.Combine(root, "mods");

                if (string.Equals(realModsPath, expectedModsPath, StringComparison.OrdinalIgnoreCase)) return;

                var parentDir = Path.GetDirectoryName(realModsPath);
                if (parentDir == null) return;

                foreach (var dir in Directory.GetDirectories(parentDir))
                {
                    var target = Path.Combine(root, Path.GetFileName(dir));
                    if (Directory.Exists(target)) Directory.Delete(target, true);
                    Directory.Move(dir, target);
                }

                foreach (var file in Directory.GetFiles(parentDir))
                {
                    var target = Path.Combine(root, Path.GetFileName(file));
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(file, target);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fix directory structure for {Root}", root);
            }
        }
    }
}