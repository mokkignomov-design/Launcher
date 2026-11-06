using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Runia.Services.Core.Infrastructure.Network.Clients;

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
        var result = await _apiService.PostAsync<PasswordResetRequest>(
            $"{BaseEndpoint}/request-password-reset",
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
    /// Подтверждение сброса пароля по коду из письма.
    /// </summary>
    public async Task<ApiResult> ConfirmPasswordResetAsync(
        string email,
        string code,
        string newPassword,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(newPassword))
        {
            return ApiResult.Failure("Email, код подтверждения и новый пароль обязательны");
        }

        var request = new ConfirmPasswordResetRequest
        {
            Email = email,
            Code = code,
            NewPassword = newPassword
        };
        var result = await _apiService.PostAsync<ConfirmPasswordResetRequest>(
            $"{BaseEndpoint}/reset-password",
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

    public async Task<ApiResult<ChangeEmailResponse>> RequestEmailChangeAsync(
        string newEmail,
        string currentPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(currentPassword))
        {
            return ApiResult<ChangeEmailResponse>.Failure("Новая почта и текущий пароль обязательны.");
        }

        var request = new ChangeEmailRequest
        {
            NewEmail = newEmail.Trim(),
            CurrentPassword = currentPassword
        };
        var result = await _apiService.PostAsync<ChangeEmailRequest, ChangeEmailResponse>(
            $"{BaseEndpoint}/change-email",
            request,
            ct);

        if (!result.IsSuccess)
        {
            return ApiResult<ChangeEmailResponse>.Failure(
                ExtractApiErrorMessage(result.ErrorMessage, "Не удалось запросить смену почты."));
        }

        return result.Data is null
            ? ApiResult<ChangeEmailResponse>.Failure("Сервер не подтвердил запрос смены почты.")
            : ApiResult<ChangeEmailResponse>.Success(result.Data);
    }

    public async Task<ApiResult<Runia.Domain.Models.AuthResponse>> ConfirmEmailChangeAsync(
        string code,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return ApiResult<Runia.Domain.Models.AuthResponse>.Failure("Введите код подтверждения.");
        }

        var request = new ConfirmEmailChangeRequest { Code = code.Trim() };
        var result = await _apiService.PostAsync<ConfirmEmailChangeRequest, Runia.Domain.Models.AuthResponse>(
            $"{BaseEndpoint}/confirm-email-change",
            request,
            ct);

        if (!result.IsSuccess)
        {
            return ApiResult<Runia.Domain.Models.AuthResponse>.Failure(
                ExtractApiErrorMessage(result.ErrorMessage, "Не удалось подтвердить новую почту."));
        }

        if (result.Data is null
            || string.IsNullOrWhiteSpace(result.Data.AccessToken)
            || string.IsNullOrWhiteSpace(result.Data.RefreshToken))
        {
            return ApiResult<Runia.Domain.Models.AuthResponse>.Failure(
                "Сервер не вернул новую пару токенов для подтверждённой почты.");
        }

        return ApiResult<Runia.Domain.Models.AuthResponse>.Success(result.Data);
    }

    private static string ExtractApiErrorMessage(string? rawMessage, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(rawMessage);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                var message = ReadStringProperty(root, "message", "detail", "error");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }

                var errors = ReadProperty(root, "errors");
                if (errors is not null)
                {
                    var messages = new List<string>();
                    CollectErrorMessages(errors.Value, messages);
                    if (messages.Count > 0)
                    {
                        return string.Join(" ", messages);
                    }
                }

                return ReadStringProperty(root, "title") ?? fallback;
            }
        }
        catch (JsonException)
        {
        }

        return rawMessage;
    }

    private static string? ReadStringProperty(JsonElement element, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (names.Contains(property.Name, StringComparer.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static JsonElement? ReadProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static void CollectErrorMessages(JsonElement element, List<string> messages)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var message = element.GetString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectErrorMessages(item, messages);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                CollectErrorMessages(property.Value, messages);
            }
        }
    }

    public Task<Result<JsonElement>> GetSessionsAsync(CancellationToken ct = default) =>
        _apiService.GetAsync<JsonElement>($"{BaseEndpoint}/sessions", ct);

    public Task<Result<bool>> RevokeSessionAsync(int sessionId, CancellationToken ct = default)
    {
        if (sessionId <= 0)
        {
            return Task.FromResult(Result<bool>.Fail(NetworkError.Unknown, "Некорректный идентификатор сессии"));
        }

        return _apiService.DeleteAsync($"{BaseEndpoint}/sessions/{sessionId}", ct);
    }

    public Task<Result<bool>> RevokeAllSessionsAsync(CancellationToken ct = default) =>
        _apiService.PostAsync<object>($"{BaseEndpoint}/revoke-all", new { }, ct);

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
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangeEmailRequest
{
    [JsonPropertyName("newEmail")]
    public string NewEmail { get; set; } = string.Empty;

    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;
}

public class ChangeEmailResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ConfirmEmailChangeRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
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
