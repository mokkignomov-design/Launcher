using Runia.Domain.Models;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Network;
using System.Text.Json;

namespace Runia.Services.Auth
{
    public enum AuthResult { Success, InvalidCredentials, ServerError, NetworkError, NotRemembered, OfflineSuccess }

    public class UserProfile { public string Name { get; set; } = ""; public string Rank { get; set; } = ""; public int Balance { get; set; } }
    public class ServerInfo { public string Name { get; set; } = ""; public int Online { get; set; } public string Status { get; set; } = ""; }

    public class AuthService(IAppConfig _config, BaseApiService _api, ITokenStore _tokenStore, ILogger<AuthService> _logger)
    {
        private readonly IAppConfig _appConfig = _config;
        private readonly ILogger<AuthService> _log = _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }

        public UserProfile? CurrentUser { get; private set; }
        public List<ServerInfo> CachedServers { get; private set; } = [];
        public string? LastRequestedResetEmail { get; private set; }

        /// <summary>
        /// Безопасное извлечение username из JWT токена.
        /// </summary>
        private string? ExtractUsernameFromToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return null;

                var payload = parts[1];
                // Добавляем padding если необходимо
                var padding = 4 - (payload.Length % 4);
                if (padding < 4) payload += new string('=', padding);

                var decodedBytes = Convert.FromBase64String(payload);
                var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);

                using var doc = JsonDocument.Parse(decodedString);
                if (doc.RootElement.TryGetProperty("sub", out var subElement))
                {
                    return subElement.GetString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _log.LogWarning("Ошибка при извлечении username из токена: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            var req = new AuthRequest { Username = username, Password = password };
            var res = await _api.PostAsync<AuthRequest, AuthResponse>("api/auth/login", req, ct);

            if (res.IsSuccess && res.Data != null)
            {
                AccessToken = res.Data.AccessToken;
                RefreshToken = res.Data.RefreshToken;

                // Сохраняем username вместе с токеном в защищенное хранилище
                await _tokenStore.SaveTokensAsync(AccessToken, RefreshToken, true, username);

                CurrentUser = new UserProfile { Name = username, Rank = "Player", Balance = 0 };
                CachedServers = [];
                return AuthResult.Success;
            }

            if (!res.IsSuccess && (res.Error == NetworkError.NetworkUnavailable || res.Error == NetworkError.Timeout))
            {
                var (access, _, storedUsername) = await _tokenStore.GetTokensAsync();
                if (!string.IsNullOrWhiteSpace(access))
                {
                    AccessToken = access;
                    // Используем сохраненный username, а не передаваемый пользователем
                    CurrentUser = new UserProfile { Name = storedUsername ?? "Player", Rank = "Player", Balance = 0 };
                    return AuthResult.OfflineSuccess;
                }

                return AuthResult.NetworkError;
            }

            return res.Error == NetworkError.Unauthorized ? AuthResult.InvalidCredentials : AuthResult.ServerError;
        }

        public async Task<AuthResult> RegisterAsync(string username, string email, string password, CancellationToken ct = default)
        {
            var req = new Runia.Domain.Models.RegisterRequest { Username = username, Email = email, Password = password };
            var res = await _api.PostAsync<Runia.Domain.Models.RegisterRequest, object>("api/auth/register", req, ct);
            if (res.IsSuccess) return AuthResult.Success;
            if (res.Error == NetworkError.NetworkUnavailable || res.Error == NetworkError.Timeout) return AuthResult.NetworkError;
            return AuthResult.ServerError;
        }

        public async Task<AuthResult> ConfirmEmailAsync(string email, string code, CancellationToken ct = default)
        {
            var req = new { email, code };
            var res = await _api.PostAsync<object, object>("api/auth/confirm-email", req, ct);
            if (res.IsSuccess) return AuthResult.Success;
            if (res.Error == NetworkError.NetworkUnavailable || res.Error == NetworkError.Timeout) return AuthResult.NetworkError;
            return AuthResult.ServerError;
        }

        public async Task<AuthResult> ResendCodeAsync(string email, CancellationToken ct = default)
        {
            var req = new { email };
            var res = await _api.PostAsync<object, object>("api/auth/resend-code", req, ct);
            if (res.IsSuccess) return AuthResult.Success;
            if (res.Error == NetworkError.NetworkUnavailable || res.Error == NetworkError.Timeout) return AuthResult.NetworkError;
            return AuthResult.ServerError;
        }

        public async Task<AuthResult> RequestPasswordResetAsync(string email, CancellationToken ct = default)
        {
            var req = new { email };
            var res = await _api.PostAsync<object, object>("api/auth/request-password-reset", req, ct);
            LastRequestedResetEmail = email;
            if (res.IsSuccess) return AuthResult.Success;
            if (res.Error == NetworkError.NetworkUnavailable || res.Error == NetworkError.Timeout) return AuthResult.NetworkError;
            return AuthResult.ServerError;
        }

        public async Task<AuthResult> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default)
        {
            var req = new { email, code, newPassword };
            var res = await _api.PostAsync<object, object>("api/auth/reset-password", req, ct);
            if (res.IsSuccess) return AuthResult.Success;
            if (res.Error == NetworkError.NetworkUnavailable || res.Error == NetworkError.Timeout) return AuthResult.NetworkError;
            return AuthResult.ServerError;
        }

        public async Task<bool> RefreshTokenAsync(CancellationToken ct = default)
        {
            await _refreshLock.WaitAsync(ct);
            try
            {
                var (_, refresh, storedUsername) = await _tokenStore.GetTokensAsync();
                if (string.IsNullOrWhiteSpace(refresh)) return false;

                var req = new { refreshToken = refresh };
                var res = await _api.PostAsync<object, AuthResponse>("api/auth/refresh", req, ct);

                if (res.IsSuccess && res.Data != null)
                {
                    AccessToken = res.Data.AccessToken;
                    RefreshToken = res.Data.RefreshToken;
                    // Сохраняем с тем же username, что и раньше
                    await _tokenStore.SaveTokensAsync(AccessToken, RefreshToken, true, storedUsername);
                    return true;
                }

                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<AuthResult> LoadAndRefreshTokensAsync(CancellationToken ct = default)
        {
            var (access, refresh, storedUsername) = await _tokenStore.GetTokensAsync();
            if (string.IsNullOrWhiteSpace(access)) return AuthResult.NotRemembered;

            AccessToken = access;
            RefreshToken = refresh;
            // Используем сохраненный username из защищенного хранилища
            CurrentUser = new UserProfile { Name = storedUsername ?? "Player", Rank = "Player", Balance = 0 };
            return AuthResult.Success;
        }

        public async Task SaveTokensAsync(bool rememberMe) => await _tokenStore.SaveTokensAsync(AccessToken, RefreshToken, rememberMe, CurrentUser?.Name);

        public async Task LogoutAsync()
        {
            AccessToken = null;
            RefreshToken = null;
            CurrentUser = null;
            CachedServers = [];
            await _tokenStore.ClearAsync();
        }

        public void SaveTokens(bool rememberMe) => _ = SaveTokensAsync(rememberMe);
        public void Logout() => _ = LogoutAsync();
    }
}