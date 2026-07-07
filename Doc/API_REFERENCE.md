# 🔌 API Reference

Полный справочник по API клиентам и сервисам Runia Launcher.

---

## 📑 Оглавление

- [API Клиенты](#-api-клиенты)
  - [AuthClient](#authclient)
  - [UserClient](#userclient)
  - [ServerClient](#serverclient)
  - [NewsClient](#newsclient)
  - [SessionClient](#sessionclient)
- [Модели данных](#-модели-данных)
- [Обработка ошибок](#-обработка-ошибок)
- [Аутентификация](#-аутентификация)

---

## 🌐 API Клиенты

### AuthClient

Управление аутентификацией пользователя.

**Namespace:** `Runia.Services.Core.Infrastructure.Network.Clients`

#### Регистрация пользователя

```csharp
public async Task<ApiResult<RegisterResponse>> RegisterAsync(
	RegisterRequest request)
```

**Параметры:**
```csharp
public class RegisterRequest
{
	public required string Email { get; set; }
	public required string Username { get; set; }
	public required string Password { get; set; }
}
```

**Возвращает:**
```csharp
public class RegisterResponse
{
	public int UserId { get; set; }
	public string Email { get; set; }
	public string Token { get; set; }  // JWT токен
}
```

**Пример использования:**
```csharp
var authClient = app.Services.GetRequiredService<AuthClient>();

var result = await authClient.RegisterAsync(new RegisterRequest
{
	Email = "newuser@example.com",
	Username = "newuser",
	Password = "SecurePassword123!"
});

if (result.IsSuccess)
{
	Console.WriteLine($"Пользователь зарегистрирован: {result.Data.UserId}");
}
else
{
	Console.WriteLine($"Ошибка: {result.ErrorMessage}");
}
```

---

#### Вход пользователя

```csharp
public async Task<ApiResult<LoginResponse>> LoginAsync(
	LoginRequest request)
```

**Параметры:**
```csharp
public class LoginRequest
{
	public required string Email { get; set; }
	public required string Password { get; set; }
}
```

**Возвращает:**
```csharp
public class LoginResponse
{
	public int UserId { get; set; }
	public string Token { get; set; }
	public DateTime ExpiresAt { get; set; }
}
```

**Пример использования:**
```csharp
var result = await authClient.LoginAsync(new LoginRequest
{
	Email = "user@example.com",
	Password = "password"
});

if (result.IsSuccess)
{
	var token = result.Data.Token;
	await tokenStore.SaveTokenAsync(token);
}
```

---

#### Подтверждение email

```csharp
public async Task<ApiResult<Unit>> ConfirmEmailAsync(
	ConfirmEmailRequest request)
```

**Параметры:**
```csharp
public class ConfirmEmailRequest
{
	public required string Email { get; set; }
	public required string ConfirmationToken { get; set; }
}
```

**Пример использования:**
```csharp
var result = await authClient.ConfirmEmailAsync(new ConfirmEmailRequest
{
	Email = "user@example.com",
	ConfirmationToken = "token_from_email"
});

if (result.IsSuccess)
{
	Console.WriteLine("Email подтверждён!");
}
```

---

#### Запрос восстановления пароля

```csharp
public async Task<ApiResult<Unit>> ForgotPasswordAsync(
	ForgotPasswordRequest request)
```

**Параметры:**
```csharp
public class ForgotPasswordRequest
{
	public required string Email { get; set; }
}
```

**Пример использования:**
```csharp
var result = await authClient.ForgotPasswordAsync(new ForgotPasswordRequest
{
	Email = "user@example.com"
});

if (result.IsSuccess)
{
	Console.WriteLine("Инструкции восстановления отправлены на почту");
}
```

---

#### Сброс пароля

```csharp
public async Task<ApiResult<Unit>> ResetPasswordAsync(
	ResetPasswordRequest request)
```

**Параметры:**
```csharp
public class ResetPasswordRequest
{
	public required string Email { get; set; }
	public required string ResetToken { get; set; }
	public required string NewPassword { get; set; }
}
```

**Пример использования:**
```csharp
var result = await authClient.ResetPasswordAsync(new ResetPasswordRequest
{
	Email = "user@example.com",
	ResetToken = "reset_token_from_email",
	NewPassword = "NewSecurePassword123!"
});

if (result.IsSuccess)
{
	Console.WriteLine("Пароль изменён успешно!");
}
```

---

### UserClient

Управление профилем пользователя.

**Namespace:** `Runia.Services.Core.Infrastructure.Network.Clients`

#### Получить профиль пользователя

```csharp
public async Task<ApiResult<UserModel>> GetProfileAsync()
```

**Возвращает:**
```csharp
public class UserModel
{
	public int Id { get; set; }
	public string Email { get; set; }
	public string Username { get; set; }
	public byte[]? Skin { get; set; }
	public DateTime CreatedAt { get; set; }
	public UserStatus Status { get; set; }  // Active, Banned, etc.
}
```

**Пример использования:**
```csharp
var userClient = app.Services.GetRequiredService<UserClient>();

var result = await userClient.GetProfileAsync();

if (result.IsSuccess)
{
	var user = result.Data;
	Console.WriteLine($"Профиль: {user.Username} ({user.Email})");
}
```

---

#### Обновить профиль

```csharp
public async Task<ApiResult<Unit>> UpdateProfileAsync(
	UpdateUserRequest request)
```

**Параметры:**
```csharp
public class UpdateUserRequest
{
	public string? Username { get; set; }
	public string? Email { get; set; }
}
```

**Пример использования:**
```csharp
var result = await userClient.UpdateProfileAsync(new UpdateUserRequest
{
	Username = "newusername",
	Email = "newemail@example.com"
});

if (result.IsSuccess)
{
	Console.WriteLine("Профиль обновлён!");
}
```

---

#### Загрузить скин

```csharp
public async Task<ApiResult<Unit>> UploadSkinAsync(
	Stream skinStream,
	string fileName)
```

**Пример использования:**
```csharp
using (var fileStream = File.OpenRead("path/to/skin.png"))
{
	var result = await userClient.UploadSkinAsync(fileStream, "skin.png");

	if (result.IsSuccess)
	{
		Console.WriteLine("Скин загружен!");
	}
}
```

---

### ServerClient

Информация о серверах.

**Namespace:** `Runia.Services.Core.Infrastructure.Network.Clients`

#### Получить список серверов

```csharp
public async Task<ApiResult<List<ServerModel>>> GetServersAsync()
```

**Возвращает:**
```csharp
public class ServerModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Address { get; set; }
	public int Port { get; set; }
	public int CurrentPlayers { get; set; }
	public int MaxPlayers { get; set; }
	public string Status { get; set; }  // "Online", "Offline"
	public string? Description { get; set; }
}
```

**Пример использования:**
```csharp
var serverClient = app.Services.GetRequiredService<ServerClient>();

var result = await serverClient.GetServersAsync();

if (result.IsSuccess)
{
	foreach (var server in result.Data)
	{
		Console.WriteLine($"{server.Name}: {server.CurrentPlayers}/{server.MaxPlayers}");
	}
}
```

---

#### Получить информацию о конкретном сервере

```csharp
public async Task<ApiResult<ServerModel>> GetServerDetailsAsync(int serverId)
```

**Пример использования:**
```csharp
var result = await serverClient.GetServerDetailsAsync(1);

if (result.IsSuccess)
{
	var server = result.Data;
	Console.WriteLine($"Статус: {server.Status}");
	Console.WriteLine($"Игроков: {server.CurrentPlayers}/{server.MaxPlayers}");
}
```

---

#### Получить игроков на сервере

```csharp
public async Task<ApiResult<List<PlayerInfo>>> GetServerPlayersAsync(
	int serverId)
```

**Возвращает:**
```csharp
public class PlayerInfo
{
	public string Username { get; set; }
	public Guid Uuid { get; set; }
}
```

**Пример использования:**
```csharp
var result = await serverClient.GetServerPlayersAsync(1);

if (result.IsSuccess)
{
	Console.WriteLine("Игроки онлайн:");
	foreach (var player in result.Data)
	{
		Console.WriteLine($"  - {player.Username}");
	}
}
```

---

### NewsClient

Новости и обновления.

**Namespace:** `Runia.Services.Core.Infrastructure.Network.Clients`

#### Получить новости

```csharp
public async Task<ApiResult<LauncherNewsResponse>> GetNewsAsync(
	int page = 1,
	int pageSize = 10)
```

**Возвращает:**
```csharp
public class LauncherNewsResponse
{
	public List<NewsModel> Items { get; set; }
	public int TotalCount { get; set; }
	public int Page { get; set; }
	public int PageSize { get; set; }
}

public class NewsModel
{
	public int Id { get; set; }
	public string Title { get; set; }
	public string Content { get; set; }
	public string? ImageUrl { get; set; }
	public DateTime PublishedAt { get; set; }
}
```

**Пример использования:**
```csharp
var newsClient = app.Services.GetRequiredService<NewsClient>();

var result = await newsClient.GetNewsAsync(page: 1, pageSize: 5);

if (result.IsSuccess)
{
	Console.WriteLine($"Всего новостей: {result.Data.TotalCount}");
	foreach (var news in result.Data.Items)
	{
		Console.WriteLine($"- {news.Title} ({news.PublishedAt:dd.MM.yyyy})");
	}
}
```

---

#### Получить детали новости

```csharp
public async Task<ApiResult<NewsModel>> GetNewsDetailsAsync(int newsId)
```

**Пример использования:**
```csharp
var result = await newsClient.GetNewsDetailsAsync(1);

if (result.IsSuccess)
{
	var news = result.Data;
	Console.WriteLine(news.Title);
	Console.WriteLine(news.Content);
}
```

---

### SessionClient

Управление сеансами.

**Namespace:** `Runia.Services.Core.Infrastructure.Network.Clients`

#### Получить информацию о сеансе

```csharp
public async Task<ApiResult<SessionModel>> GetSessionAsync()
```

**Возвращает:**
```csharp
public class SessionModel
{
	public int UserId { get; set; }
	public string Token { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime ExpiresAt { get; set; }
	public string IpAddress { get; set; }
	public string UserAgent { get; set; }
}
```

**Пример использования:**
```csharp
var sessionClient = app.Services.GetRequiredService<SessionClient>();

var result = await sessionClient.GetSessionAsync();

if (result.IsSuccess)
{
	var session = result.Data;
	Console.WriteLine($"Сеанс истекает: {session.ExpiresAt:dd.MM.yyyy HH:mm}");
}
```

---

#### Выход из аккаунта

```csharp
public async Task<ApiResult<Unit>> LogoutAsync()
```

**Пример использования:**
```csharp
var result = await sessionClient.LogoutAsync();

if (result.IsSuccess)
{
	Console.WriteLine("Вы вышли из аккаунта");
}
```

---

## 📊 Модели данных

### ApiResult<T> - Результат API запроса

```csharp
public class ApiResult<T>
{
	public bool IsSuccess { get; set; }
	public T? Data { get; set; }
	public string? ErrorMessage { get; set; }
	public int? StatusCode { get; set; }

	public ApiResult(bool isSuccess, T? data = default, 
					 string? error = null, int? statusCode = null)
	{
		IsSuccess = isSuccess;
		Data = data;
		ErrorMessage = error;
		StatusCode = statusCode;
	}
}
```

**Использование:**
```csharp
var result = await client.GetUserAsync();

if (result.IsSuccess)
{
	// Операция прошла успешно
	var user = result.Data;
}
else
{
	// Произошла ошибка
	Console.WriteLine($"Ошибка ({result.StatusCode}): {result.ErrorMessage}");
}
```

---

## ⚠️ Обработка ошибок

### Общие HTTP статусы

| Код | Значение | Описание |
|-----|----------|---------|
| 200 | OK | Запрос успешен |
| 201 | Created | Ресурс создан |
| 400 | Bad Request | Неверные параметры |
| 401 | Unauthorized | Требуется аутентификация |
| 403 | Forbidden | Доступ запрещён |
| 404 | Not Found | Ресурс не найден |
| 409 | Conflict | Конфликт (например, email уже в использовании) |
| 500 | Server Error | Ошибка сервера |

### Обработка различных ошибок

```csharp
public async Task<bool> LoginUserAsync(string email, string password)
{
	try
	{
		var result = await authClient.LoginAsync(new LoginRequest
		{
			Email = email,
			Password = password
		});

		if (!result.IsSuccess)
		{
			// Обработка различных типов ошибок
			return result.StatusCode switch
			{
				400 => throw new ValidationException(result.ErrorMessage),
				401 => throw new AuthenticationException("Неверные учётные данные"),
				409 => throw new ConflictException("Email bereits registriert"),
				_ => throw new ApiException(result.ErrorMessage)
			};
		}

		return true;
	}
	catch (HttpRequestException ex)
	{
		Console.WriteLine($"Ошибка сети: {ex.Message}");
		return false;
	}
}
```

---

## 🔐 Аутентификация

### JWT токены

Все защищённые endpoints требуют JWT токена в заголовке:

```
Authorization: Bearer <token>
```

### Сохранение токена

```csharp
var tokenStore = app.Services.GetRequiredService<ITokenStore>();

// Сохранить
await tokenStore.SaveTokenAsync(token);

// Получить
var token = await tokenStore.GetTokenAsync();

// Удалить
await tokenStore.DeleteTokenAsync();
```

### Проверка токена

```csharp
public async Task<bool> IsTokenValidAsync()
{
	var token = await tokenStore.GetTokenAsync();

	if (string.IsNullOrEmpty(token))
		return false;

	var handler = new JwtSecurityTokenHandler();
	try
	{
		var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
		return jwtToken?.ValidTo > DateTime.UtcNow;
	}
	catch
	{
		return false;
	}
}
```

---

## 📡 Использование в ViewModels

### Типичный паттерн

```csharp
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Infrastructure.Network.Clients;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Runia.ViewModels;

public partial class UserViewModel : ObservableObject
{
	private readonly UserClient _userClient;
	private readonly ILogger<UserViewModel> _logger;

	[ObservableProperty]
	private UserModel? user;

	[ObservableProperty]
	private bool isLoading;

	[ObservableProperty]
	private string? errorMessage;

	public UserViewModel(UserClient userClient, ILogger<UserViewModel> logger)
	{
		_userClient = userClient;
		_logger = logger;
	}

	[RelayCommand]
	public async Task LoadProfile()
	{
		IsLoading = true;
		ErrorMessage = null;

		try
		{
			var result = await _userClient.GetProfileAsync();

			if (result.IsSuccess)
			{
				User = result.Data;
				_logger.LogInformation("Profile loaded successfully");
			}
			else
			{
				ErrorMessage = result.ErrorMessage ?? "Ошибка загрузки профиля";
				_logger.LogError("Failed to load profile: {Error}", result.ErrorMessage);
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = "Ошибка сети";
			_logger.LogError(ex, "Error loading profile");
		}
		finally
		{
			IsLoading = false;
		}
	}
}
```

---

📚 **Дополнительные ресурсы:**
- [README.md](./README.md) - Основная документация
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Архитектура проекта
- [GitHub API Issues](https://github.com/mokkignomov-design/Launcher/issues) - Поддержка
