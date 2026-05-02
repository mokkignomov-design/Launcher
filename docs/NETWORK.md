# 🌐 Сетевое взаимодействие в Runia

Документация по API интеграции, обработке ошибок и управлению JWT токенами.

## 📋 Обзор

Сетевой слой обеспечивает:
- HTTP коммуникацию с API сервером
- Управление JWT токенами
- Обработку ошибок сети
- Безопасное хранение учётных данных
- Автоматическое обновление токенов

## 🏗️ Архитектура сетевого слоя

```
┌──────────────────────────────────────────┐
│       Application Layer (ViewModel)       │
│  (LoginViewModel, HomeViewModel, etc.)   │
└────────────────┬─────────────────────────┘
                 │
┌────────────────▼─────────────────────────┐
│         AuthService / Other Services     │
│  (High-level business logic)             │
└────────────────┬─────────────────────────┘
                 │
┌────────────────▼─────────────────────────┐
│           BaseApiService                 │
│  (Generic HTTP client)                   │
└────────────────┬─────────────────────────┘
                 │
┌────────────────▼─────────────────────────┐
│            JwtHandler                    │
│  (Automatic token injection)             │
└────────────────┬─────────────────────────┘
                 │
┌────────────────▼─────────────────────────┐
│         HttpClient (System)              │
│  (Low-level HTTP)                        │
└────────────────┬─────────────────────────┘
                 │
┌────────────────▼─────────────────────────┐
│         API Server (HTTP/HTTPS)          │
│  (Backend REST API)                      │
└──────────────────────────────────────────┘
```

## 🔧 Основные компоненты

### BaseApiService

Основной HTTP клиент для всех API запросов.

**Файл**: `Services/Core/Infrastructure/Network/BaseApiService.cs`

```csharp
public sealed class BaseApiService
{
    private readonly HttpClient _client;
    private readonly ILogger<BaseApiService> _logger;

    public BaseApiService(HttpClient client, ILogger<BaseApiService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// GET запрос
    /// </summary>
    public async Task<Result<T>> GetAsync<T>(string endpoint)

    /// <summary>
    /// POST запрос с body
    /// </summary>
    public async Task<Result<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest request)

    /// <summary>
    /// PUT запрос
    /// </summary>
    public async Task<Result<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest request)

    /// <summary>
    /// DELETE запрос
    /// </summary>
    public async Task<Result<bool>> DeleteAsync(string endpoint)
}
```

### Result<T> - Тип результата

```csharp
public sealed class Result<T>
{
    // Успешен ли результат
    public bool IsSuccess { get; }

    // Данные результата
    public T? Data { get; }

    // Тип ошибки (если есть)
    public NetworkError Error { get; }

    // Сообщение об ошибке (если есть)
    public string? ErrorMessage { get; }

    // Фабричные методы
    public static Result<T> Success(T data)
    public static Result<T> Failure(NetworkError error, string? message = null)
}

public enum NetworkError
{
    None,                      // Нет ошибок
    HttpError,                 // HTTP ошибка (4xx, 5xx)
    Timeout,                   // Timeout запроса
    NetworkUnavailable,        // Нет интернета
    DeserializationError,      // Ошибка десериализации JSON
    Unauthorized,              // 401 Unauthorized
    Unknown                    // Неизвестная ошибка
}
```

### JwtHandler

Обработчик для автоматического добавления JWT токена.

**Файл**: `Services/Core/Infrastructure/Network/JwtHandler.cs`

```csharp
public class JwtHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    public JwtHandler(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Получить токен из хранилища
        var token = _tokenStore.GetToken();

        // Добавить в заголовки если есть
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### SecureTokenStore

Безопасное хранилище для JWT токенов.

**Файл**: `Services/Core/Infrastructure/Network/SecureTokenStore.cs`

```csharp
public interface ITokenStore
{
    /// <summary>
    /// Сохранить токен в защищённое хранилище
    /// </summary>
    void SaveToken(string token);

    /// <summary>
    /// Получить токен из хранилища
    /// </summary>
    string? GetToken();

    /// <summary>
    /// Удалить токен из хранилища
    /// </summary>
    void RemoveToken();

    /// <summary>
    /// Проверить, есть ли токен в хранилище
    /// </summary>
    bool HasToken { get; }
}

