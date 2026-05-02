# 🏗️ Архитектура Runia

Этот документ описывает архитектурные решения, паттерны проектирования и структуру кода приложения Runia.

## 📐 Архитектурные паттерны

### MVVM (Model-View-ViewModel)

Runia использует **MVVM паттерн** с **MVVM Community Toolkit** для разделения логики представления от логики пользовательского интерфейса.

```
┌─────────────────────────────────────────────────┐
│                    UI Layer                      │
│  ┌──────────────────────────────────────────┐  │
│  │  View (XAML Pages/Windows)               │  │
│  │  - LoginPage.xaml                        │  │
│  │  - HomePage.xaml                        │  │
│  │  - etc.                                  │  │
│  └──────────────────────────────────────────┘  │
│                      ↕                           │
│  ┌──────────────────────────────────────────┐  │
│  │  ViewModel (C# Code-Behind Logic)       │  │
│  │  - LoginViewModel                       │  │
│  │  - HomeViewModel                        │  │
│  │  - Observable Properties                │  │
│  │  - Commands (ICommand)                  │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
              ↕ (Data Binding)
┌─────────────────────────────────────────────────┐
│               Business Logic Layer               │
│  ┌──────────────────────────────────────────┐  │
│  │  Services                                │  │
│  │  - AuthService                           │  │
│  │  - NavigationService                    │  │
│  │  - ThemeManager                          │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
              ↕
┌─────────────────────────────────────────────────┐
│             Infrastructure Layer                 │
│  ┌──────────────────────────────────────────┐  │
│  │  Network, Persistence, Configuration    │  │
│  │  - BaseApiService                        │  │
│  │  - SecureTokenStore                     │  │
│  │  - LocalDataService                     │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

### Dependency Injection (DI)

Приложение использует **Microsoft.Extensions.DependencyInjection** для управления зависимостями.

**Регистрация сервисов** происходит в `App.xaml.cs`:

```csharp
services.AddSingleton<IAppConfig, DefaultAppConfig>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<INavigationService, NavigationService>();
services.AddHttpClient<BaseApiService>();
```

### Repository Pattern

Для работы с данными используется паттерн Repository (через `DataManager` и `LocalDataService`).

## 📦 Структура слоёв

### 1. UI Layer (Ui/)

Содержит все элементы пользовательского интерфейса:

```
Ui/
├── Pages/
│   ├── Auth/                    # Страницы аутентификации
│   │   ├── LoginPage.xaml
│   │   ├── RegisterPage.xaml
│   │   ├── ForgotPasswordPage.xaml
│   │   ├── ResetPasswordPage.xaml
│   │   └── ConfirmEmailPage.xaml
│   ├── Main/                    # Основные страницы после входа
│   │   ├── HomePage.xaml
│   │   ├── ServerPage.xaml
│   │   ├── UserPage.xaml
│   │   ├── SettingsPage.xaml
│   │   └── TemplatePages/       # Детальные страницы
│   │       ├── TemplateNewsDetailsPage.xaml
│   │       └── TemplateServerDetailsPage.xaml
│   └── Controls/                # Переиспользуемые компоненты
│       └── SideBar.xaml
└── Windows/                     # Главные окна
    ├── LoginWindow.xaml
    ├── MainWindow.xaml
    └── MessageWindow.xaml
```

**Ответственность**: Отображение данных и обработка пользовательских действий

### 2. ViewModel Layer (ViewModels/)

Содержит логику представления и обработку команд:

```
ViewModels/
├── Auth/
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   ├── ForgotPasswordViewModel.cs
│   ├── ResetPasswordViewModel.cs
│   └── ConfirmEmailViewModel.cs
├── Main/
│   ├── HomeViewModel.cs
│   ├── ServerViewModel.cs
│   ├── UserViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── SidebarViewModel.cs
│   ├── NewsDetailsViewModel.cs
│   └── ServerDetailsViewModel.cs
└── Windows/
    ├── LoginWindowViewModel.cs
    └── MainWindowViewModel.cs
```

**Ответственность**: Управление состоянием, команды, валидация, бизнес-логика представления

### 3. Service Layer (Services/)

Содержит бизнес-логику и координацию:

```
Services/
├── Auth/
│   ├── AuthService.cs           # Основной сервис аутентификации
│   └── AuthManager.cs           # Менеджер для управления аутентификацией
├── Common/
│   ├── DialogService.cs         # Диалоги
│   ├── IDialogService.cs
│   ├── ImageManager.cs          # Управление изображениями
│   ├── ImageProvider.cs         # Получение изображений
│   ├── NewsService.cs           # Работа с новостями
│   └── ThemeManager.cs          # Управление темами
└── Core/
    ├── Infrastructure/
    │   ├── Configuration/
    │   │   ├── DefaultAppConfig.cs
    │   │   └── IAppConfig.cs
    │   ├── Constants/
    │   │   └── AppStatic.cs
    │   ├── Network/
    │   │   ├── BaseApiService.cs       # Базовый HTTP клиент
    │   │   ├── ImageDownloader.cs
    │   │   ├── JwtHandler.cs           # Обработчик JWT
    │   │   ├── SecureTokenStore.cs     # Хранилище токенов
    │   │   └── ITokenStore.cs
    │   ├── Persistence/
    │   │   └── DataManager.cs          # Управление данными
    │   ├── LocalDataService.cs
    │   └── WPF/
    │       └── Behaviors/
    │           └── PasswordBoxAttachmentBehavior.cs
    ├── Navigation/
    │   ├── INavigationService.cs
    │   └── NavigationService.cs        # Система навигации
    └── Launcher/
        ├── LauncherService.cs          # Запуск приложения
        └── VersionsManager.cs
