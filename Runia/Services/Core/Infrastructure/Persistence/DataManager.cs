using Microsoft.Extensions.Logging;
using Runia.Domain.Models;
using System.IO;
using System.Text.Json;
using Runia.Services.Core.Infrastructure.Network;

namespace Runia.Services.Core.Infrastructure.Persistence;

public class DataManager(BaseApiService api, ILogger<DataManager> logger)
{
    private readonly BaseApiService _api = api;
    private readonly ILogger<DataManager> _logger = logger;
    private readonly string _localConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "appsettings.json");

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<ServerPack>> GetServersAsync(CancellationToken ct = default)
    {
        var remote = await _api.GetAsync<List<ServerPack>>("api/servers", ct);
        if (remote.IsSuccess && remote.Data != null)
            return remote.Data;

        try
        {
            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Servers.json");
            if (!File.Exists(localPath)) return [];

            var json = await File.ReadAllTextAsync(localPath, ct);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<ServerPack>>>(json, s_jsonOptions);

            return data != null && data.TryGetValue("Servers", out var srv) ? srv : [];
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load local servers");
            return [];
        }
    }

    public async Task<List<NewsItem>> GetNewsAsync(CancellationToken ct = default)
    {
        var remote = await _api.GetAsync<List<NewsItem>>("api/news", ct);
        if (remote.IsSuccess && remote.Data != null)
        {
            return remote.Data;
        }

        try
        {
            if (!File.Exists(_localConfigPath)) return [];

            using var stream = File.OpenRead(_localConfigPath);
            using var doc = await JsonDocument.ParseAsync(stream, default, ct);

            if (doc.RootElement.TryGetProperty("News", out var newsEl))
            {
                return JsonSerializer.Deserialize<List<NewsItem>>(newsEl.GetRawText(), s_jsonOptions) ?? [];
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to read news");
        }

        return [];
    }
}
