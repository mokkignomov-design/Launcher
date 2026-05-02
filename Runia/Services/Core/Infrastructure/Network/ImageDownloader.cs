using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.Services.Core.Infrastructure.Network
{
    public interface IImageDownloader
    {
        /// <summary>
        /// Returns local path to cached image or null if failed.
        /// </summary>
        Task<string?> GetLocalPathAsync(string url, CancellationToken ct = default);
    }

    public sealed class ImageDownloader(IHttpClientFactory httpFactory, ILogger<ImageDownloader> logger) : IImageDownloader
    {
        private readonly IHttpClientFactory _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
        private readonly ILogger<ImageDownloader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Runia", "Cache", "Images");

        public async Task<string?> GetLocalPathAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            Directory.CreateDirectory(_cacheDir);
            var fileName = $"{GetMd5Hash(url)}{GetExtension(url)}";
            var localPath = Path.Combine(_cacheDir, fileName);

            if (File.Exists(localPath)) return localPath;

            try
            {
                using var client = _httpFactory.CreateClient("ImageClient");
                client.Timeout = TimeSpan.FromSeconds(30);
                var data = await client.GetByteArrayAsync(url, ct);
                await File.WriteAllBytesAsync(localPath, data, ct);
                return localPath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image: {Url}", url);
                return null;
            }
        }

        private static string GetMd5Hash(string input) =>
            Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input)));

        private static string GetExtension(string url) =>
            Path.GetExtension(url.Split('?')[0]) is { Length: > 0 } ext ? ext : ".png";
    }
}
