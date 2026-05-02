# ⚙️ Сервисы и инфраструктура

Документация по сервисам приложения и инфраструктурным компонентам.

## 📋 Обзор

Слой сервисов содержит:
- **Бизнес-логика**: Основная функциональность приложения
- **Интеграция**: Координация между компонентами
- **Конфигурация**: Управление параметрами приложения
- **Инфраструктура**: Низкоуровневые операции

## 🏗️ Структура сервисов

```
Services/
├── Auth/                              # Аутентификация
│   ├── AuthService.cs                # Основной сервис
│   └── AuthManager.cs                # Менеджер состояния
│
├── Common/                            # Общие сервисы
│   ├── DialogService.cs              # Диалоги
│   ├── IDialogService.cs
│   ├── ImageManager.cs               # Управление изображениями
│   ├── ImageProvider.cs              # Провайдер изображений
│   ├── NewsService.cs                # Работа с новостями
│   └── ThemeManager.cs               # Управление темами
│
└── Core/                              # Ядро инфраструктуры
    ├── Infrastructure/
    │   ├── Configuration/
    │   │   ├── DefaultAppConfig.cs
    │   │   └── IAppConfig.cs
    │   ├── Constants/
    │   │   └── AppStatic.cs
    │   ├── Network/
    │   │   ├── BaseApiService.cs
    │   │   ├── ImageDownloader.cs
    │   │   ├── JwtHandler.cs
    │   │   ├── SecureTokenStore.cs
    │   │   └── ITokenStore.cs
    │   ├── Persistence/
    │   │   └── DataManager.cs
    │   ├── LocalDataService.cs
    │   └── WPF/
    │       └── Behaviors/
    │           └── PasswordBoxAttachmentBehavior.cs
    ├── Navigation/
    │   ├── INavigationService.cs
    │   └── NavigationService.cs
    └── Launcher/
        ├── LauncherService.cs
        └── VersionsManager.cs
```

## 🔑 Ключевые сервисы

### 1. AuthService - Аутентификация

**Файл**: `Services/Auth/AuthService.cs`

Основной сервис для управления аутентификацией пользователя.

```csharp
public class AuthService
{
    private readonly BaseApiService _apiService;
    private readonly ITokenStore _tokenStore;
    private User? _currentUser;

    // Регистрация нового пользователя
    public async Task<Result<AuthResponse>> RegisterAsync(
        string email,
        string password,
        string confirmPassword)
    {
        // Валидация
        if (!IsValidEmail(email))
            return Result<AuthResponse>.Failure(
                AuthError.InvalidEmail,
                "Некорректный email");

        if (password != confirmPassword)
            return Result<AuthResponse>.Failure(
                AuthError.PasswordMismatch,
                "Пароли не совпадают");

        // Отправить запрос
        var request = new RegisterRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        var result = await _apiService.PostAsync<RegisterRequest, AuthResponse>(
            "api/auth/register",
            request);

        // Обработать результат
        if (result.IsSuccess && result.Data != null)
        {
            _currentUser = result.Data.User;
        }

        return result;
    }

    // Подтверждение email
    public async Task<Result<bool>> ConfirmEmailAsync(
        string email,
        string confirmationCode)
    {
        var request = new ConfirmEmailRequest
        {
            Email = email,
            ConfirmationCode = confirmationCode
        };

        return await _apiService.PostAsync<ConfirmEmailRequest, bool>(
            "api/auth/confirm-email",
            request);
    }

    // Вход
    public async Task<Result<AuthResponse>> LoginAsync(
        string email,
        string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var result = await _apiService.PostAsync<LoginRequest, AuthResponse>(
            "api/auth/login",
            request);

        if (result.IsSuccess && result.Data?.Token != null)
        {
            _currentUser = result.Data.User;
            _tokenStore.SaveToken(result.Data.Token);
        }

        return result;
    }

    // Выход
    public async Task LogoutAsync()
    {
        _currentUser = null;
        _tokenStore.RemoveToken();

        // Уведомить API (опционально)
        await _apiService.PostAsync<object, bool>("api/auth/logout", new { });
    }

    // Получить текущего пользователя
    public User? GetCurrentUser() => _currentUser;

    // Проверить, авторизован ли пользователь
    public bool IsAuthenticated => _currentUser != null && _tokenStore.HasToken;
}
```