└── Installers/
    ├── ForgeManager.cs
    ├── JavaManager.cs
    └── ModPackManager.cs
```

**Ответственность**: Реализация бизнес-правил, координация сервисов

### 4. Infrastructure Layer (Services/Core/Infrastructure/)

Низкоуровневые операции и интеграции:

- **Network**: HTTP запросы, API интеграция, JWT токены
- **Persistence**: Локальное хранилище, кэширование
- **Configuration**: Конфигурационные параметры
- **Constants**: Статические константы приложения

## 🔄 Поток данных

### Типичный сценарий: Вход пользователя

```
1. User Input
   └─→ LoginPage.xaml
       └─→ LoginViewModel.LoginCommand
           └─→ AuthService.LoginAsync()
               └─→ BaseApiService.PostAsync<LoginRequest, LoginResponse>()
                   └─→ API Server
                       └─→ Response with JWT Token
                           └─→ SecureTokenStore.SaveToken()
                               └─→ NavigationService.Navigate<MainWindow>()
                                   └─→ MainWindow Displayed
```

### Поток данных приложения

```
┌─────────────────────────────────────────────────────┐
│  User Interaction (Click, Input)                    │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  ViewModel Command Executed                          │
│  (LoginCommand, RegisterCommand, etc.)               │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  Service Method Called                               │
│  (AuthService.LoginAsync())                          │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  API Call (BaseApiService)                           │
│  - Prepare Request                                   │
│  - Add JWT Token (JwtHandler)                        │
│  - Send HTTP Request                                │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  Response Handling                                   │
│  - Deserialize JSON                                  │
│  - Handle Errors                                     │
│  - Save Token (if auth)                              │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  ViewModel Update                                    │
│  - Update Observable Properties                     │
│  - Raise PropertyChanged Events                     │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  UI Update (XAML Binding)                            │
│  - Data Binding Updates                              │
│  - UI Refresh                                        │
└─────────────────────────────────────────────────────┘
```

## 🎯 Ключевые компоненты

### App.xaml.cs - Инициализация приложения

```csharp
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // Регистрация сервисов
        services.AddSingleton<IAppConfig, DefaultAppConfig>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddHttpClient<BaseApiService>();

        Services = services.BuildServiceProvider();
    }
}
```

### Navigation Service - Управление переходами

```csharp
public interface INavigationService
{
    void Navigate<T>() where T : Window;
    void Navigate<T>(object? parameter) where T : Window;
    void Navigate(Type windowType);
}

public class NavigationService : INavigationService
{
    public void Navigate<T>() where T : Window
    {
        var window = App.Services.GetRequiredService<T>();
        window.Show();
    }
}
```

### BaseApiService - HTTP запросы

```csharp
public sealed class BaseApiService
{
    private readonly HttpClient _client;

    public async Task<Result<T>> PostAsync<TRequest, T>(
        string endpoint, 
        TRequest request)
    {
        // Логика HTTP запроса
    }
}
```

### Secure Token Store - Безопасное хранилище

```csharp
public interface ITokenStore
{
    void SaveToken(string token);
    string? GetToken();
    void RemoveToken();
}

public class SecureTokenStore : ITokenStore
{
    // Безопасное сохранение в DPAPI
}
```

## 🔐 Безопасность

### JWT Токены

1. **Получение**: После входа токен получается от API
2. **Сохранение**: Защищённое хранилище (DPAPI)
3. **Использование**: Автоматически добавляется в заголовок `Authorization`
4. **Удаление**: При выходе токен удаляется

### Обработка ошибок

```csharp
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public NetworkError Error { get; }
    public string? ErrorMessage { get; }
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

## 🧩 Расширяемость

### Добавление нового сервиса

1. Создайте интерфейс в `Services/`
2. Реализуйте интерфейс
3. Зарегистрируйте в DI контейнер в `App.xaml.cs`
4. Используйте в ViewModels через конструктор

### Добавление новой страницы

1. Создайте XAML файл в `Ui/Pages/Main/`
2. Создайте ViewModel в `ViewModels/Main/`
3. Связните View и ViewModel через DataContext
4. Добавьте навигацию в `NavigationService`

## 📊 Диаграмма классов (упрощённо)

```
IAppConfig (Interface)
    └─→ DefaultAppConfig (Implementation)

INavigationService (Interface)
    └─→ NavigationService (Implementation)

IDialogService (Interface)
    └─→ DialogService (Implementation)

ITokenStore (Interface)
    └─→ SecureTokenStore (Implementation)

AuthService
    └─→ Uses: BaseApiService, ITokenStore

BaseApiService
    └─→ Uses: HttpClient, JwtHandler

JwtHandler
    └─→ Uses: ITokenStore

NavigationService
    └─→ Uses: App.Services (DI Container)
```

## 🚀 Performance Considerations

1. **Async/Await**: Все сетевые операции асинхронные
2. **Caching**: Кэширование изображений и данных
3. **Lazy Loading**: Ленивая загрузка страниц
4. **Resource Management**: Правильное управление ресурсами

## 📝 Соглашения кода

- **Naming**: PascalCase для классов, camelCase для переменных
- **Async Methods**: Суффикс `Async` для асинхронных методов
- **Interfaces**: Префикс `I` для интерфейсов
- **Private fields**: Префикс `_` для приватных полей
- **Constants**: UPPER_SNAKE_CASE

## 🔗 Связанные документы

- [Система аутентификации](./AUTHENTICATION.md)
- [Навигация](./NAVIGATION.md)
- [Сетевое взаимодействие](./NETWORK.md)
- [Сервисы и инфраструктура](./SERVICES.md)

---

**Следующий шаг**: Изучите [Систему аутентификации](./AUTHENTICATION.md) для понимания процесса регистрации и входа.
