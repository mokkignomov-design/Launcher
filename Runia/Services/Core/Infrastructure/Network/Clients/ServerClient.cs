using Runia.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Runia.Services.Core.Infrastructure.Network.Clients;

public class ServersResponse
{
    [JsonPropertyName("servers")]
    public List<ServerPack> Servers { get; set; } = [];
}

/// <summary>
/// Клиент для работы с API серверов.
/// Отвечает за загрузку списка серверов, информации о конкретном сервере и управление сервером.
/// </summary>
public class ServerClient(BaseApiService apiService)
{
    private readonly BaseApiService _apiService = apiService;
    // API uses launcher namespace on backend
    private const string BaseEndpoint = "/api/launcher/servers";

    /// <summary>
    /// Получить список всех доступных серверов.
    /// </summary>
    public async Task<ApiResult<IEnumerable<ServerPack>>> GetServersAsync(CancellationToken ct = default)
    {
        var result = await _apiService.GetAsync<ServersResponse>(BaseEndpoint, ct);

        if (!result.IsSuccess)
        {
            return ApiResult<IEnumerable<ServerPack>>.Failure(
                result.ErrorMessage ?? $"Ошибка загрузки серверов: {result.Error}"
            );
        }

        return ApiResult<IEnumerable<ServerPack>>.Success(result.Data?.Servers ?? []);
    }

    /// <summary>
    /// Получить информацию о конкретном сервере по ID.
    /// </summary>
    public async Task<ApiResult<ServerPack>> GetServerByIdAsync(string serverId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return ApiResult<ServerPack>.Failure("ID сервера не может быть пустым");
        }

        var result = await _apiService.GetAsync<ServerPack>($"{BaseEndpoint}/{serverId}", ct);

        if (!result.IsSuccess)
        {
            return ApiResult<ServerPack>.Failure(
                result.ErrorMessage ?? $"Ошибка загрузки информации о сервере: {result.Error}"
            );
        }

        if (result.Data == null)
        {
            return ApiResult<ServerPack>.Failure("Сервер не найден");
        }

        return ApiResult<ServerPack>.Success(result.Data);
    }

    /// <summary>
    /// Получить информацию об игроке на сервере.
    /// </summary>
    public async Task<ApiResult<ServerPlayerInfo>> GetPlayerInfoAsync(string serverId, string playerName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(playerName))
        {
            return ApiResult<ServerPlayerInfo>.Failure("ID сервера и имя игрока обязательны");
        }

        var result = await _apiService.GetAsync<ServerPlayerInfo>(
            $"{BaseEndpoint}/{serverId}/players/{playerName}",
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<ServerPlayerInfo>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки информации об игроке"
            );
        }

        return ApiResult<ServerPlayerInfo>.Success(result.Data);
    }

    /// <summary>
    /// Присоединиться к серверу.
    /// </summary>
    public async Task<ApiResult<JoinServerResponse>> JoinServerAsync(string serverId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return ApiResult<JoinServerResponse>.Failure("ID сервера не может быть пустым");
        }

        var result = await _apiService.PostAsync<object, JoinServerResponse>(
            $"{BaseEndpoint}/{serverId}/join",
            new { },
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<JoinServerResponse>.Failure(
                result.ErrorMessage ?? $"Ошибка присоединения к серверу: {result.Error}"
            );
        }

        return ApiResult<JoinServerResponse>.Success(result.Data);
    }
}

/// <summary>
/// Информация об игроке на сервере.
/// </summary>
public class ServerPlayerInfo
{
    public string PlayerName { get; set; } = string.Empty;
    public int Level { get; set; }
    public long PlayTime { get; set; }
    public DateTime LastOnline { get; set; }
}

/// <summary>
/// Ответ при присоединении к серверу.
/// </summary>
public class JoinServerResponse
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public int ServerPort { get; set; }
    public string LaunchUrl { get; set; } = string.Empty;
}