### 2. DialogService - Диалоги

**Файл**: `Services/Common/DialogService.cs`

Сервис для показа диалогов пользователю.

```csharp
public interface IDialogService
{
    Task ShowInfoAsync(string message, string title = "Информация");
    Task ShowErrorAsync(string message, string title = "Ошибка");
    Task<bool> ShowConfirmAsync(string message, string title = "Подтверждение");
    Task<string?> ShowInputAsync(string prompt, string title = "Ввод");
}

public class DialogService : IDialogService
{
    public async Task ShowInfoAsync(string message, string title = "Информация")
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    public async Task ShowErrorAsync(string message, string title = "Ошибка")
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    public async Task<bool> ShowConfirmAsync(string message, string title = "Подтверждение")
    {
        var result = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var res = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return res == MessageBoxResult.Yes;
        });

        return result;
    }

    public async Task<string?> ShowInputAsync(string prompt, string title = "Ввод")
    {
        // Реализация с кастомным окном ввода
        // или использование встроенного диалога
        return await Task.FromResult<string?>(null);
    }
}
```

### 3. ThemeManager - Управление темами

**Файл**: `Services/Common/ThemeManager.cs`

Сервис для переключения между светлой и тёмной темами.

```csharp
public class ThemeManager
{
    public enum Theme
    {
        Light,
        Dark
    }

    private Theme _currentTheme = Theme.Light;

    public Theme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            ApplyTheme(value);
        }
    }

    private void ApplyTheme(Theme theme)
    {
        var themeName = theme == Theme.Light ? "LightTheme" : "DarkTheme";
        var uri = new Uri($"assets/Themes/{themeName}.xaml", UriKind.Relative);

        // Загрузить ресурсы темы
        var resourceDictionary = new ResourceDictionary { Source = uri };

        // Применить тему
        var existingResourceDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme") == true);

        if (existingResourceDict != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(existingResourceDict);
        }

        Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
    }

    public void Toggle()
    {
        CurrentTheme = CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light;
    }
}
```

### 4. ImageManager - Управление изображениями

**Файл**: `Services/Common/ImageManager.cs`

Кэширование и управление изображениями.

```csharp
public class ImageManager
{
    private readonly Dictionary<string, BitmapImage> _cache = new();
    private readonly ImageDownloader _downloader;
    private const int CacheSize = 50; // Максимум изображений в кэше

    public ImageManager(ImageDownloader downloader)
    {
        _downloader = downloader;
    }

    public async Task<BitmapImage?> GetImageAsync(string url)
    {
        // Проверить кэш
        if (_cache.TryGetValue(url, out var cachedImage))
        {
            return cachedImage;
        }

        try
        {
            // Скачать изображение
            var bytes = await _downloader.DownloadImageAsync(url);

            if (bytes == null)
                return null;

            // Конвертировать в BitmapImage
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            // Добавить в кэш
            if (_cache.Count >= CacheSize)
            {
                var oldestKey = _cache.Keys.First();
                _cache.Remove(oldestKey);
            }

            _cache[url] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
```

### 5. ImageDownloader - Скачивание изображений

**Файл**: `Services/Core/Infrastructure/Network/ImageDownloader.cs`

```csharp
public class ImageDownloader
{
    private readonly HttpClient _client;

    public ImageDownloader(HttpClient client)
    {
        _client = client;
    }

    public async Task<byte[]?> DownloadImageAsync(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }
}
```

### 6. LocalDataService - Локальное хранилище

