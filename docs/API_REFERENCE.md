# 👨‍💻 API для разработчиков Runia

Справочник по основным компонентам, интерфейсам и классам для разработки на основе Runia.

## 📋 Обзор

Этот документ содержит справку по основным API и классам, которые используются при разработке.

## 🎯 Основные интерфейсы

### IAppConfig - Конфигурация приложения

```csharp
public interface IAppConfig
{
    /// <summary>
    /// Базовый URL API сервера
    /// </summary>
    string ApiBaseUrl { get; }

    /// <summary>
    /// Версия приложения
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Окружение (Development, Production, etc.)
    /// </summary>
    string Environment { get; }
}
```

**Реализация**: `DefaultAppConfig`

**Использование**:
```csharp
public class MyService
{
    public MyService(IAppConfig config)
    {
        var apiUrl = config.ApiBaseUrl; // "https://api.example.com"
    }
}
```

---

### INavigationService - Навигация

```csharp
public interface INavigationService
{
    /// <summary>
    /// Перейти на новое окно без параметров
    /// </summary>
    void Navigate<T>() where T : Window;

    /// <summary>
    /// Перейти на новое окно с параметром
    /// </summary>
    void Navigate<T>(object? parameter) where T : Window;

    /// <summary>
    /// Перейти по типу окна
    /// </summary>
    void Navigate(Type windowType);

    /// <summary>
    /// Событие при успешной навигации
    /// </summary>
    event EventHandler<NavigatedEventArgs>? Navigated;
}
```

**Реализация**: `NavigationService`

**Использование**:
```csharp
_navigationService.Navigate<MainWindow>();
_navigationService.Navigate<LoginWindow>(userParameter);
```

---

### IDialogService - Диалоги

```csharp
public interface IDialogService
{
    /// <summary>
    /// Показать информационное сообщение
    /// </summary>
    Task ShowInfoAsync(string message, string title = "Информация");

    /// <summary>
    /// Показать сообщение об ошибке
    /// </summary>
    Task ShowErrorAsync(string message, string title = "Ошибка");

    /// <summary>
    /// Показать диалог подтверждения
    /// </summary>
    Task<bool> ShowConfirmAsync(string message, string title = "Подтверждение");

    /// <summary>
    /// Показать диалог ввода
    /// </summary>
    Task<string?> ShowInputAsync(string prompt, string title = "Ввод");
}
```

**Реализация**: `DialogService`

**Использование**:
```csharp
await _dialog.ShowInfoAsync("Операция успешна!");
await _dialog.ShowErrorAsync("Произошла ошибка");
var confirmed = await _dialog.ShowConfirmAsync("Вы уверены?");
```

---

### ITokenStore - Хранилище токенов

```csharp
public interface ITokenStore
{
    /// <summary>
    /// Сохранить JWT токен
    /// </summary>
    void SaveToken(string token);

    /// <summary>
    /// Получить сохранённый токен
    /// </summary>
    string? GetToken();

    /// <summary>
    /// Удалить токен
    /// </summary>
    void RemoveToken();

    /// <summary>
    /// Проверить наличие токена
    /// </summary>
    bool HasToken { get; }
}
```

**Реализация**: `SecureTokenStore` (использует DPAPI)

**Использование**:
```csharp
_tokenStore.SaveToken(jwtToken);
var token = _tokenStore.GetToken();
if (_tokenStore.HasToken)
{
    // Пользователь авторизован
}
```

---

### ILocalDataService - Локальное хранилище

```csharp
public interface ILocalDataService
{
    /// <summary>
    /// Сохранить данные в JSON файл
    /// </summary>
    void SaveData<T>(string key, T data);

    /// <summary>
    /// Загрузить данные из JSON файла
    /// </summary>
    T? LoadData<T>(string key);

    /// <summary>
    /// Удалить данные
    /// </summary>
    void DeleteData(string key);

    /// <summary>
    /// Очистить все данные
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Проверить существование данных
    /// </summary>
    bool DataExists(string key);
}
```

**Реализация**: `LocalDataService`

**Использование**:
```csharp
_localStorage.SaveData("user_profile", userProfile);
var profile = _localStorage.LoadData<User>("user_profile");
```

---

## 🌐 Сетевые классы

### BaseApiService - HTTP клиент

