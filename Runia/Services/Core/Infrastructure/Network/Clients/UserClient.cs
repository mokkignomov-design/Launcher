using Runia.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.Services.Core.Infrastructure.Network.Clients;
public class UserClient(BaseApiService apiService, ITokenStore tokenStore, Auth.AuthService authService)
{
    private readonly BaseApiService _apiService = apiService;
    private readonly ITokenStore _tokenStore = tokenStore;
    private readonly Auth.AuthService _authService = authService;
    private const string BaseEndpoint = "/api/users";

    public async Task<ApiResult<UserProfile>> GetCurrentUserProfileAsync(CancellationToken ct = default)
    {
        // Попробуем получить профиль по access token через /api/auth/me (предпочтительно)
        var meResult = await _apiService.GetAsync<UserProfile>("/api/auth/me", ct);
        if (meResult.IsSuccess && meResult.Data != null)
        {
            return ApiResult<UserProfile>.Success(meResult.Data);
        }

        // Если не получилось, пробуем взять username из текущего AuthService (после логина в памяти)
        var fromAuth = _authService.CurrentUser?.Name;
        if (!string.IsNullOrWhiteSpace(fromAuth))
        {
            return await GetUserProfileByNameAsync(fromAuth, ct);
        }

        // В крайнем случае пробуем username из token store (если был сохранён)
        var (_, _, storedUsername) = await _tokenStore.GetTokensAsync();
        if (string.IsNullOrWhiteSpace(storedUsername))
        {
            return ApiResult<UserProfile>.Failure("Username not available");
        }

        return await GetUserProfileByNameAsync(storedUsername, ct);
    }

    public async Task<ApiResult<UserProfile>> GetUserProfileByNameAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ApiResult<UserProfile>.Failure("Имя пользователя не может быть пустым");
        }

        var result = await _apiService.GetAsync<UserProfile>($"{BaseEndpoint}/{username}", ct);

        if (!result.IsSuccess)
        {
            return ApiResult<UserProfile>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки профиля пользователя"
            );
        }

        if (result.Data == null)
        {
            return ApiResult<UserProfile>.Failure("Пользователь не найден");
        }

        return ApiResult<UserProfile>.Success(result.Data);
    }

    public async Task<ApiResult<UserStats>> GetUserStatsAsync(string? username = null, CancellationToken ct = default)
    {
        string? target = username;
        if (string.IsNullOrWhiteSpace(target))
        {
            var (_, _, Username) = await _tokenStore.GetTokensAsync();
            target = Username;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            return ApiResult<UserStats>.Failure("Username not available");
        }

        var result = await _apiService.GetAsync<UserStats>($"{BaseEndpoint}/{target}/stats", ct);

        if (!result.IsSuccess)
        {
            return ApiResult<UserStats>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки статистики"
            );
        }

        return ApiResult<UserStats>.Success(result.Data);
    }

    public async Task<ApiResult<IEnumerable<UserActivity>>> GetUserActivityAsync(
        string? username = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        string? target = username;
        if (string.IsNullOrWhiteSpace(target))
        {
            var (_, _, Username) = await _tokenStore.GetTokensAsync();
            target = Username;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            return ApiResult<IEnumerable<UserActivity>>.Failure("Username not available");
        }

        var url = $"{BaseEndpoint}/{target}/activities?page={page}&pageSize={pageSize}";
        var result = await _apiService.GetAsync<IEnumerable<UserActivity>>(url, ct);

        if (!result.IsSuccess)
        {
            return ApiResult<IEnumerable<UserActivity>>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки истории активности"
            );
        }

        return ApiResult<IEnumerable<UserActivity>>.Success(result.Data ?? []);
    }

    public async Task<ApiResult<UserProfile>> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        CancellationToken ct = default
    )
    {
        if (request == null)
        {
            return ApiResult<UserProfile>.Failure("Данные для обновления не могут быть пустыми");
        }

        var result = await _apiService.PutAsync<UpdateUserProfileRequest, UserProfile>(
            $"{BaseEndpoint}/me",
            request,
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<UserProfile>.Failure(
                result.ErrorMessage ?? "Ошибка при обновлении профиля"
            );
        }

        return ApiResult<UserProfile>.Success(result.Data);
    }

  
    public async Task<ApiResult<IEnumerable<UserFriend>>> GetFriendsAsync(CancellationToken ct = default)
    {
        var result = await _apiService.GetAsync<IEnumerable<UserFriend>>($"{BaseEndpoint}/me/friends", ct);

        if (!result.IsSuccess)
        {
            return ApiResult<IEnumerable<UserFriend>>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки списка друзей"
            );
        }

        return ApiResult<IEnumerable<UserFriend>>.Success(result.Data ?? []);
    }

    public async Task<ApiResult> AddFriendAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ApiResult.Failure("Имя пользователя не может быть пустым");
        }

        var result = await _apiService.PostAsync<object, object>(
            $"{BaseEndpoint}/me/friends/{username}",
            new { },
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при добавлении друга"
            );
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult> RemoveFriendAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ApiResult.Failure("Имя пользователя не может быть пустым");
        }

        var result = await _apiService.DeleteAsync($"{BaseEndpoint}/me/friends/{username}", ct);

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при удалении друга"
            );
        }

        return ApiResult.Success();
    }

}

public class UpdateUserProfileRequest
{
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UserFriend
{
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime FriendsSince { get; set; }
}
