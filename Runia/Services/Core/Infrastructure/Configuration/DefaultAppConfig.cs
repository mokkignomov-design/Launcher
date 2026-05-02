using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Runia.Domain.Models; 
using Runia.Services.Core.Launcher;

namespace Runia.Services.Core.Infrastructure.Configuration
{
    public class ServerResponse
    {
        public List<ServerPack> Servers { get; set; } = [];
    }

    /// <summary>
    /// DefaultAppConfig предоставляет конфигурацию приложения.
    /// 
    /// ВНИМАНИЕ БЕЗОПАСНОСТИ: Этот класс НЕ содержит методов для управления Username, AccessToken или RefreshToken.
    /// Все данные аутентификации полностью управляются AuthService и защищены в SecureTokenStore с использованием DPAPI.
    /// </summary>
    public class DefaultAppConfig(IHttpClientFactory httpFactory, ILogger<DefaultAppConfig> logger) : IAppConfig
    {
        private readonly HttpClient _httpClient = httpFactory.CreateClient("Api");
        private readonly ILogger<DefaultAppConfig> _logger = logger;
        private List<ServerPack>? _cachedServers;

        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string ApiBaseUrl => "https://api.runia.su/";
        public int RamAmount => Runia.Properties.Settings.Default.RamAmount * 1024;
        public bool ShowConsole => Runia.Properties.Settings.Default.ShowConsole;
        public bool IsFullscreen => Runia.Properties.Settings.Default.IsFullscreen;
        public string JvmArguments => Runia.Properties.Settings.Default.JvmArguments ?? string.Empty;

        public VersionsManager Versions { get; } = new();

        public string GameRootPath
        {
            get
            {
                var savedPath = Runia.Properties.Settings.Default.GameRootPath;
                if (!string.IsNullOrWhiteSpace(savedPath)) return savedPath;

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Runia"
                );
            }
        }

        public async Task<List<ServerPack>> GetServersAsync()
        {
            string cachePath = Path.Combine(VersionsManager.Root, "servers_cache.json");
            if (_cachedServers != null) return _cachedServers;
            if (File.Exists(cachePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cachePath);
                    _cachedServers = JsonSerializer.Deserialize<List<ServerPack>>(json, s_jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Кэш серверов поврежден: {Msg}", ex.Message);
                }
            }

            try
            {
                var response = await _httpClient.GetFromJsonAsync<ServerResponse>($"{ApiBaseUrl}api/launcher/servers");
                if (response?.Servers != null)
                {
                    _cachedServers = response.Servers;

                    var json = JsonSerializer.Serialize(_cachedServers, s_jsonOptions);
                    await File.WriteAllTextAsync(cachePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка связи с сервером {Url}: {Msg}", ApiBaseUrl, ex.Message);
            }

            return _cachedServers ?? [];
        }
    }
}