```csharp
public sealed class BaseApiService
{
    /// <summary>
    /// GET запрос
    /// </summary>
    public async Task<Result<T>> GetAsync<T>(string endpoint)

    /// <summary>
    /// POST запрос
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

**Использование**:
```csharp
// GET
var result = await _apiService.GetAsync<User>("api/users/1");

// POST
var loginResponse = await _apiService.PostAsync<LoginRequest, AuthResponse>(
    "api/auth/login",
    new LoginRequest { Email = "user@test.com", Password = "pass123" }
);

// PUT
var updateResponse = await _apiService.PutAsync<UpdateProfileRequest, User>(
    "api/users/profile",
    new UpdateProfileRequest { Nickname = "NewName" }
);

// DELETE
var deleteResult = await _apiService.DeleteAsync("api/users/1");
```

---

### Result<T> - Результат операции

```csharp
public sealed class Result<T>
{
    /// <summary>
    /// Успешна ли операция
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Данные результата
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Тип ошибки
    /// </summary>
    public NetworkError Error { get; }

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    public string? ErrorMessage { get; }

    // Фабричные методы
    public static Result<T> Success(T data);
    public static Result<T> Failure(NetworkError error, string? message = null);
}

public enum NetworkError
{
    None,
    HttpError,
    Timeout,
    NetworkUnavailable,
    DeserializationError,
    Unauthorized,
    Unknown
}
```

**Использование**:
```csharp
var result = await _apiService.GetAsync<User>("api/users/1");

if (result.IsSuccess)
{
    var user = result.Data;
    Console.WriteLine(user.Name);
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

---

## 🔐 Сервисы аутентификации

### AuthService

```csharp
public class AuthService
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public async Task<Result<AuthResponse>> RegisterAsync(
        string email,
        string password,
        string confirmPassword)

    /// <summary>
    /// Вход в аккаунт
    /// </summary>
    public async Task<Result<AuthResponse>> LoginAsync(
        string email,
        string password)

    /// <summary>
    /// Подтверждение email
    /// </summary>
    public async Task<Result<bool>> ConfirmEmailAsync(
        string email,
        string confirmationCode)

    /// <summary>
    /// Забыли пароль
    /// </summary>
    public async Task<Result<bool>> ForgotPasswordAsync(string email)

    /// <summary>
    /// Сброс пароля
    /// </summary>
    public async Task<Result<bool>> ResetPasswordAsync(
        string email,
        string resetCode,
        string newPassword)

    /// <summary>
    /// Выход из аккаунта
    /// </summary>
    public async Task LogoutAsync()

    /// <summary>
    /// Получить текущего пользователя
    /// </summary>
    public User? GetCurrentUser()

    /// <summary>
    /// Проверить, авторизован ли пользователь
    /// </summary>
    public bool IsAuthenticated { get; }
}
```

**Использование**:
```csharp
var registerResult = await _auth.RegisterAsync(
    "user@example.com",
    "Password123!",
    "Password123!"
);

if (registerResult.IsSuccess)
{
    // Перейти на страницу подтверждения email
}

var loginResult = await _auth.LoginAsync("user@example.com", "Password123!");

if (loginResult.IsSuccess)
{
    // Перейти на главное окно
}

await _auth.LogoutAsync();
```

---

## 🎨 Сервисы пользовательского интерфейса

### ThemeManager - Управление темами

```csharp
public class ThemeManager
{
    public enum Theme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Текущая тема
    /// </summary>
    public Theme CurrentTheme { get; set; }

    /// <summary>
    /// Переключить тему
    /// </summary>
    public void Toggle()
}
```

**Использование**:
```csharp
_themeManager.CurrentTheme = ThemeManager.Theme.Dark;
_themeManager.Toggle(); // Переключить на светлую
```

---

### ImageManager - Управление изображениями

```csharp
public class ImageManager
{
    /// <summary>
    /// Получить изображение с кэшированием
    /// </summary>
    public async Task<BitmapImage?> GetImageAsync(string url)

    /// <summary>
    /// Очистить кэш
    /// </summary>
    public void ClearCache()
}
```

**Использование**:
```csharp
var image = await _imageManager.GetImageAsync("https://example.com/image.jpg");
if (image != null)
{
    myImageControl.Source = image;
}
```

---

## 📦 Установщики

### ForgeManager

```csharp
public class ForgeManager
{
    /// <summary>
    /// Установить Forge
    /// </summary>
    public async Task<Result<bool>> InstallForgeAsync(
        string version,
        string installPath)

    /// <summary>
    /// Удалить Forge
    /// </summary>
    public async Task<Result<bool>> UninstallForgeAsync(string version)

    /// <summary>
    /// Получить установленные версии
    /// </summary>
    public List<ForgeInstallation> GetInstalledVersions()
}
```

---

### JavaManager

```csharp
public class JavaManager
{
    /// <summary>
    /// Проверить установку Java
    /// </summary>
    public static bool IsJavaInstalled()

    /// <summary>
    /// Получить версию Java
    /// </summary>
    public static string? GetJavaVersion()

    /// <summary>
    /// Установить Java
    /// </summary>
    public async Task<Result<bool>> InstallJavaAsync(
        string version,
        string installPath)
}
```

---

### ModPackManager

```csharp
public class ModPackManager
{
    /// <summary>
    /// Получить доступные модпаки
    /// </summary>
    public async Task<Result<List<ModPack>>> GetAvailableModPacksAsync()

    /// <summary>
    /// Установить модпак
    /// </summary>
    public async Task<Result<bool>> InstallModPackAsync(
        string modpackId,
        string installPath)

    /// <summary>
    /// Удалить модпак
    /// </summary>
    public async Task<Result<bool>> UninstallModPackAsync(string modpackId)

    /// <summary>
    /// Обновить модпак
    /// </summary>
    public async Task<Result<bool>> UpdateModPackAsync(string modpackId)
}
```

---

## 📊 Модели данных

### User

```csharp
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Nickname { get; set; }
    public string? Avatar { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
```

---

### AuthResponse

```csharp
public class AuthResponse
{
    public User User { get; set; }
    public string Token { get; set; }  // JWT токен
    public DateTime ExpiresAt { get; set; }
}
```

---

### Server

```csharp
public class Server
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public int Players { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; }  // "online", "offline"
}
```

---

## 🔧 MVVM Компоненты

### ObservableObject

Базовый класс для MVVM (из MVVM Community Toolkit):

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [RelayCommand]
    private async Task LoadData()
    {
        IsLoading = true;
        // Логика загрузки
        IsLoading = false;
    }
}
```

---

### RelayCommand

Команда для XAML binding:

```csharp
[RelayCommand]
private void MyCommand()
{
    // Логика команды
}

