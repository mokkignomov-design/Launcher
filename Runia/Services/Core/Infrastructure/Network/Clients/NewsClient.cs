using System.Collections.Generic;
using System.Linq;
using Runia.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.Services.Core.Infrastructure.Network.Clients;

public class NewsClient(BaseApiService apiService, ILocalDataService? localData = null)
{
    private readonly BaseApiService _apiService = apiService;
    private readonly ILocalDataService? _localData = localData;
    private const string BaseEndpoint = "/api/launcher/news";
    private const string NewsFileName = "news.json";
        public async Task<ApiResult<IEnumerable<NewsItem>>> GetNewsAsync(CancellationToken ct = default)
    {
        var result = await _apiService.GetAsync<LauncherNewsResponse>(BaseEndpoint, ct);

        if (!result.IsSuccess)
        {
            return ApiResult<IEnumerable<NewsItem>>.Failure(
                result.ErrorMessage ?? $"Ошибка загрузки новостей: {result.Error}"
            );
        }

        var items = result.Data?.News ?? [];

        if (_localData != null && items != null)
        {
            try
            {
                await _localData.SaveJsonAsync(NewsFileName, items);
            }
            catch { /* Ошибка кэша не критична */ }
        }

        return ApiResult<IEnumerable<NewsItem>>.Success(items);
    }

    public async Task<ApiResult<PaginatedNews>> GetLatestNewsAsync(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var url = $"{BaseEndpoint}?page={page}&pageSize={pageSize}";
        var result = await _apiService.GetAsync<LauncherNewsResponse>(url, ct);

        if (!result.IsSuccess)
        {
            if (_localData != null && page == 1)
            {
                try
                {
                    var cached = await _localData.LoadFromJsonAsync<List<NewsItem>>(NewsFileName);
                    if (cached != null && cached.Count > 0)
                    {
                        var paginatedCached = new PaginatedNews
                        {
                            Items = cached,
                            TotalCount = cached.Count,
                            PageNumber = 1,
                            PageSize = cached.Count
                        };
                        System.Diagnostics.Debug.WriteLine($"[NewsClient] API error when fetching news: {result.Error}. Falling back to cached {cached.Count} items.");
                        return ApiResult<PaginatedNews>.Success(paginatedCached);
                    }
                }
                catch { /* Ошибка кэша не критична */ }
            }
            return ApiResult<PaginatedNews>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки новостей"
            );
        }
        var response = result.Data;
        var items = response?.News ?? [];

        if (_localData != null && items != null)
        {
            try
            {
                await _localData.SaveJsonAsync(NewsFileName, items);
            }
            catch { /* Ошибка кэша не критична */ }
        }

      
        var paginated = new PaginatedNews
        {
            Items = items!, 
            TotalCount = items!.Count, 
            PageNumber = page,
            PageSize = pageSize
        };

        return ApiResult<PaginatedNews>.Success(paginated);
    }
    public async Task<ApiResult<NewsItem>> GetNewsDetailAsync(string newsId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newsId))
        {
            return ApiResult<NewsItem>.Failure("ID новости не может быть пустым");
        }

        var result = await _apiService.GetAsync<NewsItem>($"{BaseEndpoint}/{newsId}", ct);

        if (!result.IsSuccess)
        {
            return ApiResult<NewsItem>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки новости"
            );
        }

        if (result.Data == null)
        {
            return ApiResult<NewsItem>.Failure("Новость не найдена");
        }

        return ApiResult<NewsItem>.Success(result.Data);
    }

    public async Task<ApiResult<IEnumerable<NewsItem>>> GetNewsByCategoryAsync(string category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return ApiResult<IEnumerable<NewsItem>>.Failure("Категория не может быть пустой");
        }

        var result = await _apiService.GetAsync<IEnumerable<NewsItem>>(
            $"{BaseEndpoint}/category/{category}",
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<IEnumerable<NewsItem>>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки новостей по категории"
            );
        }

        return ApiResult<IEnumerable<NewsItem>>.Success(result.Data ?? []);
    }

    /// <summary>
    /// Подписаться на новости определенной категории.
    /// </summary>
    public async Task<ApiResult> SubscribeToCategoryAsync(string category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return ApiResult.Failure("Категория не может быть пустой");
        }

        var result = await _apiService.PostAsync<object, object>(
            $"{BaseEndpoint}/subscribe/{category}",
            new { },
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при подписке на категорию"
            );
        }

        return ApiResult.Success();
    }

    /// <summary>
    /// Отписаться от новостей определенной категории.
    /// </summary>
    public async Task<ApiResult> UnsubscribeFromCategoryAsync(string category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return ApiResult.Failure("Категория не может быть пустой");
        }

        var result = await _apiService.DeleteAsync($"{BaseEndpoint}/subscribe/{category}", ct);

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при отписке от категории"
            );
        }

        return ApiResult.Success();
    }
}

/// <summary>
/// Пагинированный результат новостей.
/// </summary>
public class PaginatedNews
{
    public IEnumerable<NewsItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
