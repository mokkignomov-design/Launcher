using Runia.Services.Core.Infrastructure.Network;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.Services.Network.Clients;

/// <summary>
/// Клиент для работы с API аутентификации.
/// Отвечает за логин, регистрацию, восстановление пароля и управление токенами.
/// </summary>
public class AuthClient(BaseApiService apiService)
{
    private readonly BaseApiService _apiService = apiService;
    private const string BaseEndpoint = "/api/auth";

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public async Task<ApiResult<AuthResponse>> LoginAsync(
        string username,
        string password,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return ApiResult<AuthResponse>.Failure("Имя пользователя и пароль обязательны");
        }

        var request = new LoginRequest { Username = username, Password = password };
        var result = await _apiService.PostAsync<LoginRequest, AuthResponse>(
            $"{BaseEndpoint}/login",
            request,
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<AuthResponse>.Failure(
                result.ErrorMessage ?? "Ошибка при входе. Проверьте учетные данные."
            );
        }

        if (result.Data == null)
        {
            return ApiResult<AuthResponse>.Failure("Ошибка при входе");
        }

        return ApiResult<AuthResponse>.Success(result.Data);
    }

    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    public async Task<ApiResult<AuthResponse>> RegisterAsync(
        string username,
        string email,
        string password,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return ApiResult<AuthResponse>.Failure("Все поля обязательны");
        }

        var request = new RegisterRequest { Username = username, Email = email, Password = password };
        var result = await _apiService.PostAsync<RegisterRequest, AuthResponse>(
            $"{BaseEndpoint}/register",
            request,
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<AuthResponse>.Failure(
                result.ErrorMessage ?? "Ошибка при регистрации"
            );
        }

        if (result.Data == null)
        {
            return ApiResult<AuthResponse>.Failure("Ошибка при регистрации");
        }

        return ApiResult<AuthResponse>.Success(result.Data);
    }

    /// <summary>
    /// Запрос на восстановление пароля.
    /// </summary>
    public async Task<ApiResult> RequestPasswordResetAsync(
        string email,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ApiResult.Failure("Email не может быть пустым");
        }

        var request = new PasswordResetRequest { Email = email };
        var result = await _apiService.PostAsync<PasswordResetRequest, object>(
            $"{BaseEndpoint}/reset-password/request",
            request,
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при запросе восстановления пароля"
            );
        }

        return ApiResult.Success();
    }

    /// <summary>
    /// Подтверждение восстановления пароля.
    /// </summary>
    public async Task<ApiResult> ConfirmPasswordResetAsync(
        string token,
        string newPassword,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
        {
            return ApiResult.Failure("Токен и пароль обязательны");
        }

        var request = new ConfirmPasswordResetRequest { Token = token, NewPassword = newPassword };
        var result = await _apiService.PostAsync<ConfirmPasswordResetRequest, object>(
            $"{BaseEndpoint}/reset-password/confirm",
            request,
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при подтверждении восстановления пароля"
            );
        }

        return ApiResult.Success();
    }

    /// <summary>
    /// Обновить токен доступа используя refresh token.
    /// </summary>
    public async Task<ApiResult<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return ApiResult<AuthResponse>.Failure("Refresh token не может быть пустым");
        }

        var request = new RefreshTokenRequest { RefreshToken = refreshToken };
        var result = await _apiService.PostAsync<RefreshTokenRequest, AuthResponse>(
            $"{BaseEndpoint}/refresh-token",
            request,
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<AuthResponse>.Failure(
                result.ErrorMessage ?? "Ошибка при обновлении токена"
            );
        }

        if (result.Data == null)
        {
            return ApiResult<AuthResponse>.Failure("Ошибка при обновлении токена");
        }

        return ApiResult<AuthResponse>.Success(result.Data);
    }

    /// <summary>
    /// Выход из системы.
    /// </summary>
    public async Task<ApiResult> LogoutAsync(CancellationToken ct = default)
    {
        var result = await _apiService.PostAsync<object, object>(
            $"{BaseEndpoint}/logout",
            new { },
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult.Failure(
                result.ErrorMessage ?? "Ошибка при выходе"
            );
        }

        return ApiResult.Success();
    }

    /// <summary>
    /// Проверить доступность имени пользователя.
    /// </summary>
    public async Task<ApiResult<bool>> IsUsernameAvailableAsync(
        string username,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ApiResult<bool>.Failure("Имя пользователя не может быть пустым");
        }

        var result = await _apiService.GetAsync<bool>(
            $"{BaseEndpoint}/check-username/{username}",
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<bool>.Failure(
                result.ErrorMessage ?? "Ошибка при проверке доступности имени"
            );
        }

        return ApiResult<bool>.Success(result.Data);
    }

    /// <summary>
    /// Проверить доступность email.
    /// </summary>
    public async Task<ApiResult<bool>> IsEmailAvailableAsync(
        string email,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ApiResult<bool>.Failure("Email не может быть пустым");
        }

        var result = await _apiService.GetAsync<bool>(
            $"{BaseEndpoint}/check-email/{email}",
            ct
        );

        if (!result.IsSuccess)
        {
            return ApiResult<bool>.Failure(
                result.ErrorMessage ?? "Ошибка при проверке доступности email"
            );
        }

        return ApiResult<bool>.Success(result.Data);
    }
}

/// <summary>
/// Запрос на логин.
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на регистрацию.
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на восстановление пароля.
/// </summary>
public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на подтверждение восстановления пароля.
/// </summary>
public class ConfirmPasswordResetRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на обновление токена.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Ответ при успешной аутентификации.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