public class SecureTokenStore : ITokenStore
{
    private const string TokenFileName = "secure_token.dat";
    private static readonly string StoragePath = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Runia",
            "SecureTokens");

    public void SaveToken(string token)
    {
        // Шифрование с использованием DPAPI
        byte[] dataToEncrypt = Encoding.UTF8.GetBytes(token);
        byte[] encryptedData = ProtectedData.Protect(
            dataToEncrypt,
            null,
            DataProtectionScope.CurrentUser);

        Directory.CreateDirectory(StoragePath);
        File.WriteAllBytes(
            Path.Combine(StoragePath, TokenFileName),
            encryptedData);
    }

    public string? GetToken()
    {
        var filePath = Path.Combine(StoragePath, TokenFileName);

        if (!File.Exists(filePath))
            return null;

        try
        {
            byte[] encryptedData = File.ReadAllBytes(filePath);
            byte[] decryptedData = ProtectedData.Unprotect(
                encryptedData,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decryptedData);
        }
        catch
        {
            return null;
        }
    }

    public void RemoveToken()
    {
        var filePath = Path.Combine(StoragePath, TokenFileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public bool HasToken
    {
        get
        {
            var filePath = Path.Combine(StoragePath, TokenFileName);
            return File.Exists(filePath);
        }
    }
}
```

## 🔄 Типичные API операции

### 1. POST запрос с аутентификацией

**Пример**: Регистрация

```csharp
// Класс запроса
public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}

// Класс ответа
public class AuthResponse
{
    public User User { get; set; }
    public string Token { get; set; }
}

// Использование в AuthService
public async Task<Result<AuthResponse>> RegisterAsync(
    string email,
    string password,
    string confirmPassword)
{
    var request = new RegisterRequest
    {
        Email = email,
        Password = password,
        ConfirmPassword = confirmPassword
    };

    var result = await _apiService.PostAsync<RegisterRequest, AuthResponse>(
        "api/auth/register",
        request);

    if (result.IsSuccess && !string.IsNullOrEmpty(result.Data?.Token))
    {
        // Сохранить токен
        _tokenStore.SaveToken(result.Data.Token);

        // Сохранить пользователя
        _currentUser = result.Data.User;
    }

    return result;
}
```

### 2. GET запрос с пагинацией

**Пример**: Получить список серверов

```csharp
public class ServerListRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ServerListResponse
{
    public List<Server> Servers { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
}

// Использование
public async Task<Result<ServerListResponse>> GetServersAsync(int page = 1)
{
    var endpoint = $"api/servers?page={page}&pageSize=10";
    return await _apiService.GetAsync<ServerListResponse>(endpoint);
}
```

### 3. PUT запрос для обновления

**Пример**: Обновить профиль

```csharp
public class UpdateProfileRequest
{
    public string Nickname { get; set; }
    public string Avatar { get; set; }
}

public async Task<Result<User>> UpdateProfileAsync(UpdateProfileRequest request)
{
    return await _apiService.PutAsync<UpdateProfileRequest, User>(
        "api/users/profile",
        request);
}
```

### 4. DELETE запрос

**Пример**: Удалить аккаунт

```csharp
public async Task<Result<bool>> DeleteAccountAsync()
{
    var result = await _apiService.DeleteAsync("api/users/account");

    if (result.IsSuccess)
    {
        // Удалить токен
        _tokenStore.RemoveToken();
    }

    return result;
}
```

## 📡 Обработка ошибок

### Иерархия обработки ошибок

```
HTTP Request
    ↓
Response Received
    ↓
    ├─ 2xx Success
    │  └─ Result<T>.Success()
    │
    ├─ 4xx Client Error
    │  ├─ 401 Unauthorized
    │  │  └─ NetworkError.Unauthorized
    │  ├─ 400 Bad Request
    │  │  └─ NetworkError.HttpError
    │  └─ 404 Not Found
    │     └─ NetworkError.HttpError
    │
    ├─ 5xx Server Error
    │  └─ NetworkError.HttpError
    │
    └─ Network/Other Error
       ├─ Timeout
       │  └─ NetworkError.Timeout
       ├─ No Connection
       │  └─ NetworkError.NetworkUnavailable
       └─ Unknown
          └─ NetworkError.Unknown
```

### Обработка ошибок в коде

```csharp
// В Service
public async Task<Result<LoginResponse>> LoginAsync(string email, string password)
{
    try
    {
        var request = new LoginRequest { Email = email, Password = password };
        var result = await _apiService.PostAsync<LoginRequest, LoginResponse>(
            "api/auth/login",
            request);

        if (!result.IsSuccess)
        {
            return result; // Пробросить ошибку
        }

        if (result.Data?.Token != null)
        {
            _tokenStore.SaveToken(result.Data.Token);
        }

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login failed");
        return Result<LoginResponse>.Failure(
            NetworkError.Unknown,
            "Ошибка при входе");
    }
}

// В ViewModel
[RelayCommand]
private async Task Login()
{
    var result = await _auth.LoginAsync(Email, Password);

    if (result.IsSuccess)
    {
        _nav.Navigate<MainWindow>();
    }
    else
    {
        var message = result.Error switch
        {
            NetworkError.Unauthorized => "Неправильные учётные данные",
            NetworkError.NetworkUnavailable => "Нет подключения к интернету",
            NetworkError.Timeout => "Истёк timeout запроса",
            _ => $"Ошибка: {result.ErrorMessage}"
        };

        await _dialog.ShowErrorAsync(message);
    }
}
```

## ⚙️ Конфигурация сетевого слоя

### App.xaml.cs - Регистрация

```csharp
services.AddHttpClient<BaseApiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IAppConfig>();

