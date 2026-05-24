using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    public class DefaultAppConfig : IAppConfig
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DefaultAppConfig> _logger;
        private string? _appSettingsApiBase;

        public DefaultAppConfig()
        {
            _httpClient = new HttpClient();
            _logger = NullLogger<DefaultAppConfig>.Instance;
            InitAppSettingsFromFile();
        }

        public DefaultAppConfig(IHttpClientFactory httpFactory, ILogger<DefaultAppConfig> logger)
        {
            _httpClient = httpFactory.CreateClient("Api");
            _logger = logger;
            InitAppSettingsFromFile();
        }

        private void InitAppSettingsFromFile()
        {
            try
            {
                var basePath = AppContext.BaseDirectory;
                var cfgPath = Path.Combine(basePath, "appsettings.json");
                if (File.Exists(cfgPath))
                {
                    var txt = File.ReadAllText(cfgPath);
                    using var doc = JsonDocument.Parse(txt);
                    if (doc.RootElement.TryGetProperty("Runia", out var runia) && runia.TryGetProperty(nameof(ApiBaseUrl), out var url))
                    {
                        // assign to backing field via reflection to satisfy readonly semantics
                        // but since _appSettingsApiBase is readonly, assign directly here in constructor flow
                        // this method is only called from constructors
                        _appSettingsApiBase = url.GetString();
                    }
                }
            }
            catch
            {
                // keep _appSettingsApiBase as null
                _appSettingsApiBase = null;
            }
        }
        private List<ServerPack>? _cachedServers;

        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string EnvVarApiBase = "RUNIA_API_BASE_URL";
        public string ApiBaseUrl
        {
            get
            {
                var env = Environment.GetEnvironmentVariable(EnvVarApiBase);
                if (!string.IsNullOrWhiteSpace(env)) return env.EndsWith("/") ? env : env + "/";
                if (!string.IsNullOrWhiteSpace(_appSettingsApiBase)) return _appSettingsApiBase!.EndsWith("/") ? _appSettingsApiBase! : _appSettingsApiBase! + "/";
                return "https://api.runia.su/";
            }
        }
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