// Может быть использована в XAML:
// <Button Command="{Binding MyCommandCommand}" />
```

---

## 🧪 Примеры использования

### Пример 1: Создание нового ViewModel

```csharp
public partial class MyViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;
    private readonly AuthService _auth;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public MyViewModel(
        INavigationService nav,
        IDialogService dialog,
        AuthService auth)
    {
        _nav = nav;
        _dialog = dialog;
        _auth = auth;
    }

    [RelayCommand]
    private async Task Login()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        var result = await _auth.LoginAsync(Email, Password);

        if (result.IsSuccess)
        {
            await _dialog.ShowInfoAsync("Добро пожаловать!");
            _nav.Navigate<MainWindow>();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Ошибка входа";
        }

        IsLoading = false;
    }
}
```

---

### Пример 2: Использование DataManager

```csharp
public partial class ServerViewModel : ObservableObject
{
    private readonly DataManager _dataManager;

    [ObservableProperty]
    private List<Server>? servers;

    public ServerViewModel(DataManager dataManager)
    {
        _dataManager = dataManager;
    }

    [RelayCommand]
    private async Task LoadServers()
    {
        Servers = await _dataManager.GetDataAsync<List<Server>>(
            key: "servers_list",
            apiEndpoint: "api/servers"
        );
    }

    [RelayCommand]
    private void RefreshServers()
    {
        _dataManager.InvalidateCache("servers_list");
        LoadServersCommand.Execute(null);
    }
}
```

---

## 🔗 Связанные документы

- [Архитектура](./ARCHITECTURE.md)
- [Сетевое взаимодействие](./NETWORK.md)
- [Сервисы и инфраструктура](./SERVICES.md)

---

**Примечание**: Все API следуют принципам SOLID и готовы к расширению.
