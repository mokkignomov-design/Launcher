using Microsoft.Extensions.Logging;
using Runia.Domain.Models;
using Runia.Services.Auth;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Network;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Runia.Services.Common
{
    public interface ISkinService
    {
        Task<string?> GetSkinAsync(string username, string skinUrl, CancellationToken ct = default);
        Task<string?> GetCachedSkinAsync(string username);
        Task SetLastSelectedSkinAsync(string username, string localPath);
        Task<LastSelectedSkin?> GetLastSelectedSkinAsync();
        Task CleanOldCacheAsync();
        Task<bool> UploadSkinAsync(string filePath, string username);
        Task<SkinInfoResponse?> GetSkinInfoAsync(string username);
    }

    public class SkinInfoResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;
    }

    public class SkinService : ISkinService
    {
        private readonly BaseApiService _api;
        private readonly IAppConfig _config;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<SkinService> _logger;
        private readonly ITokenStore _tokenStore;

        private readonly string _skinCacheDir;
        private readonly ConcurrentDictionary<string, string> _memoryCache = new();

        private const string LAST_SELECTED_FILE = "last_selected_skin.json";
        private const int CACHE_EXPIRY_DAYS = 30;

        public SkinService(
            BaseApiService api,
            IAppConfig config,
            IHttpClientFactory httpFactory,
            ILogger<SkinService> logger,
            ITokenStore tokenStore)
        {
            _api = api;
            _config = config;
            _httpFactory = httpFactory;
            _logger = logger;
            _tokenStore = tokenStore;

            _skinCacheDir = Path.Combine(_config.GameRootPath, "Cache", "Skins");
            Directory.CreateDirectory(_skinCacheDir);
        }

        public async Task<SkinInfoResponse?> GetSkinInfoAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var result = await _api.GetAsync<SkinInfoResponse>($"/api/v1/skins/info/{username}");

            if (result.IsSuccess && result.Data != null && !string.IsNullOrWhiteSpace(result.Data.Url))
            {
                return result.Data;
            }

            _logger.LogInformation("Skin info missing or API error for {Username}: {Error}. Returning null.", username, result.ErrorMessage);
            return null;
        }

        public async Task<bool> UploadSkinAsync(string filePath, string username)
        {
            try
            {
                var client = _httpFactory.CreateClient("SkinClient");

                using var content = new MultipartFormDataContent();
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                content.Add(new StringContent(username), "username");

                _logger.LogInformation("Uploading skin for {Username}...", username);

                var response = await client.PostAsync("api/v1/skins/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Skin uploaded successfully.");
                    return true;
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Skin upload failed: {StatusCode} - {Msg}", response.StatusCode, errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading skin for {Username}", username);
                return false;
            }
        }

        public async Task<string?> GetSkinAsync(string username, string skinUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(skinUrl)) return null;

            if (_memoryCache.TryGetValue(username, out var cachedPath) && File.Exists(cachedPath))
                return cachedPath;

            string localPath = Path.Combine(_skinCacheDir, $"{username}.png");

            if (File.Exists(localPath))
            {
                _memoryCache.TryAdd(username, localPath);
                return localPath;
            }

         
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
                        return localPath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy local skin file for {Username}", username);
                    return null;
                }
            }

            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                var data = await client.GetByteArrayAsync(skinUrl, ct);
                await File.WriteAllBytesAsync(localPath, data, ct);
                _memoryCache.TryAdd(username, localPath);
                return localPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download skin from {Url}", skinUrl);
                return null;
            }
        }

        public async Task<string?> GetCachedSkinAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            string localPath = Path.Combine(_skinCacheDir, $"{username}.png");

            if (File.Exists(localPath))
            {
                _memoryCache.TryAdd(username, localPath);
                return localPath;
            }
            return null;
        }

        public async Task SetLastSelectedSkinAsync(string username, string localPath)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(localPath)) return;
            try
            {
                var lastSelected = new LastSelectedSkin { Username = username, LocalPath = localPath, SelectedAt = DateTime.Now };
                string filePath = Path.Combine(_config.GameRootPath, "Cache", LAST_SELECTED_FILE);

                var json = JsonSerializer.Serialize(lastSelected, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save last selected skin for {Username}", username);
            }
        }

        public async Task<LastSelectedSkin?> GetLastSelectedSkinAsync()
        {
            try
            {
                string filePath = Path.Combine(_config.GameRootPath, "Cache", LAST_SELECTED_FILE);
                if (!File.Exists(filePath)) return null;

                var json = await File.ReadAllTextAsync(filePath);
                var lastSelected = JsonSerializer.Deserialize<LastSelectedSkin>(json);

                if (lastSelected != null && File.Exists(lastSelected.LocalPath))
                {
                    _memoryCache.TryAdd(lastSelected.Username, lastSelected.LocalPath);
                    return lastSelected;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load last selected skin info");
                return null;
            }
        }

        public async Task CleanOldCacheAsync()
        {
            try
            {
                if (!Directory.Exists(_skinCacheDir)) return;
                var now = DateTime.Now;
                foreach (var file in Directory.GetFiles(_skinCacheDir))
                {
                    if ((now - new FileInfo(file).LastWriteTime).TotalDays > CACHE_EXPIRY_DAYS)
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while cleaning skin cache");
            }
        }
    }
}