**Файл**: `Services/Core/Infrastructure/LocalDataService.cs`

```csharp
public interface ILocalDataService
{
    void SaveData<T>(string key, T data);
    T? LoadData<T>(string key);
    void DeleteData(string key);
    void ClearAll();
}

public class LocalDataService : ILocalDataService
{
    private readonly string _dataPath = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Runia",
            "Data");

    public void SaveData<T>(string key, T data)
    {
        Directory.CreateDirectory(_dataPath);
        var path = Path.Combine(_dataPath, $"{key}.json");

        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(path, json);
    }

    public T? LoadData<T>(string key)
    {
        var path = Path.Combine(_dataPath, $"{key}.json");

        if (!File.Exists(path))
            return default;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    public void DeleteData(string key)
    {
        var path = Path.Combine(_dataPath, $"{key}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    public void ClearAll()
    {
        if (Directory.Exists(_dataPath))
            Directory.Delete(_dataPath, true);
    }
}
```

### 7. DataManager - Управление данными

**Файл**: `Services/Core/Infrastructure/Persistence/DataManager.cs`

```csharp
public class DataManager
{
    private readonly ILocalDataService _localStorage;
    private readonly BaseApiService _apiService;

    // Кэш данных
    private Dictionary<string, object?> _cache = new();

    public async Task<T?> GetDataAsync<T>(string key, string apiEndpoint)
    {
        // Проверить локальный кэш
        if (_cache.TryGetValue(key, out var cachedData))
        {
            return (T?)cachedData;
        }

        try
        {
            // Загрузить из API
            var result = await _apiService.GetAsync<T>(apiEndpoint);

            if (result.IsSuccess)
            {
                // Сохранить в кэш
                _cache[key] = result.Data;

                // Сохранить локально
                _localStorage.SaveData(key, result.Data);

                return result.Data;
            }
        }
        catch
        {
            // Если ошибка, загрузить из локального хранилища
        }

        // Загрузить из локального хранилища как fallback
        return _localStorage.LoadData<T>(key);
    }

    public void InvalidateCache(string key)
    {
        _cache.Remove(key);
    }

    public void InvalidateAllCache()
    {
        _cache.Clear();
    }
}
```

### 8. NavigationService - Навигация

**Файл**: `Services/Core/Navigation/NavigationService.cs`

```csharp
public interface INavigationService
{
    void Navigate<T>() where T : Window;
    void Navigate<T>(object? parameter) where T : Window;
    void Navigate(Type windowType);
    event EventHandler<NavigatedEventArgs>? Navigated;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDialogService _dialogService;

    public event EventHandler<NavigatedEventArgs>? Navigated;

    public NavigationService(
        IServiceProvider serviceProvider,
        IDialogService dialogService)
    {
        _serviceProvider = serviceProvider;
        _dialogService = dialogService;
    }

    public void Navigate<T>() where T : Window
    {
        Navigate(typeof(T));
    }

    public void Navigate<T>(object? parameter) where T : Window
    {
        // Передать параметр если нужно
        Navigate(typeof(T));
    }

    public void Navigate(Type windowType)
    {
        try
        {
            var window = _serviceProvider.GetService(windowType) as Window;

            if (window == null)
            {
                _dialogService.ShowErrorAsync("Не удалось открыть окно").Wait();
                return;
            }

            // Закрыть текущее окно если нужно
            var current = Application.Current?.MainWindow;
            if (current != null && current.GetType() != windowType)
            {
                current.Close();
            }

            window.Show();

            // Уведомить подписчиков
            Navigated?.Invoke(this, new NavigatedEventArgs { TargetType = windowType });
        }
        catch (Exception ex)
        {
            _dialogService.ShowErrorAsync($"Ошибка навигации: {ex.Message}").Wait();
        }
    }
}

public class NavigatedEventArgs : EventArgs
{
    public Type? TargetType { get; set; }
}
```

