using Runia.Domain.Models;
using Runia.Services.Core.Infrastructure.Network;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.Services.Network.Clients;

public class SessionClient(BaseApiService apiService)
{
    private readonly BaseApiService _apiService = apiService;
    private const string BaseEndpoint = "/api/session";

    public async Task<ApiResult<SessionResponse>> CreateTemporarySessionAsync(CancellationToken ct = default)
    {
        var result = await _apiService.GetAsync<SessionResponse>($"{BaseEndpoint}/create", ct);
        if (!result.IsSuccess)
        {
            return ApiResult<SessionResponse>.Failure(result.ErrorMessage ?? "Не удалось получить временный ключ сессии");
        }

        if (result.Data == null)
        {
            return ApiResult<SessionResponse>.Failure("Пустой ответ от сервера");
        }

        return ApiResult<SessionResponse>.Success(result.Data);
    }
}