    // Установить базовый адрес
    client.BaseAddress = new Uri(config.ApiBaseUrl);

    // Установить timeout
    client.Timeout = TimeSpan.FromSeconds(30);

    // Установить стандартные заголовки
    client.DefaultRequestHeaders.Add("User-Agent", "Runia/1.0");
})
.AddHttpMessageHandler<JwtHandler>();  // Добавить JwtHandler

// Регистрация хранилища токенов
services.AddSingleton<ITokenStore, SecureTokenStore>();

// Регистрация JwtHandler
services.AddTransient<JwtHandler>();
```

### DefaultAppConfig - Конфигурация

**Файл**: `Services/Core/Infrastructure/Configuration/DefaultAppConfig.cs`

```csharp
public interface IAppConfig
{
    string ApiBaseUrl { get; }
    string Version { get; }
    string Environment { get; }
}

public class DefaultAppConfig : IAppConfig
{
    // Для локальной разработки
    public string ApiBaseUrl => "http://localhost:5000";

    // Для продакшена
    // public string ApiBaseUrl => "https://api.yourdomain.com";

    public string Version => "1.0.0";
    public string Environment => "Development";
}
```

## 🔐 SSL/HTTPS конфигурация

### Для локальной разработки (НЕ ДЛЯ ПРОДАКШЕНА!)

```csharp
// Временное отключение проверки сертификата
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = 
    (message, cert, chain, errors) => true;

services.AddHttpClient<BaseApiService>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiUrl))
    .ConfigurePrimaryHttpMessageHandler(() => handler);
```

### Для продакшена

```csharp
// Используется стандартная проверка сертификата
// HttpClientHandler автоматически проверяет SSL сертификат
```

## 📊 Мониторинг запросов

### Логирование HTTP запросов

```csharp
// В BaseApiService.cs
private async Task<Result<T>> SendRequestAsync<T>(
    Func<Task<HttpResponseMessage>> requestFunc,
    string endpoint)
{
    try
    {
        _logger.LogInformation("Sending request to: {endpoint}", endpoint);

        var response = await requestFunc();

        _logger.LogInformation(
            "Response: {statusCode} from {endpoint}",
            response.StatusCode,
            endpoint);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<T>(content);
            return Result<T>.Success(data);
        }

        return HandleErrorResponse<T>(response);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP request failed");
        return Result<T>.Failure(NetworkError.NetworkUnavailable);
    }
    catch (TaskCanceledException ex)
    {
        _logger.LogError(ex, "Request timeout");
        return Result<T>.Failure(NetworkError.Timeout);
    }
}
```

## 🧪 Тестирование API интеграции

### Mock API для тестирования

```csharp
[Test]
public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
{
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(
                JsonSerializer.Serialize(new { token = "jwt_token_here" }))
        });

    var client = new HttpClient(mockHandler.Object);
    var service = new BaseApiService(client);

    // Act
    var result = await service.PostAsync<LoginRequest, AuthResponse>(
        "api/auth/login",
        new LoginRequest { Email = "test@test.com", Password = "password" });

    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual("jwt_token_here", result.Data.Token);
}
```

## 📝 API документация

### Endpoints структура

```
Aутентификация:
POST   /api/auth/register                - Регистрация
POST   /api/auth/login                   - Вход
POST   /api/auth/confirm-email           - Подтверждение email
POST   /api/auth/forgot-password         - Забыли пароль
POST   /api/auth/reset-password          - Сброс пароля
POST   /api/auth/refresh-token           - Обновление токена
POST   /api/auth/logout                  - Выход

Серверы:
GET    /api/servers                      - Список серверов
GET    /api/servers/{id}                 - Детали сервера
POST   /api/servers                      - Создать сервер
PUT    /api/servers/{id}                 - Обновить сервер
DELETE /api/servers/{id}                 - Удалить сервер

Пользователь:
GET    /api/users/profile                - Профиль
PUT    /api/users/profile                - Обновить профиль
DELETE /api/users/account                - Удалить аккаунт
```

## 🔗 Связанные документы

- [Архитектура](./ARCHITECTURE.md)
- [Система аутентификации](./AUTHENTICATION.md)
- [Сервисы и инфраструктура](./SERVICES.md)
- [API для разработчиков](./API_REFERENCE.md)

---

**Следующий шаг**: Изучите [Сервисы и инфраструктура](./SERVICES.md) для понимания высокоуровневых сервисов.