### 9. AppStatic - Статические константы

**Файл**: `Services/Core/Infrastructure/Constants/AppStatic.cs`

```csharp
public static class AppStatic
{
    // Версия приложения
    public const string AppVersion = "1.0.0";

    // Названия
    public const string AppName = "Runia";
    public const string AppDescription = "Game Server Manager";

    // Timeout значения (в секундах)
    public const int HttpRequestTimeout = 30;
    public const int ImageDownloadTimeout = 15;

    // Пути
    public static string AppDataPath => 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);

    // Требования к паролю
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    // Требования к email
    public const int MaxEmailLength = 254;
}
```

### 10. PasswordBoxAttachmentBehavior - Поведение PasswordBox

**Файл**: `Services/Core/Infrastructure/WPF/Behaviors/PasswordBoxAttachmentBehavior.cs`

Позволяет привязать пароль к ViewModel (не рекомендуется, но иногда необходимо).

```csharp
public static class PasswordBoxAttachmentBehavior
{
    public static string GetPassword(DependencyObject obj)
    {
        return (string)obj.GetValue(PasswordProperty);
    }

    public static void SetPassword(DependencyObject obj, string value)
    {
        obj.SetValue(PasswordProperty, value);
    }

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxAttachmentBehavior),
            new PropertyMetadata("", OnPasswordChanged));

    private static void OnPasswordChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox)
        {
            passwordBox.Password = e.NewValue as string ?? string.Empty;
        }
    }
}

// Использование в XAML:
// <PasswordBox local:PasswordBoxAttachmentBehavior.Password="{Binding Password}" />
```

## 📊 Диаграмма зависимостей сервисов

```
ViewModels
    │
    ├─→ AuthService
    ├─→ DialogService
    ├─→ NavigationService
    ├─→ ImageManager
    ├─→ ThemeManager
    └─→ NewsService
        │
        ├─→ BaseApiService
        ├─→ DataManager
        ├─→ LocalDataService
        └─→ ImageDownloader
            │
            ├─→ HttpClient
            └─→ ITokenStore (SecureTokenStore)
                │
                ├─→ JwtHandler
                └─→ DPAPI Encryption
```

## ⚙️ Регистрация сервисов в DI

**Файл**: `App.xaml.cs`

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    _host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            // Конфигурация
            services.AddSingleton<IAppConfig, DefaultAppConfig>();

            // Диалоги и навигация
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Локальное хранилище
            services.AddSingleton<ILocalDataService, LocalDataService>();
            services.AddSingleton<DataManager>();

            // Управление изображениями
            services.AddSingleton<ImageManager>();
            services.AddTransient<ImageDownloader>();

            // Управление темами
            services.AddSingleton<ThemeManager>();

            // Сервисы
            services.AddSingleton<NewsService>();
            services.AddSingleton<AuthService>();

            // HTTP клиент
            services.AddTransient<JwtHandler>();
            services.AddHttpClient<BaseApiService>((sp, c) =>
            {
                var cfg = sp.GetRequiredService<IAppConfig>();
                c.BaseAddress = new Uri(cfg.ApiBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(AppStatic.HttpRequestTimeout);
            }).AddHttpMessageHandler<JwtHandler>();

            // Токены
            services.AddSingleton<ITokenStore, SecureTokenStore>();

            // Views и ViewModels
            services.AddSingleton<LoginWindow>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<LoginWindowViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            // ... остальные страницы и ViewModels
        })
        .Build();

    Services = _host.Services;

    var loginWindow = Services.GetRequiredService<LoginWindow>();
    loginWindow.Show();
}
```

## 🔗 Связанные документы

- [Архитектура](./ARCHITECTURE.md)
- [Сетевое взаимодействие](./NETWORK.md)
- [API для разработчиков](./API_REFERENCE.md)

---

**Следующий шаг**: Изучите [UI/UX](./UI_UX.md) для создания интерфейсов.
