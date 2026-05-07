using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using System.Net.Http;


namespace Runia.Services.Common
{
    /// <summary>
    /// Сервис для управления загрузкой и кэшированием изображений.
    /// </summary>
    public class ImageManager : IDisposable
    {
        private readonly string _cachePath;
        private readonly ConcurrentDictionary<string, BitmapImage> _memoryCache = new();
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<ImageManager> _logger;

        public ImageManager(IHttpClientFactory httpFactory, ILogger<ImageManager> logger)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Runia", "Cache", "Images");
            if (!Directory.Exists(_cachePath)) Directory.CreateDirectory(_cachePath);
        }

        public async Task<BitmapImage?> GetImageAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (_memoryCache.TryGetValue(url, out var cached)) return cached;

            string fileName = GetMd5Hash(url) + GetExtension(url);
            string localPath = Path.Combine(_cachePath, fileName);

            if (!File.Exists(localPath))
            {
                var ok = await DownloadImageAsync(url, localPath);
                if (!ok) return null;
            }

            var bitmap = LoadFromFile(localPath);
            if (bitmap != null)
            {
                bitmap.Freeze();
                _memoryCache.TryAdd(url, bitmap);
            }
            return bitmap;
        }

        private async Task<bool> DownloadImageAsync(string url, string localPath)
        {
            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var data = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(localPath, data);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image {Url}", url);
                return false;
            }
        }

        private static BitmapImage? LoadFromFile(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetMd5Hash(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        private static string GetExtension(string url)
        {
            try
            {
                string ext = Path.GetExtension(url.Split('?')[0]);
                return string.IsNullOrEmpty(ext) ? ".png" : ext;
            }
            catch
            {
                return ".png";
            }
        }

        public void Dispose()
        {
            _memoryCache.Clear();
            GC.SuppressFinalize(this);
        }
    }
}