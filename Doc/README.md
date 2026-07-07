# Runia Launcher 🎮

**Современный и полнофункциональный Minecraft Launcher** с поддержкой аутентификации, управлением серверами, историей новостей и системой установки модов.

> *Runia* - это кроссплатформенный лаунчер для Minecraft, разработанный на C# с использованием WPF и .NET 9.

---

## 📋 Оглавление

- [Основные возможности](#-основные-возможности)
- [Требования к системе](#-требования-к-системе)
- [Установка и запуск](#-установка-и-запуск)
- [Архитектура проекта](#-архитектура-проекта)
- [Руководство разработчика](#-руководство-разработчика)
- [API и сервисы](#-api-и-сервисы)
- [Конфигурация](#-конфигурация)
- [Лицензия](#-лицензия)

---

## ✨ Основные возможности

### 🔐 Аутентификация
- **Регистрация и вход** - Полная система управления аккаунтами
- **Восстановление пароля** - Email-верификация и сброс пароля
- **JWT токены** - Безопасное хранилище токенов с шифрованием
- **Подтверждение email** - Обязательная верификация при регистрации
- **Двухфакторная аутентификация (безопасность)** - Управление сеансами и токенами

### 🎮 Запуск Minecraft
- **Установка Java** - Автоматический поиск или установка нужной версии
- **Управление Forge** - Установка и конфигурация Forge модов
- **Модпаки** - Поддержка установки и управления модпаками
- **Версионирование** - Загрузка и управление различными версиями игры
- **Запуск процесса** - Интеграция CmlLib для полного контроля над игровым процессом

### 📰 Новости и информация
- **Лента новостей** - Получение актуальных новостей из сервера
- **Детальные статьи** - Просмотр полной информации о каждной новости
- **Изображения** - Загрузка и кеширование изображений новостей

### 🖥️ Серверы
- **Каталог серверов** - Просмотр доступных серверов
- **Информация о серверах** - Статус, игроки, детали конфигурации
- **История подключений** - Сохранение недавно используемых серверов

### 👤 Профиль пользователя
- **Управление скинами** - Загрузка и применение пользовательских скинов
- **История платежей** - Просмотр покупок и подписок
- **Параметры безопасности** - Управление паролем и данными аккаунта
- **Тема оформления** - Переключение между светлой и тёмной темами

### 🛠️ Администрирование
- **Система логирования** - Файловое логирование для отладки и аналитики
- **Диалоги** - Уведомления и подтверждения действий
- **Сохранение состояния** - Локальное хранилище данных приложения

---

## 🖥️ Требования к системе

### Минимальные требования
- **ОС**: Windows 7 SP1 или выше
- **.NET Framework**: .NET 9.0 (Windows Desktop)
- **Процессор**: Intel Core i3 или эквивалент
- **Оперативная память**: 2 ГБ
- **Интернет**: Подключение для загрузки игры и компонентов
- **Java**: Будет установлена автоматически при необходимости

### Рекомендуемые требования
- **ОС**: Windows 10/11
- **Процессор**: Intel Core i5 или выше
- **Оперативная память**: 4 ГБ+
- **Место на диске**: 50+ ГБ (для игры и модов)
- **Интернет**: Широкополосное соединение

---

## 📦 Установка и запуск

### Из исходного кода

#### Предварительные требования
```bash
# Установить .NET 9.0 SDK
# Скачать с: https://dotnet.microsoft.com/download/dotnet/9.0
```

#### Клонирование репозитория
```bash
git clone https://github.com/mokkignomov-design/Launcher.git
cd Launcher
```

#### Восстановление зависимостей
```bash
dotnet restore
```

#### Компиляция
```bash
dotnet build -c Release
```

#### Запуск
```bash
# Из папки с выходными файлами
cd Runia/bin/Release/net9.0-windows
Runia.exe
```

### Из выпуска (Release)

1. Скачайте последнюю версию с [GitHub Releases](https://github.com/mokkignomov-design/Launcher/releases)
2. Распакуйте архив
3. Запустите `Runia.exe`

---

## 🏗️ Архитектура проекта

```
Runia/
├── Domain/
│   └── Models/              # Данные модели (Auth, User, Server и т.д.)
├── Ui/
│   ├── Pages/               # XAML страницы UI
│   │   ├── Auth/            # Страницы аутентификации
│   │   ├── Main/            # Основные страницы (Home, Server, Settings)
│   │   ├── Controls/        # Переиспользуемые компоненты
│   │   └── Profile/         # Профиль пользователя
│   └── Windows/             # Основные окна приложения
├── ViewModels/              # ViewModel слой (MVVM логика)
│   ├── Auth/                # ViewModels аутентификации
│   └── Main/                # ViewModels основного приложения
├── Services/
│   ├── Auth/                # Логика аутентификации (AuthManager, AuthService)
│   ├── Common/              # Общие сервисы (Theme, Image, Dialog)
│   ├── Core/
│   │   ├── Infrastructure/  # Базовая инфраструктура
│   │   │   ├── Configuration/   # Конфигурация приложения
│   │   │   ├── Constants/       # Константы
│   │   │   ├── Logging/         # Система логирования
│   │   │   ├── Network/         # HTTP клиенты и API
│   │   │   ├── Persistence/     # Сохранение данных
│   │   │   └── WPF/             # WPF утилиты и behaviors
│   │   ├── Launcher/        # Сервис запуска Minecraft
│   │   └── Navigation/      # Система навигации страниц
│   └── Installers/          # Менеджеры установки (Java, Forge, ModPacks)
├── assets/
│   ├── Fonts/               # Пользовательские шрифты
│   ├── Icons/               # Иконки приложения
│   ├── Images/              # Изображения интерфейса
│   ├── Themes/              # XAML темы (Dark/Light)
│   └── Resources/           # XAML ресурсы стилей
└── App.xaml / App.xaml.cs   # Точка входа и конфигурация DI

Updater/
└── Program.cs               # Утилита для обновления лаунчера
```

### Слои архитектуры

```
┌─────────────────────────────────┐
│        UI Layer (XAML)          │
│    Pages + Windows + Controls   │
├─────────────────────────────────┤
│    ViewModel Layer (MVVM)       │
│     Бизнес-логика интерфейса    │
├─────────────────────────────────┤
│       Service Layer             │
│  Auth, Launcher, Navigation     │
├─────────────────────────────────┤
│   Infrastructure Layer          │
│  Network, Logging, Storage      │
├─────────────────────────────────┤
│      Domain Layer               │
│    Models + Data Contracts      │
└─────────────────────────────────┘
```

---

## 👨‍💻 Руководство разработчика

### Структура решения

**Основной проект (Runia)**
- Главное приложение WPF с GUI
- Содержит всю логику лаунчера
- Целевая платформа: .NET 9.0 (Windows Desktop)

**Утилита обновления (Updater)**
- Консольное приложение для обновления основного лаунчера
- Использует семантическое версионирование

### Настройка среды разработки

#### Требуемые инструменты
```bash
# Visual Studio 2022+ или VS Code
# .NET 9.0 SDK
# Git

# Рекомендуемые расширения VS Code
# - C# (powered by OmniSharp)
# - XAML Previewer
```

#### Первый запуск
```bash
# 1. Клонировать репозиторий
git clone https://github.com/mokkignomov-design/Launcher.git
cd Launcher

# 2. Восстановить зависимости
dotnet restore

# 3. Открыть в VS (или запустить)
dotnet run --project Runia/Runia.csproj
```

### Ключевые паттерны кода

#### Dependency Injection (DI)
```csharp
// Регистрация в App.xaml.cs
services.AddSingleton<IAuthService, AuthService>();
services.AddScoped<INavigationService, NavigationService>();

// Внедрение через конструктор
public class MyService(IAuthService authService)
{
	private readonly IAuthService _authService = authService;
}
```

#### MVVM с ViewModel
```csharp
// ViewModel наследует от ObservableObject
public class LoginViewModel : ObservableObject
{
	private string _username = "";
	private bool _isLoading;

	public string Username
	{
		get => _username;
		set => SetProperty(ref _username, value);
	}

	public IRelayCommand LoginCommand { get; }

	public LoginViewModel(IAuthService authService)
	{
		LoginCommand = new AsyncRelayCommand(LoginAsync);
	}

	private async Task LoginAsync()
	{
		IsLoading = true;
		try
		{
			await _authService.LoginAsync(Username);
		}
		finally
		{
			IsLoading = false;
		}
	}
}
```

#### Работа с API
```csharp
// Клиент наследует от BaseApiService
public class UserClient(HttpClient httpClient) : BaseApiService(httpClient)
{
	public async Task<ApiResult<UserResponse>> GetProfileAsync()
	{
		return await GetAsync<UserResponse>("api/users/profile");
	}
}

// Использование
var result = await _userClient.GetProfileAsync();
if (result.IsSuccess)
{
	var user = result.Data;
}
```

### Логирование

```csharp
using Microsoft.Extensions.Logging;

public class MyService
{
	private readonly ILogger<MyService> _logger;

	public MyService(ILogger<MyService> logger)
	{
		_logger = logger;
	}

	public void MyMethod()
	{
		_logger.LogInformation("Выполняю операцию");
		_logger.LogWarning("Предупреждение о чём-то");
		_logger.LogError("Произошла ошибка");
	}
}
```

Логи сохраняются в: `%APPDATA%\Runia\logs\`

---

## 🌐 API и сервисы

### API Клиенты

#### AuthClient
```csharp
// Регистрация
var registerResult = await authClient.RegisterAsync(new RegisterRequest
{
	Email = "user@example.com",
	Password = "password",
	Username = "username"
});

// Вход
var loginResult = await authClient.LoginAsync(new LoginRequest
{
	Email = "user@example.com",
	Password = "password"
});

// Подтверждение email
await authClient.ConfirmEmailAsync(confirmationToken);

// Восстановление пароля
await authClient.ResetPasswordAsync(new ResetPasswordRequest
{
	Email = "user@example.com",
	Token = resetToken,
	NewPassword = "newPassword"
});
```

#### UserClient
```csharp
// Получить профиль
var profile = await userClient.GetProfileAsync();

// Обновить профиль
await userClient.UpdateProfileAsync(new UserUpdateRequest { /* ... */ });

// Загрузить скин
await userClient.UploadSkinAsync(skinFile);
```

#### ServerClient
```csharp
// Получить список серверов
var servers = await serverClient.GetServersAsync();

// Получить информацию о сервере
var serverInfo = await serverClient.GetServerDetailsAsync(serverId);

// Получить игроков на сервере
var players = await serverClient.GetServerPlayersAsync(serverId);
```

#### NewsClient
```csharp
// Получить новости
var news = await newsClient.GetNewsAsync();

// Получить детали новости
var article = await newsClient.GetNewsDetailsAsync(newsId);
```

#### SessionClient
```csharp
// Получить информацию о сеансе
var session = await sessionClient.GetSessionAsync();

// Выйти из аккаунта
await sessionClient.LogoutAsync();
```

### Основные сервисы

#### LauncherService
Главный сервис для управления запуском Minecraft.

```csharp
public class LauncherService
{
	// События
	public event Action? OnGameStarted;
	public event Action? OnGameExited;

	// Методы
	public async Task<bool> CheckAndInstallJavaAsync();
	public async Task<bool> InstallForgeAsync(string version);
	public async Task<bool> InstallModPackAsync(string modPackName);
	public async Task StartGameAsync(LaunchOptions options);
}
```

#### AuthService
Управление аутентификацией пользователя.

```csharp
public class AuthService
{
	public async Task RegisterAsync(RegisterRequest request);
	public async Task LoginAsync(LoginRequest request);
	public async Task LogoutAsync();
	public bool IsAuthenticated { get; }
	public string? CurrentToken { get; }
}
```

#### NavigationService
Навигация между страницами/экранами.

```csharp
public interface INavigationService
{
	void NavigateTo(string pageName);
	void GoBack();
	string CurrentPage { get; }
	event EventHandler<string>? PageChanged;
}
```

#### ThemeManager
Управление темой приложения.

```csharp
public class ThemeManager
{
	public void SetTheme(ThemeType theme); // Dark или Light
	public ThemeType CurrentTheme { get; }
}
```

#### ImageProvider
Загрузка и кеширование изображений.

```csharp
public async Task<ImageSource?> GetImageAsync(string url, string? cacheKey = null);
```

---

## ⚙️ Конфигурация

### appsettings.json

```json
{
  "Runia": {
	"ApiBaseUrl": "https://api.runia.su/"
  },
  "Note": "Приложение читает RUNIA_API_BASE_URL из переменных окружения. Этот файл служит ориентиром/примером и копируется в выходную папку."
}
```

### Переменные окружения

Приложение поддерживает следующие переменные окружения:

- `RUNIA_API_BASE_URL` - Базовый URL API сервера (по умолчанию: `https://api.runia.su/`)
- `RUNIA_LOG_LEVEL` - Уровень логирования (по умолчанию: `Information`)

**Пример установки переменной (PowerShell):**
```powershell
$env:RUNIA_API_BASE_URL="https://api.example.com/"
```

### Файл конфигурации приложения (IAppConfig)

```csharp
public interface IAppConfig
{
	string ApiBaseUrl { get; }
	string GameDirectory { get; }
	string ModPacksDirectory { get; }
	// ... другие настройки
}
```

#### Значения по умолчанию (DefaultAppConfig)
- **ApiBaseUrl**: Переменная окружения или `appsettings.json`
- **GameDirectory**: `%APPDATA%\Runia\.minecraft\`
- **ModPacksDirectory**: `%APPDATA%\Runia\modpacks\`

---

## 🔄 Жизненный цикл приложения

### Инициализация приложения

1. **App.xaml.cs OnStartup**
   - Создание Host и конфигурация DI контейнера
   - Регистрация всех сервисов

2. **Регистрация сервисов**
   - Инфраструктура (конфиг, логирование, хранилище)
   - HTTP клиенты для API
   - Бизнес-сервисы (Auth, Launcher, Navigation)
   - ViewModels для страниц

3. **LoginWindow**
   - Первое окно - экран входа
   - Проверка сохранённого токена

4. **MainWindow**
   - Основное приложение после успешной аутентификации
   - Инициализация SideBar и основного контента

### Поток аутентификации

```
Пользователь вводит учётные данные
	↓
LoginViewModel.LoginCommand
	↓
AuthService.LoginAsync()
	↓
AuthClient.LoginAsync() → API запрос
	↓
SecureTokenStore сохраняет JWT токен
	↓
Переход на MainWindow
	↓
Загрузка профиля пользователя
```

### События жизненного цикла игры

```
LauncherService.StartGameAsync()
	↓
OnGameStarted event (запуск игры)
	↓
Игра работает...
	↓
OnGameExited event (выход из игры)
	↓
Логирование статистики
```

---

## 🐛 Диагностика и отладка

### Просмотр логов

Логи приложения сохраняются в:
```
%APPDATA%\Runia\logs\runia_YYYY-MM-DD.log
```

**Пример содержимого лога:**
```
[2024-01-15 10:30:45] Information: Application started
[2024-01-15 10:30:46] Information: User authenticated: user@example.com
[2024-01-15 10:31:00] Warning: Java not found, attempting installation
[2024-01-15 10:31:45] Information: Java 17 installed successfully
[2024-01-15 10:32:00] Information: Game started
```

### Включение расширенного логирования

Установите переменную окружения:
```powershell
$env:RUNIA_LOG_LEVEL="Debug"
```

### Очистка кеша приложения

```bash
# Удалить кеш изображений
rm $env:APPDATA\Runia\cache\images\*

# Удалить логи
rm $env:APPDATA\Runia\logs\*
```

### Решение распространённых проблем

| Проблема | Решение |
|----------|---------|
| **Ошибка подключения к API** | Проверьте интернет, переменную `RUNIA_API_BASE_URL` и доступность сервера |
| **Java не найдена** | Лаунчер попытается её установить автоматически, или установите вручную JDK 8+ |
| **Проблемы с токеном** | Удалите сохранённый токен из `securestore.dat` и переавторизуйтесь |
| **Проблемы с графикой** | Обновите драйверы видеокарты, проверьте .NET 9 Desktop Runtime |
| **Долгая загрузка модов** | Это нормально при первой установке, проверьте скорость интернета |

---

## 📚 Дополнительные ресурсы

### Используемые библиотеки
- **CmlLib.Core** - Minecraft Launcher Library
- **Microsoft.Xaml.Behaviors.Wpf** - WPF behaviors для привязок
- **Microsoft.Extensions.*** - DI, Logging, Configuration (.NET Extensions)
- **System.Text.Json** - JSON сериализация

### Документация проекта
- 📖 [Архитектура (Architecture.md)](./Architecture.md)
- 🔧 [Руководство контриьютора (Contributing.md)](./Contributing.md)
- 📋 [Изменения (CHANGELOG.md)](./CHANGELOG.md)

### Внешние ресурсы
- [Документация .NET WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [CmlLib.Core GitHub](https://github.com/AlphaBs/CmlLib.Core)
- [Minecraft Wiki](https://minecraft.wiki/)

---

## 📝 Лицензия

Проект распространяется под лицензией, указанной в файле [LICENSE](../LICENSE).

---

## 🤝 Контрибьютинг

Мы приветствуем вклад в развитие проекта! Пожалуйста, смотрите [Contributing.md](./Contributing.md) для подробной информации.

### Как сообщить об ошибке

1. Откройте [Issues на GitHub](https://github.com/mokkignomov-design/Launcher/issues)
2. Проверьте, не зарегистрирована ли ошибка
3. Создайте новую issue с:
   - Описанием проблемы
   - Шагами для воспроизведения
   - Версией ОС и .NET
   - Логами из `%APPDATA%\Runia\logs\`

---

## ❓ Часто задаваемые вопросы (FAQ)

**Q: Работает ли лаунчер на Linux/macOS?**
A: В текущей версии - нет, используется Windows Desktop Runtime. Возможна будущая портация на Avalonia.

**Q: Можно ли запустить игру в офлайн режиме?**
A: Нет, требуется интернет для аутентификации, но после входа можно играть без интернета.

**Q: Что делать если забыл пароль?**
A: Используйте функцию "Забыл пароль" на экране входа, восстановление придёт на почту, связанную с аккаунтом.

**Q: Где сохраняются мои данные лаунчера?**
A: В папке `%APPDATA%\Runia\` (локальные данные, конфиг, логи, кеш).

**Q: Поддерживаются ли модпаки?**
A: Да! ModPackManager поддерживает установку и управление модпаками.

---

**Последнее обновление документации**: 2024
**Версия проекта**: Runia Launcher v1.0+

🎮 Приятной игры с Runia Launcher!
