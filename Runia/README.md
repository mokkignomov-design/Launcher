# Runia Launcher - Полная Документация

## Оглавление

1. [Обзор Проекта](#обзор-проекта)
2. [Архитектура](#архитектура)
3. [Установка и Запуск](#установка-и-запуск)
4. [Основные Компоненты](#основные-компоненты)
5. [Аутентификация и Авторизация](#аутентификация-и-авторизация)
6. [Управление Серверами](#управление-серверами)
7. [Управление Новостями](#управление-новостями)
8. [Запуск Игры](#запуск-игры)
9. [Управление Модами и Установщиками](#управление-модами-и-установщиками)
10. [Настройки и Конфигурация](#настройки-и-конфигурация)
11. [Интерфейс Пользователя](#интерфейс-пользователя)
12. [API Интеграция](#api-интеграция)
13. [Управление Данными](#управление-данными)
14. [Расширение и Развитие](#расширение-и-развитие)

---

## Обзор Проекта

**Runia Launcher** - это современный WPF-приложение для управления и запуска модифицированных серверов Minecraft с поддержкой аутентификации, управления модами, новостями и системой профилей.

### Ключевые особенности:

- 🔐 **Безопасная аутентификация** с JWT токенами
- 🎮 **Управление серверами** - добавляйте и администрируйте серверы
- 📰 **Система новостей** - отображение актуальной информации о серверах
- 🔧 **Управление модами** - автоматическое скачивание и установка модов
- ⚙️ **Гибкие настройки** - кастомизация RAM, темы и многое другое
- 📱 **Современный интерфейс** - интуитивный WPF UI с боковой панелью
- 🔄 **Интеграция API** - полная поддержка REST API

### Технологический стек:

- **.NET 9.0** - целевая платформа
- **WPF** - UI фреймворк
- **CmlLib.Core** - библиотека для запуска Minecraft
- **Forge Modloader** - поддержка модов
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **DPAPI** - шифрование чувствительных данных

---

## Архитектура

### Слоистая Архитектура

Проект организован в следующие основные слои:

```
Runia/
├── Ui/                          # Слой представления (UI)
│   ├── Windows/                 # Основные окна приложения
│   ├── Pages/                   # Страницы (Auth, Main)
│   └── Controls/                # Переиспользуемые контролы
├── ViewModels/                  # Слой представления (Business Logic)
├── Services/                    # Слой сервисов (Application Logic)
│   ├── Core/                    # Ядро приложения
│   ├── Auth/                    # Аутентификация
│   ├── Common/                  # Общие сервисы
│   └── Installers/              # Установщики (Java, Forge, ModPacks)
└── Domain/                      # Слой данных (Models)
```

### Принципы Архитектуры:

- **MVVM Pattern** - разделение UI и логики
- **Dependency Injection** - IoC контейнер для управления зависимостями
- **Repository Pattern** - абстракция доступа к данным
- **Service Locator** - централизованный доступ к сервисам через `App.Services`

---

## Установка и Запуск

### Предварительные требования:

- **.NET 9.0 SDK** - для разработки
- **Visual Studio 2026** (или более новая версия) - для работы с проектом
- **Java Runtime Environment** - для запуска Minecraft (подгружается лаунчером)
- **Windows 7 или выше** - ОС, на которой работает приложение

### Этапы установки:

#### 1. Клонирование репозитория

```powershell
git clone https://github.com/yuranaumov2007-boop/GoGoGaGA.git
cd GoGoGaGA
```

#### 2. Восстановление зависимостей

```powershell
dotnet restore
```

#### 3. Сборка проекта

```powershell
dotnet build
```

#### 4. Запуск приложения

```powershell
dotnet run --project Runia/Runia.csproj
```

### Структура файлов после установки:

```
C:\Users\[Username]\AppData\Local\Runia\
├── versions/                    # Установленные версии серверов
├── data/                        # Локальные данные приложения
├── settings.json                # Конфигурация приложения
└── tokens.bin                   # Защищенное хранилище токенов (DPAPI)
```

---

## Основные Компоненты

### 1. Главное Окно (`MainWindow`)

**Файл**: `Ui/Windows/MainWindow.xaml.cs`

Главное окно приложения, которое служит контейнером для всех страниц.

**Ответственность:**
- Управление фреймом для навигации между страницами
- Обработка перемещения окна
- Управление видимостью боковой панели

**Код инициализации:**
```csharp
public MainWindow()
{
    InitializeComponent();
    Loaded += OnWindowLoaded;
    Activated += OnWindowActivated;
}
```

### 2. Окно Входа (`LoginWindow`)

**Файл**: `Ui/Windows/LoginWindow.xaml.cs`

Отдельное окно для процесса аутентификации пользователя.

**Особенности:**
- Скрытое главное окно до успешной аутентификации
- Переключение на главное окно после входа

### 3. Боковая Панель (`SideBar`)

**Файл**: `Ui/Pages/Controls/SideBar.xaml.cs`

Навигационная боковая панель с основными разделами приложения.

**Разделы:**
- Home - Главная страница
- Server - Управление серверами
- News - Просмотр новостей
- Settings - Настройки приложения

---

## Аутентификация и Авторизация

### Процесс Аутентификации

#### 1. Регистрация

**Файл**: `ViewModels/Auth/RegisterViewModel.cs`

Процесс создания нового аккаунта:

```csharp
public async Task RegisterAsync(string username, string email, string password)
{
    // Валидация входных данных
    // Отправка запроса на API
    // Сохранение пользовательских данных
}
```

**Параметры:**
- `username` - уникальное имя пользователя (3-20 символов)
- `email` - адрес электронной почты для восстановления
- `password` - безопасный пароль (минимум 8 символов, буквы + цифры)

#### 2. Вход (Login)

**Файл**: `ViewModels/Auth/LoginViewModel.cs`

Основной процесс входа в приложение:

```csharp
public async Task LoginAsync(string username, string password)
{
    var response = await _authService.LoginAsync(username, password);
    if (response.IsSuccessful)
    {
        // Токены сохраняются в защищенном хранилище
        // Пользователь редиректится на главное окно
    }
}
```

#### 3. Подтверждение Email

**Файл**: `ViewModels/Auth/ConfirmEmailViewModel.cs`

После регистрации требуется подтверждение email:

```csharp
public async Task ConfirmEmailAsync(string email, string code)
{
    // Отправка кода подтверждения
    // Активация аккаунта
}
```

#### 4. Восстановление Пароля

**Файл**: `ViewModels/Auth/ForgotPasswordViewModel.cs` и `ResetPasswordViewModel.cs`

Двухэтапное восстановление пароля:

```csharp
// Этап 1: Запрос кода восстановления
public async Task RequestResetAsync(string email)

// Этап 2: Установка нового пароля
public async Task ResetPasswordAsync(string email, string code, string newPassword)
```

### Сервис Аутентификации

**Файл**: `Services/Auth/AuthService.cs`

Центральный сервис управления аутентификацией:

```csharp
public class AuthService
{
    public UserModel? CurrentUser { get; private set; }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    public async Task<AuthResponse> RegisterAsync(string username, string email, string password)
    public async Task LogoutAsync()
    public void UpdateCurrentUser(UserModel user)
}
```

### Управление Токенами

**Файлы**:
- `Services/Core/Infrastructure/Network/ITokenStore.cs` - интерфейс
- `Services/Core/Infrastructure/Network/SecureTokenStore.cs` - защищенное хранилище

**Особенности:**
- Использует **DPAPI** для шифрования токенов на уровне ОС
- Автоматическое сохранение при входе
- Автоматическое удаление при выходе
- Защита от несанкционированного доступа

```csharp
public interface ITokenStore
{
    void SaveTokens(string accessToken, string refreshToken);
    bool TryGetAccessToken(out string token);
    bool TryGetRefreshToken(out string token);
    void ClearTokens();
}
```

### JWT Обработчик

**Файл**: `Services/Core/Infrastructure/Network/JwtHandler.cs`

Автоматическое добавление JWT токена ко всем HTTP запросам:

```csharp
public class JwtHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Добавление Authorization заголовка
        // Обновление токена если истек
    }
}
```

---

## Управление Серверами

### Модель Сервера

**Файл**: `Domain/Models/ServerModel.cs`

```csharp
public class ServerModel
{
    public int Id { get; set; }
    public string Title { get; set; }              // Название сервера
    public string Description { get; set; }        // Описание
    public string? ImageUrl { get; set; }          // URL аватара
    public string? ZipUrl { get; set; }            // URL для скачивания модпака
    public string? ForgeVersion { get; set; }      // Версия Forge
    public string? MinecraftVersion { get; set; }  // Версия Minecraft
    public int? MaxPlayers { get; set; }           // Максимум игроков
    public int? CurrentPlayers { get; set; }       // Текущее количество
    public int? Port { get; set; }                 // Порт сервера
    public string? ServerAddress { get; set; }     // IP адрес
    public DateTime CreatedAt { get; set; }        // Дата создания
}
```

### ViewModel Сервера

**Файл**: `ViewModels/Main/ServerViewModel.cs`

Управление списком серверов:

```csharp
public class ServerViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ServerModel> Servers { get; }

    public async Task LoadServersAsync()
    public async Task RefreshServersAsync()
    public ServerModel? SelectedServer { get; set; }
}
```

### Детали Сервера

**Файл**: `ViewModels/Main/ServerDetailsViewModel.cs`

Детальная информация о конкретном сервере:

```csharp
public class ServerDetailsViewModel
{
    public ServerModel CurrentServer { get; set; }

    public async Task LaunchGameAsync()
    // Инициирует запуск игры на этом сервере
}
```

### Использование в UI

```csharp
// Получение списка серверов
var serverViewModel = App.Services.GetRequiredService<ServerViewModel>();
await serverViewModel.LoadServersAsync();

// Запуск на конкретном сервере
await serverViewModel.SelectedServer.LaunchAsync();
```

---

## Управление Новостями

### Модель Новости

**Файл**: `Domain/Models/NewsModel.cs`

```csharp
public class NewsModel
{
    public int Id { get; set; }
    public string Title { get; set; }              // Заголовок
    public string Content { get; set; }            // Содержимое
    public string? ImageUrl { get; set; }          // URL изображения
    public DateTime PublishedAt { get; set; }      // Дата публикации
    public string AuthorName { get; set; }         // Автор
    public int ServerId { get; set; }              // Связанный сервер
}
```

### Сервис Новостей

**Файл**: `Services/Common/NewsService.cs`

```csharp
public class NewsService
{
    public async Task<List<NewsModel>> GetNewsAsync()
    public async Task<NewsModel> GetNewsByIdAsync(int id)
    public async Task<List<NewsModel>> GetServerNewsAsync(int serverId)
    public async Task PublishNewsAsync(NewsModel news)
}
```

### ViewModel Новостей

**Файлы**:
- `ViewModels/Main/NewsDetailsViewModel.cs` - детали одной новости
- `HomeViewModel.cs` - отображение на главной странице

```csharp
public class NewsDetailsViewModel
{
    public NewsModel CurrentNews { get; set; }

    public async Task LoadNewsAsync(int newsId)
}
```

### Отображение Новостей

Новости отображаются на:
- **Главной странице** - последние новости всех серверов
- **Странице сервера** - новости конкретного сервера
- **Отдельная страница** - полный просмотр новости

---

## Запуск Игры

### Сервис Лаунчера

**Файл**: `Services/Core/Launcher/LauncherService.cs`

Основной сервис для запуска игры с полной защитой безопасности:

```csharp
public class LauncherService
{
    public event Action? OnGameStarted;    // Событие: игра запущена
    public event Action? OnGameExited;     // Событие: игра завершена

    public async Task LaunchPackAsync(
        string packName, 
        IProgress<string>? logProgress, 
        CancellationToken ct)
    {
        // 1. Проверка сервера
        // 2. Подготовка директорий
        // 3. Установка Java
        // 4. Установка Forge
        // 5. Установка модпака
        // 6. Запуск процесса игры
    }
}
```

### Процесс Запуска

#### Этап 1: Проверка Сервера

```csharp
var servers = await _config.GetServersAsync();
var currentServer = servers.FirstOrDefault(s => s.Title == packName);
if (currentServer == null)
    throw new Exception($"Сервер {packName} не найден");
```

#### Этап 2: Получение Параметров

```csharp
int ram = _config.RamAmount;
string instancePath = VersionsManager.GetVersionPath(packName);
string username = _authService.CurrentUser?.Username ?? "Player";
```

#### Этап 3: Подготовка Директорий

```csharp
VersionsManager.EnsureDirectoriesExist(packName);
logProgress?.Report("Preparing directories...");
```

#### Этап 4: Установка Java

```csharp
logProgress?.Report("Installing Java...");
var javaPath = await _javaManager.EnsureJavaAsync();
```

#### Этап 5: Установка Forge

```csharp
if (!string.IsNullOrWhiteSpace(currentServer.ForgeVersion))
{
    logProgress?.Report($"Installing Forge {currentServer.ForgeVersion}...");
    await _forgeManager.InstallForgeAsync(
        currentServer.ForgeVersion, 
        instancePath);
}
```

#### Этап 6: Установка Модпака

```csharp
if (!string.IsNullOrWhiteSpace(currentServer.ZipUrl))
{
    logProgress?.Report("Downloading and extracting modpack...");
    await _modPackManager.DownloadAndExtractAsync(
        currentServer.ZipUrl, 
        instancePath);
}
```

#### Этап 7: Запуск Процесса

```csharp
var launcher = new CmlLib.Launcher(instancePath);
var process = launcher.CreateGameProcess(username, ram);
process.Start();

OnGameStarted?.Invoke();

// Ожидание завершения
process.WaitForExit();
OnGameExited?.Invoke();
```

### Безопасность Запуска

⚠️ **Важно**: Username (`username`) получается **ТОЛЬКО** из `AuthService.CurrentUser`, что гарантирует:

1. ✅ Пользователь может запуститься **только** под своим подтвержденным аккаунтом
2. ✅ Невозможно подделать никнейм через UI или конфиг
3. ✅ Все критические данные защищены DPAPI
4. ✅ Fallback значение "Player" используется только при недоступности сервера

### Использование в UI

```csharp
var launcherService = App.Services.GetRequiredService<LauncherService>();
var progress = new Progress<string>(log => Console.WriteLine(log));

try
{
    await launcherService.LaunchPackAsync(
        "ServerName", 
        progress, 
        CancellationToken.None);
}
catch (Exception ex)
{
    // Обработка ошибок
}

// Подписка на события
launcherService.OnGameStarted += () => Debug.WriteLine("Game started!");
launcherService.OnGameExited += () => Debug.WriteLine("Game exited!");
```

---

## Управление Модами и Установщиками

### Менеджер Java

**Файл**: `Services/Installers/JavaManager.cs`

Автоматическое управление Java Runtime Environment:

```csharp
public class JavaManager
{
    public async Task<string> EnsureJavaAsync()
    // Проверяет установку Java, скачивает если нужно

    public bool IsJavaInstalled()
    // Проверка наличия Java

    public async Task<string> GetJavaPathAsync()
    // Получить полный путь к java.exe
}
```

**Функции:**
- Автоматическое обнаружение Java в системе
- Скачивание требуемой версии Java
- Кэширование установленных версий
- Проверка совместимости

### Менеджер Forge

**Файл**: `Services/Installers/ForgeManager.cs`

Управление установкой Forge Modloader:

```csharp
public class ForgeManager
{
    public async Task InstallForgeAsync(string version, string gamePath)
    // Установка конкретной версии Forge

    public async Task<List<string>> GetAvailableVersionsAsync()
    // Получить доступные версии Forge

    public bool IsForgeInstalled(string version, string gamePath)
    // Проверка установки версии
}
```

**Процесс установки:**
1. Скачивание инсталлятора Forge
2. Запуск инсталлятора
3. Копирование файлов в игровую директорию
4. Проверка целостности файлов

### Менеджер Модпаков

**Файл**: `Services/Installers/ModPackManager.cs`

Управление скачиванием и установкой модпаков:

```csharp
public class ModPackManager
{
    public async Task DownloadAndExtractAsync(string zipUrl, string gamePath)
    // Скачать архив и распаковать в игровую директорию

    public async Task<bool> VerifyModPackAsync(string gamePath)
    // Проверка целостности установленного модпака

    public async Task CleanupModsAsync(string gamePath)
    // Удаление временных файлов
}
```

**Процесс установки модпака:**
1. Скачивание ZIP архива с модами
2. Распаковка в директорию mods
3. Проверка целостности файлов
4. Удаление временных файлов

### Менеджер Версий

**Файл**: `Services/Core/Launcher/VersionsManager.cs`

Управление версиями серверов и игры:

```csharp
public class VersionsManager
{
    public static string GetVersionPath(string packName)
    // Получить путь к версии

    public static void EnsureDirectoriesExist(string packName)
    // Создать необходимые директории

    public static void ClearVersion(string packName)
    // Удалить версию и освободить место
}
```

---

## Настройки и Конфигурация

### Интерфейс Конфигурации

**Файл**: `Services/Core/Infrastructure/Configuration/IAppConfig.cs`

```csharp
public interface IAppConfig
{
    string ApiBaseUrl { get; }
    int RamAmount { get; set; }
    string GamePath { get; set; }
    string Theme { get; set; }

    Task<List<ServerModel>> GetServersAsync();
    Task SaveSettingsAsync();
}
```

### Конфигурация По Умолчанию

**Файл**: `Services/Core/Infrastructure/Configuration/DefaultAppConfig.cs`

```csharp
public class DefaultAppConfig : IAppConfig
{
    public string ApiBaseUrl => "https://api.runia-launcher.com";
    public int RamAmount { get; set; } = 4096;  // 4 GB по умолчанию
    public string GamePath { get; set; } = @"%APPDATA%\Runia\versions";
    public string Theme { get; set; } = "Dark";
}
```

### ViewModel Настроек

**Файл**: `ViewModels/Main/SettingsViewModel.cs`

```csharp
public class SettingsViewModel : INotifyPropertyChanged
{
    public int RamAmount { get; set; }      // Объем RAM для игры
    public string GamePath { get; set; }    // Путь к папке с игрой
    public string Theme { get; set; }       // Тема оформления

    public async Task SaveSettingsAsync()
    public async Task ResetSettingsAsync()
    public async Task ClearCacheAsync()
}
```

### Использование Конфигурации

```csharp
var config = App.Services.GetRequiredService<IAppConfig>();

// Чтение настроек
int ram = config.RamAmount;
string gamePath = config.GamePath;

// Изменение настроек
config.RamAmount = 8192;  // 8 GB
await config.SaveSettingsAsync();
```

### Статические Константы

**Файл**: `Services/Core/Infrastructure/Constants/AppStatic.cs`

```csharp
public static class AppStatic
{
    public const string ApiBaseUrl = "https://api.runia-launcher.com";
    public const int DefaultRam = 4096;
    public const string GameDirectory = "Runia\\versions";
    // Другие константы приложения
}
```

---

## Интерфейс Пользователя

### Архитектура UI

Интерфейс построен по MVVM паттерну:

```
UI (XAML)
    ↓
ViewModel (Business Logic)
    ↓
Services (Application Logic)
    ↓
Models (Data)
```

### Главные Страницы

#### 1. Главная Страница (Home)

**Файлы**:
- `Ui/Pages/Main/HomePage.xaml.cs`
- `ViewModels/Main/HomeViewModel.cs`

**Содержимое:**
- Последние новости всех серверов
- Быстрые ссылки на популярные серверы
- Статистика активности
- Объявления и обновления

**Использование:**
```csharp
var homeViewModel = App.Services.GetRequiredService<HomeViewModel>();
await homeViewModel.LoadHomeDataAsync();
```

#### 2. Страница Серверов

**Файлы**:
- `Ui/Pages/Main/ServerPage.xaml.cs`
- `ViewModels/Main/ServerViewModel.cs`

**Функции:**
- Просмотр всех доступных серверов
- Фильтрация по категориям
- Поиск серверов
- Запуск игры на сервере

**Пример использования:**
```csharp
var serverVM = App.Services.GetRequiredService<ServerViewModel>();
await serverVM.LoadServersAsync();

// Запуск на сервере
var launcherService = App.Services.GetRequiredService<LauncherService>();
await launcherService.LaunchPackAsync(serverVM.SelectedServer.Title, null, CancellationToken.None);
```

#### 3. Детали Сервера

**Файлы**:
- `Ui/Pages/Main/TemplatePages/TemplateServerDetailsPage.xaml.cs`
- `ViewModels/Main/ServerDetailsViewModel.cs`

**Информация:**
- Полное описание сервера
- Новости сервера
- Статус игроков онлайн
- Кнопка запуска игры

#### 4. Страница Новостей

**Файлы**:
- Главная страница показывает все новости
- `Ui/Pages/Main/TemplatePages/TemplateNewsDetailsPage.xaml.cs` - детали
- `ViewModels/Main/NewsDetailsViewModel.cs`

#### 5. Страница Настроек

**Файлы**:
- `Ui/Pages/Main/SettingsPage.xaml.cs`
- `ViewModels/Main/SettingsViewModel.cs`

**Опции:**
- Объем RAM для игры (1-32 GB)
- Путь к папке с игрой
- Выбор темы (Light/Dark)
- Кэширование и чистка данных
- Выход из аккаунта

**Пример:**
```csharp
var settingsVM = App.Services.GetRequiredService<SettingsViewModel>();
settingsVM.RamAmount = 8192;
await settingsVM.SaveSettingsAsync();
```

### Страницы Аутентификации

#### 1. Страница Входа (Login)

**Файлы**:
- `Ui/Pages/Auth/LoginPage.xaml.cs`
- `ViewModels/Auth/LoginViewModel.cs`

**Поля:**
- Username (электронная почта или имя)
- Password (защищенное поле ввода)

**События:**
- Успешный вход → переход на MainWindow
- Ошибка входа → сообщение об ошибке
- Забыли пароль? → переход на страницу восстановления

#### 2. Страница Регистрации

**Файлы**:
- `Ui/Pages/Auth/RegisterPage.xaml.cs`
- `ViewModels/Auth/RegisterViewModel.cs`

**Поля:**
- Username (уникальное имя)
- Email (адрес электронной почты)
- Password (безопасный пароль)
- Confirm Password (подтверждение)

**Валидация:**
- Email формат корректный
- Пароль содержит буквы и цифры
- Пароли совпадают

#### 3. Подтверждение Email

**Файлы**:
- `Ui/Pages/Auth/ConfirmEmailPage.xaml.cs`
- `ViewModels/Auth/ConfirmEmailViewModel.cs`

**Процесс:**
- Отправка кода на email
- Ввод кода подтверждения
- Активация аккаунта

#### 4. Восстановление Пароля

**Страница 1 - Запрос кода:**
- `Ui/Pages/Auth/ForgotPasswordPage.xaml.cs`
- `ViewModels/Auth/ForgotPasswordViewModel.cs`

**Страница 2 - Установка пароля:**
- `Ui/Pages/Auth/ResetPasswordPage.xaml.cs`
- `ViewModels/Auth/ResetPasswordViewModel.cs`

### Боковая Панель (SideBar)

**Файлы**:
- `Ui/Pages/Controls/SideBar.xaml.cs`
- `ViewModels/Main/SidebarViewModel.cs`

**Навигация:**
```
┌─ Home
├─ Servers
├─ News
├─ Settings
└─ Logout (в футере)
```

**Использование:**
```csharp
var sidebarVM = App.Services.GetRequiredService<SidebarViewModel>();
sidebarVM.NavigateTo("Home");
```

### Поведение Вложений (Behaviors)

**Файл**: `Services/Core/Infrastructure/WPF/Behaviors/PasswordBoxAttachmentBehavior.cs`

Привязка защищенного пароля в XAML:

```csharp
public static class PasswordBoxAttachmentBehavior
{
    public static string GetPassword(PasswordBox passwordBox)
    public static void SetPassword(PasswordBox passwordBox, string value)
}
```

**Использование в XAML:**
```xml
<PasswordBox 
    local:PasswordBoxAttachmentBehavior.Password="{Binding Password}" />
```

### Сервис Диалогов

**Файлы**:
- `Services/Common/IDialogService.cs` - интерфейс
- `Services/Common/DialogService.cs` - реализация

```csharp
public interface IDialogService
{
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string message);
}
```

**Пример использования:**
```csharp
var dialogService = App.Services.GetRequiredService<IDialogService>();
await dialogService.ShowInfoAsync("Success", "Operation completed!");
```

### Менеджер Тем

**Файл**: `Services/Common/ThemeManager.cs`

Управление темой приложения:

```csharp
public class ThemeManager
{
    public void SetTheme(string themeName)
    // Установить тему (Light/Dark/Custom)

    public string GetCurrentTheme()
    // Получить текущую тему

    public List<string> GetAvailableThemes()
    // Получить доступные темы
}
```

---

## API Интеграция

### Базовый API Сервис

**Файл**: `Services/Core/Infrastructure/Network/BaseApiService.cs`

Основной сервис для всех REST API запросов:

```csharp
public class BaseApiService
{
    protected readonly HttpClient _httpClient;

    protected async Task<T> GetAsync<T>(string endpoint)
    protected async Task<T> PostAsync<T>(string endpoint, object data)
    protected async Task<T> PutAsync<T>(string endpoint, object data)
    protected async Task<bool> DeleteAsync(string endpoint)
}
```

### API Эндпоинты

#### Аутентификация

```
POST /api/auth/register
    Body: { username, email, password }
    Response: { success, message }

POST /api/auth/login
    Body: { username, password }
    Response: { accessToken, refreshToken, user }

POST /api/auth/refresh
    Body: { refreshToken }
    Response: { accessToken, refreshToken }

POST /api/auth/logout
    Response: { success }

POST /api/auth/confirm-email
    Body: { email, code }
    Response: { success }

POST /api/auth/forgot-password
    Body: { email }
    Response: { success }

POST /api/auth/reset-password
    Body: { email, code, newPassword }
    Response: { success }
```

#### Серверы

```
GET /api/servers
    Response: [{ id, title, description, imageUrl, ... }]

GET /api/servers/{id}
    Response: { id, title, description, ... }

POST /api/servers
    Body: { title, description, ... }
    Response: { id, ... }

PUT /api/servers/{id}
    Body: { title, description, ... }
    Response: { id, ... }

DELETE /api/servers/{id}
    Response: { success }
```

#### Новости

```
GET /api/news
    Response: [{ id, title, content, publishedAt, ... }]

GET /api/news/{id}
    Response: { id, title, content, ... }

GET /api/servers/{serverId}/news
    Response: [{ id, title, ... }]

POST /api/news
    Body: { title, content, serverId, ... }
    Response: { id, ... }

PUT /api/news/{id}
    Body: { title, content, ... }
    Response: { id, ... }

DELETE /api/news/{id}
    Response: { success }
```

### Скачивание Изображений

**Файл**: `Services/Core/Infrastructure/Network/ImageDownloader.cs`

```csharp
public class ImageDownloader
{
    public async Task<Stream> DownloadImageAsync(string imageUrl)
    // Скачать изображение по URL

    public async Task<byte[]> GetImageBytesAsync(string imageUrl)
    // Получить изображение в виде массива байт
}
```

### Менеджер Изображений

**Файл**: `Services/Common/ImageManager.cs`

Кэширование и управление изображениями:

```csharp
public class ImageManager
{
    public async Task<ImageSource> GetImageAsync(string url)
    // Получить изображение (с кэшированием)

    public void ClearCache()
    // Очистить кэш изображений

    public async Task<bool> PreloadImageAsync(string url)
    // Предварительно загрузить изображение
}
```

### Поставщик Изображений

**Файл**: `Services/Common/ImageProvider.cs`

```csharp
public class ImageProvider
{
    public static ImageSource? GetImageSource(string? url)
    // Получить ImageSource из URL или пути

    public static void SetDefaultImage(ImageSource? defaultImage)
    // Установить изображение по умолчанию
}
```

### Обработка HTTP Ошибок

**Файл**: `Services/Core/Infrastructure/Network/JwtHandler.cs`

Обработчик для добавления JWT токена и обновления:

```csharp
public class JwtHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...)
    {
        // 1. Добавить токен в заголовок Authorization
        // 2. Отправить запрос
        // 3. Если 401 → обновить токен и повторить
        // 4. Если ошибка → выбросить исключение
    }
}
```

---

## Управление Данными

### Модель Пользователя

**Файл**: `Domain/Models/UserModel.cs`

```csharp
public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsEmailConfirmed { get; set; }
}
```

### Модель Аутентификации

**Файл**: `Domain/Models/AuthModel.cs`

```csharp
public class AuthResponse
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserModel? User { get; set; }
}
```

### Локальный Сервис Данных

**Файл**: `Services/Core/Infrastructure/LocalDataService.cs`

Работа с локальным хранилищем данных:

```csharp
public class LocalDataService
{
    public async Task<T?> LoadAsync<T>(string key)
    // Загрузить данные из локального хранилища

    public async Task SaveAsync<T>(string key, T data)
    // Сохранить данные в локальное хранилище

    public async Task DeleteAsync(string key)
    // Удалить данные

    public async Task ClearAllAsync()
    // Очистить все данные
}
```

### Менеджер Данных

**Файл**: `Services/Core/Infrastructure/Persistence/DataManager.cs`

Управление персистентностью данных:

```csharp
public class DataManager
{
    public async Task SaveApplicationStateAsync()
    // Сохранить состояние приложения

    public async Task LoadApplicationStateAsync()
    // Загрузить состояние приложения

    public async Task BackupDataAsync()
    // Создать резервную копию данных

    public async Task RestoreDataAsync()
    // Восстановить из резервной копии
}
```

### JSON Сериализация

Приложение использует стандартную JSON сериализацию:

```csharp
var json = JsonSerializer.Serialize(data);
var data = JsonSerializer.Deserialize<T>(json);
```

---

## Расширение и Развитие

### Добавление Новой Страницы

#### Шаг 1: Создать ViewModel

```csharp
// ViewModels/Main/MyFeatureViewModel.cs
public class MyFeatureViewModel : INotifyPropertyChanged
{
    private string _title = "My Feature";

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### Шаг 2: Зарегистрировать в DI

```csharp
// App.xaml.cs - в ConfigureServices
services.AddSingleton<MyFeatureViewModel>();
```

#### Шаг 3: Создать XAML Страницу

```xml
<!-- Ui/Pages/Main/MyFeaturePage.xaml -->
<Page x:Class="Runia.Ui.Pages.MyFeaturePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <TextBlock Text="{Binding Title}" FontSize="24"/>
    </Grid>
</Page>
```

#### Шаг 4: Создать Code-Behind

```csharp
// Ui/Pages/Main/MyFeaturePage.xaml.cs
public partial class MyFeaturePage : Page
{
    public MyFeaturePage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MyFeatureViewModel>();
    }
}
```

#### Шаг 5: Добавить в Навигацию

```csharp
// ViewModels/Main/SidebarViewModel.cs
private void NavigateToMyFeature()
{
    var navService = App.Services.GetRequiredService<INavigationService>();
    navService.Navigate(typeof(MyFeaturePage));
}
```

### Добавление Нового Сервиса

#### Пример: Сервис Уведомлений

```csharp
// Services/Common/INotificationService.cs
public interface INotificationService
{
    void ShowNotification(string title, string message, NotificationType type);
    event Action<NotificationModel>? OnNotificationReceived;
}

// Services/Common/NotificationService.cs
public class NotificationService : INotificationService
{
    public void ShowNotification(string title, string message, NotificationType type)
    {
        var notification = new NotificationModel
        {
            Title = title,
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
        };

        OnNotificationReceived?.Invoke(notification);
    }

    public event Action<NotificationModel>? OnNotificationReceived;
}

// Регистрация в App.xaml.cs
services.AddSingleton<INotificationService, NotificationService>();
```

### Добавление API Эндпоинта

```csharp
// Services/BaseApiService.cs - добавить метод
public async Task<List<MyModel>> GetMyDataAsync()
{
    return await GetAsync<List<MyModel>>("/api/mydata");
}
```

### Обработка Ошибок

```csharp
try
{
    await myService.DoSomethingAsync();
}
catch (HttpRequestException ex)
{
    // Сетевая ошибка
    await dialogService.ShowErrorAsync("Network Error", ex.Message);
}
catch (TaskCanceledException)
{
    // Таймаут
    await dialogService.ShowErrorAsync("Timeout", "Request took too long");
}
catch (Exception ex)
{
    // Неожиданная ошибка
    await dialogService.ShowErrorAsync("Error", ex.Message);
}
```

### Логирование

Приложение использует встроенное логирование:

```csharp
using Microsoft.Extensions.Logging;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("Doing something");
        _logger.LogWarning("Warning occurred");
        _logger.LogError("Error occurred", new Exception());
    }
}
```

### Тестирование

Пример Unit теста:

```csharp
[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var authService = new AuthService();

        // Act
        var result = await authService.LoginAsync("user", "password");

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsNotNull(result.AccessToken);
    }
}
```

### Code Style и Соглашения

Проект использует C# 12 и следует этим соглашениям:

- **Collection Expressions**: `var items = new[] { item1, item2 };` → `var items = [item1, item2];`
- **Null-coalescing**: `x ?? y`
- **Pattern Matching**: `if (obj is MyClass { Property: value })`
- **Records**: для DTO моделей
- **Nullable Reference Types**: `string?`
- **Async/Await**: для всех асинхронных операций
- **Property Initialization**: `public int Id { get; set; }`

### Структура Папок

```
Runia/
├── Domain/                      # Модели данных
│   └── Models/
├── Services/                    # Бизнес-логика
│   ├── Auth/                   # Аутентификация
│   ├── Common/                 # Общие сервисы
│   ├── Core/
│   │   ├── Infrastructure/     # Инфраструктура
│   │   ├── Launcher/           # Запуск игры
│   │   └── Navigation/         # Навигация
│   └── Installers/             # Установщики
├── Ui/                         # Пользовательский интерфейс
│   ├── Pages/
│   │   ├── Auth/               # Страницы аутентификации
│   │   ├── Main/               # Основные страницы
│   │   └── Controls/           # Контролы
│   └── Windows/                # Окна приложения
├── ViewModels/                 # ViewModel слой
├── App.xaml                    # Конфигурация приложения
└── Resources/                  # Ресурсы приложения
```

---

## Профили (Упоминание в разработке)

⚠️ **Статус**: Функциональность профилей еще находится в разработке.

**Планируется:**
- Система создания и управления профилями игрока
- Сохранение и восстановление конфигурации
- Быстрое переключение между профилями
- Синхронизация профилей между устройствами

**Документация будет добавлена** после завершения разработки этой функции.

---

## Часто Задаваемые Вопросы (FAQ)

### Q: Как изменить объем RAM для игры?
**A**: Перейдите в Settings → выберите нужный объем (1-32 GB) → нажмите Save.

### Q: Где хранятся мои токены?
**A**: Токены хранятся в защищенном хранилище, зашифрованном DPAPI на уровне ОС. Расположение: `%APPDATA%\Runia\tokens.bin`

### Q: Как добавить новый сервер?
**A**: Администраторы могут добавлять серверы через API. Обычные пользователи видят доступные серверы на странице "Servers".

### Q: Что делать если игра не запускается?
**A**: 
1. Проверьте установку Java (в Лаунчере должно быть предложено установить)
2. Убедитесь, что у вас достаточно места на диске
3. Проверьте наличие Forge версии для вашей версии игры
4. Посмотрите логи ошибок

### Q: Как восстановить пароль?
**A**: На странице входа нажмите "Forgot Password?" → введите email → следуйте инструкциям в письме.

### Q: Безопасно ли хранить пароль в приложении?
**A**: Пароли **никогда не хранятся** локально. Вместо этого используются JWT токены, хранящиеся в защищенном DPAPI хранилище.

### Q: Можно ли использовать несколько аккаунтов?
**A**: Да, просто выйдите из приложения и войдите под другим аккаунтом.

### Q: Как обновить кэш новостей?
**A**: Новости обновляются автоматически при заходе на соответствующую страницу, или используйте кнопку "Refresh".

---

## Поддержка и Разработка

### Получение Помощи

- 📧 **Email**: support@runia-launcher.com
- 🐛 **Баги**: https://github.com/yuranaumov2007-boop/GoGoGaGA/issues
- 💬 **Дискуссии**: https://github.com/yuranaumov2007-boop/GoGoGaGA/discussions

### Участие в Разработке

Мы приветствуем контрибьюции! Пожалуйста:

1. Fork репозиторий
2. Создайте feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit изменения (`git commit -m 'Add AmazingFeature'`)
4. Push в branch (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request

### Лицензия

[Укажите лицензию]

### Благодарности

Спасибо за интерес к Runia Launcher! 🚀

---

**Версия документации**: 1.0
**Последнее обновление**: 2024
**Целевая платформа**: .NET 9 / Windows 7+
