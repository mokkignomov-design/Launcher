using Microsoft.Extensions.Logging;
using Runia.Domain.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.Services.Common
{
    public interface ISkinService
    {
        /// <summary>
        /// Загружает скин с бэка, кэширует его и возвращает локальный путь
        /// </summary>
        Task<string?> GetSkinAsync(string username, string skinUrl, CancellationToken ct = default);

        /// <summary>
        /// Возвращает путь к кэшированному скину если он существует
        /// </summary>
        Task<string?> GetCachedSkinAsync(string username);

        /// <summary>
        /// Запоминает последний выбранный скин
        /// </summary>
        Task SetLastSelectedSkinAsync(string username, string localPath);

        /// <summary>
        /// Возвращает последний выбранный скин
        /// </summary>
        Task<LastSelectedSkin?> GetLastSelectedSkinAsync();

        /// <summary>
        /// Очищает кэш скинов старше 30 дней
        /// </summary>
        Task CleanOldCacheAsync();
    }

    public class SkinService : ISkinService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<SkinService> _logger;
        private readonly string _skinCacheDir;
        private readonly string _lastSelectedPath;
        private readonly ConcurrentDictionary<string, string> _memoryCache = new();

        private const string LAST_SELECTED_FILE = "last_selected_skin.json";
        private const int CACHE_EXPIRY_DAYS = 30;

        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SkinService(IHttpClientFactory httpFactory, ILogger<SkinService> logger)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _skinCacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Runia", "Cache", "Skins");

            _lastSelectedPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Runia", "Cache");

            Directory.CreateDirectory(_skinCacheDir);
            Directory.CreateDirectory(_lastSelectedPath);
        }

        public async Task<string?> GetSkinAsync(string username, string skinUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(skinUrl))
                return null;

            // Проверяем память
            if (_memoryCache.TryGetValue(username, out var cachedPath) && File.Exists(cachedPath))
                return cachedPath;

            // Проверяем диск
            string fileName = $"{username}.png";
            string localPath = Path.Combine(_skinCacheDir, fileName);

            if (File.Exists(localPath))
            {
                _memoryCache.TryAdd(username, localPath);
                return localPath;
            }

            // Если это локальный файл (file:///)
            if (skinUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string sourceFile = new Uri(skinUrl).LocalPath;
                    if (File.Exists(sourceFile))
                    {
                        var data = await File.ReadAllBytesAsync(sourceFile, ct);
                        await File.WriteAllBytesAsync(localPath, data, ct);
                        _memoryCache.TryAdd(username, localPath);

                        _logger.LogInformation("Successfully cached local skin for {Username} from {SourceFile}", username, sourceFile);
                        return localPath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load local skin file for {Username}", username);
                    return null;
                }
            }

            // Скачиваем скин с URL
            try
            {
                _logger.LogInformation("Downloading skin for {Username} from {Url}", username, skinUrl);

                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                var data = await client.GetByteArrayAsync(skinUrl, ct);

                await File.WriteAllBytesAsync(localPath, data, ct);
                _memoryCache.TryAdd(username, localPath);

                _logger.LogInformation("Successfully cached skin for {Username} at {Path}", username, localPath);
                return localPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download skin for {Username}", username);
                return null;
            }
        }

        public async Task<string?> GetCachedSkinAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string fileName = $"{username}.png";
            string localPath = Path.Combine(_skinCacheDir, fileName);

            if (File.Exists(localPath))
            {
                _memoryCache.TryAdd(username, localPath);
                return localPath;
            }

            return await Task.FromResult<string?>(null);
        }

        public async Task SetLastSelectedSkinAsync(string username, string localPath)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(localPath))
                return;

            try
            {
                var lastSelected = new LastSelectedSkin
                {
                    Username = username,
                    LocalPath = localPath,
                    SelectedAt = DateTime.Now
                };

                string filePath = Path.Combine(_lastSelectedPath, LAST_SELECTED_FILE);
                string json = JsonSerializer.Serialize(lastSelected, s_jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogDebug("Saved last selected skin: {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save last selected skin");
            }
        }

        public async Task<LastSelectedSkin?> GetLastSelectedSkinAsync()
        {
            try
            {
                string filePath = Path.Combine(_lastSelectedPath, LAST_SELECTED_FILE);
                if (!File.Exists(filePath))
                    return null;

                string json = await File.ReadAllTextAsync(filePath);
                var lastSelected = JsonSerializer.Deserialize<LastSelectedSkin>(json, s_jsonOptions);

                if (lastSelected != null && File.Exists(lastSelected.LocalPath))
                {
                    _logger.LogDebug("Loaded last selected skin: {Username}", lastSelected.Username);
                    _memoryCache.TryAdd(lastSelected.Username, lastSelected.LocalPath);
                    return lastSelected;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load last selected skin");
                return null;
            }
        }

        public async Task CleanOldCacheAsync()
        {
            try
            {
                if (!Directory.Exists(_skinCacheDir))
                    return;

                var now = DateTime.Now;
                var files = Directory.GetFiles(_skinCacheDir);

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    if ((now - info.LastWriteTime).TotalDays > CACHE_EXPIRY_DAYS)
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.LogDebug("Deleted old cached skin: {File}", file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old cache file: {File}", file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }

            await Task.CompletedTask;
        }
    }
}
