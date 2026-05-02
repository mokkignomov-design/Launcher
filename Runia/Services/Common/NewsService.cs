using Microsoft.Extensions.Logging;
using Runia.Domain.Models;
using Runia.Services.Core.Infrastructure;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Network;

namespace Runia.Services.Common;

public class NewsService(IAppConfig config, BaseApiService api, ILocalDataService localData, ILogger<NewsService> logger)
{
    private const string NewsFileName = "news.json";
    private readonly IAppConfig _config = config;
    private readonly BaseApiService _api = api;
    private readonly ILocalDataService _localData = localData;
    private readonly ILogger<NewsService> _logger = logger;

    private class NewsResponse
    {
        public List<Runia.Domain.Models.NewsItem> News { get; set; } = [];
    }

    public async Task<List<NewsItem>> GetLatestNewsAsync(CancellationToken ct = default)
    {
        var remote = await _api.GetAsync<NewsResponse>("api/launcher/news", ct);
        if (remote.IsSuccess && remote.Data?.News != null)
        {
            await _localData.SaveJsonAsync(NewsFileName, remote.Data.News);
            return remote.Data.News;
        }

        try
        {
            var local = await _localData.LoadFromJsonAsync<List<NewsItem>>(NewsFileName);
            return local ?? [];
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to load local news cache");
            return [];
        }
    }
}
