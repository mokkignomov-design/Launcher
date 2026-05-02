using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace Runia.Services.Installers
{
    public class JavaManager
    {
        private readonly string _installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Runia", "java");
        private static readonly HttpClient _httpClient = new();
        public async Task<string?> GetJavaPathAsync()
        {
            string? existing = FindJava();
            if (existing != null) return existing;

            return await InstallJavaAsync();
        }
        private string? FindJava()
        {
            if (!Directory.Exists(_installPath)) return null;
            return Directory.EnumerateFiles(_installPath, "javaw.exe", SearchOption.AllDirectories).FirstOrDefault();
        }
        private async Task<string?> InstallJavaAsync()
        {
            try
            {
                string zipPath = Path.Combine(Path.GetTempPath(), "jdk.zip");
                string url = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse";

                var data = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(zipPath, data);

                if (Directory.Exists(_installPath)) Directory.Delete(_installPath, true);
                ZipFile.ExtractToDirectory(zipPath, _installPath);
                File.Delete(zipPath);

                return FindJava();
            }
            catch { return null; }
        }
    }
}