# Руководство по архитектуре Runia Launcher

## 📐 Общая архитектура

Runia Launcher использует **N-слойную архитектуру** с четким разделением ответственности:

```
┌──────────────────────────────────────────────┐
│          PRESENTATION LAYER (UI)             │
│  XAML Pages, Windows, Controls (WPF)         │
└──────────────┬───────────────────────────────┘
			   │
┌──────────────▼───────────────────────────────┐
│      VIEWMODEL LAYER (Business Logic)        │
│  ViewModels, RelayCommands, Bindings        │
└──────────────┬───────────────────────────────┘
			   │
┌──────────────▼───────────────────────────────┐
│         APPLICATION LAYER (Services)         │
│  Auth, Launcher, Navigation, Theme           │
└──────────────┬───────────────────────────────┘
			   │
┌──────────────▼───────────────────────────────┐
│      INFRASTRUCTURE LAYER                    │
│  Network, Logging, Config, Storage           │
└──────────────┬───────────────────────────────┘
			   │
┌──────────────▼───────────────────────────────┐
│         DOMAIN LAYER (Models)                │
│  Data Contracts, Entities, DTOs              │
└──────────────────────────────────────────────┘
```

---

## 🏛️ Слои архитектуры (подробно)

### 1. Domain Layer (Слой Доменных Модел)
**Объект**: Определение структуры данных  
**Файлы**: `Domain/Models/`

```csharp
// Models/UserModel.cs
public class UserModel
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string Email { get; set; }
	public byte[]? Skin { get; set; }
	public DateTime CreatedAt { get; set; }
}

// Models/ServerModel.cs
public class ServerModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Address { get; set; }
	public int CurrentPlayers { get; set; }
	public int MaxPlayers { get; set; }
}
```

**Отношения**: 
- Используются во всех слоях выше
- Не зависят от других слоёв (чистые POCO классы)

---

### 2. Infrastructure Layer (Инфраструктурный слой)
**Объект**: Техническая реализация (API, логирование, хранилище, конфиг)  
**Директория**: `Services/Core/Infrastructure/`

#### 2.1 Network Client (API клиенты)
```
Infrastructure/Network/
├── BaseApiService.cs        # Базовый класс для всех API клиентов
├── ApiResult.cs             # Результат API запроса (Success/Error)
├── JwtHandler.cs            # Обработка JWT токенов
├── ITokenStore.cs           # Интерфейс хранилища токенов
├── SecureTokenStore.cs      # Защищённое хранилище в Windows
├── ImageDownloader.cs       # Загрузка и кеширование изображений
└── Clients/
	├── AuthClient.cs        # API для аутентификации
	├── UserClient.cs        # API пользователей
	├── ServerClient.cs      # API серверов
	├── NewsClient.cs        # API новостей
	└── SessionClient.cs     # API сеансов
```

**Пример реализации**:
```csharp
public class BaseApiService(HttpClient httpClient)
{
	protected async Task<ApiResult<T>> GetAsync<T>(string endpoint)
	{
		try
		{
			var response = await httpClient.GetAsync(endpoint);
			var content = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var data = JsonSerializer.Deserialize<T>(content);
				return new ApiResult<T>(true, data);
			}

			return new ApiResult<T>(false, null, response.StatusCode.ToString());
		}
		catch (Exception ex)
		{
			return new ApiResult<T>(false, null, ex.Message);
		}
	}

	protected async Task<ApiResult<Unit>> PostAsync<TRequest>(
		string endpoint, TRequest request)
	{
		var json = JsonSerializer.Serialize(request);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var response = await httpClient.PostAsync(endpoint, content);
		return response.IsSuccessStatusCode
			? new ApiResult<Unit>(true, Unit.Value)
			: new ApiResult<Unit>(false, null, await response.Content.ReadAsStringAsync());
	}
}
```

#### 2.2 Logging Service
```
Infrastructure/Logging/
├── FileLogger.cs            # Логгер в файл
└── FileLoggerProvider.cs    # Провайдер логгера для DI
```

**Особенности**:
- Логи сохраняются каждый день в новый файл
- Директория: `%APPDATA%\Runia\logs\`
- Форматирование: `[timestamp] [level] message`

#### 2.3 Local Data Service
**Файл**: `Infrastructure/LocalDataService.cs`

```csharp
public interface ILocalDataService
{
	void SaveData<T>(string key, T data);
	T? LoadData<T>(string key);
	void DeleteData(string key);
}
```

Сохраняет данные локально в JSON формате.

#### 2.4 Configuration
```
Infrastructure/Configuration/
├── IAppConfig.cs            # Интерфейс конфигурации
└── DefaultAppConfig.cs      # Реализация по умолчанию
```

**Загрузка параметров**:
1. Переменные окружения
2. appsettings.json
3. Значения по умолчанию

---

### 3. Application Service Layer (Слой приложения)
**Объект**: Бизнес-логика приложения  
**Директория**: `Services/`

#### 3.1 Authentication Services
```
Services/Auth/
├── AuthManager.cs           # Управление аутентификацией
└── AuthService.cs           # Сервис аутентификации
```

```csharp
public class AuthService(AuthClient authClient, ITokenStore tokenStore)
{
	public async Task<bool> LoginAsync(LoginRequest request)
	{
		var result = await authClient.LoginAsync(request);

		if (result.IsSuccess)
		{
			await tokenStore.SaveTokenAsync(result.Data.Token);
			return true;
		}

		return false;
	}

	public async Task LogoutAsync()
	{
		await tokenStore.DeleteTokenAsync();
	}

	public bool IsAuthenticated => 
		tokenStore.GetTokenAsync().Result != null;
}
```

#### 3.2 Launcher Service
```
Services/Core/Launcher/
├── LauncherService.cs       # Главный сервис запуска игры
└── VersionsManager.cs       # Управление версиями Minecraft
```

```csharp
public class LauncherService(
	JavaManager javaManager,
	ForgeManager forgeManager,
	ModPackManager modPackManager,
	IAppConfig config,
	AuthService authService,
	SessionClient sessionClient)
{
	public event Action? OnGameStarted;
	public event Action? OnGameExited;

	public async Task StartGameAsync()
	{
		// 1. Проверить Java
		if (!await javaManager.ValidateJavaAsync())
		{
			await javaManager.InstallJavaAsync();
		}

		// 2. Установить Forge если нужен
		if (NeedsForge)
		{
			await forgeManager.InstallAsync();
		}

		// 3. Запустить игру через CmlLib
		var launcher = new CMLauncher(config.GameDirectory);
		var process = await launcher.BuildProcessAsync();

		OnGameStarted?.Invoke();

		process.WaitForExit();

		OnGameExited?.Invoke();
	}
}
```

#### 3.3 Third-party Managers
```
Services/Installers/
├── JavaManager.cs           # Управление Java
├── ForgeManager.cs          # Управление Forge
└── ModPackManager.cs        # Управление модпаками
```

#### 3.4 Common Services
```
Services/Common/
├── DialogService.cs         # Диалоги и notifications
├── ThemeManager.cs          # Переключение тем
├── SkinService.cs           # Управление скинами
└── ImageProvider.cs         # Загрузка и кеш изображений
```

#### 3.5 Core Services
```
Services/Core/
├── Navigation/
│   └── NavigationService.cs # Навигация между страницами
└── Persistence/
	└── DataManager.cs       # Управление сохранениями данных
```

---

### 4. ViewModel Layer (MVVM слой)
**Объект**: Логика представления и привязка данных  
**Директория**: `ViewModels/`

```csharp
public class LoginViewModel : ObservableObject
{
	private string _email = "";
	private string _password = "";
	private bool _isLoading;
	private string? _errorMessage;

	public string Email
	{
		get => _email;
		set => SetProperty(ref _email, value);
	}

	public string Password
	{
		get => _password;
		set => SetProperty(ref _password, value);
	}

	public bool IsLoading
	{
		get => _isLoading;
		set => SetProperty(ref _isLoading, value);
	}

	public string? ErrorMessage
	{
		get => _errorMessage;
		set => SetProperty(ref _errorMessage, value);
	}

	public IRelayCommand LoginCommand { get; }

	public LoginViewModel(AuthService authService)
	{
		LoginCommand = new AsyncRelayCommand(LoginAsync);
	}

	private async Task LoginAsync()
	{
		ErrorMessage = null;
		IsLoading = true;

		try
		{
			var success = await _authService.LoginAsync(new LoginRequest
			{
				Email = Email,
				Password = Password
			});

			if (!success)
			{
				ErrorMessage = "Неверные учётные данные";
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Ошибка: {ex.Message}";
		}
		finally
		{
			IsLoading = false;
		}
	}
}
```

**Жизненный цикл ViewModel**:
1. **Создание** - регистрируется в DI контейнере
2. **Использование** - привязывается к View (XAML)
3. **Команды** - IRelayCommand/AsyncRelayCommand реагируют на взаимодействие пользователя
4. **Уведомления** - ObservableObject уведомляет UI об изменениях

---

### 5. Presentation Layer (UI слой)
**Объект**: Интерфейс пользователя  
**Директория**: `Ui/`

#### 5.1 XAML страницы
```
Ui/Pages/
├── Auth/
│   ├── LoginPage.xaml
│   ├── LoginPage.xaml.cs
│   ├── RegisterPage.xaml
│   └── RegisterPage.xaml.cs
└── Main/
	├── HomePage.xaml
	├── ServerPage.xaml
	├── SettingsPage.xaml
	└── UserPage.xaml
```

```xml
<Page x:Class="Runia.Ui.Pages.Auth.LoginPage"
	  DataContext="{Binding Source={x:Static local:App.Services}, 
							Converter={x:Static local:ServiceLocatorConverter}, 
							ConverterParameter=Runia.ViewModels.Auth.LoginViewModel}">

	<StackPanel Margin="20">
		<TextBlock Text="Вход" FontSize="24" FontWeight="Bold" Margin="0,0,0,20"/>

		<TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}" 
				 Margin="0,0,0,10" 
				 PlaceholderText="Email"/>

		<PasswordBox x:Name="PasswordBox" 
					 Margin="0,0,0,10"/>

		<Button Command="{Binding LoginCommand}" 
				Content="Войти"
				IsEnabled="{Binding IsLoading, Converter={x:Static local:InvBoolConverter}}"/>

		<TextBlock Text="{Binding ErrorMessage}" 
				   Foreground="Red" 
				   TextWrapping="Wrap"/>
	</StackPanel>
</Page>
```

#### 5.2 Windows
```
Ui/Windows/
├── LoginWindow.xaml         # Окно входа
├── MainWindow.xaml          # Главное окно приложения
└── MessageWindow.xaml       # Диалоги и уведомления
```

#### 5.3 Controls (переиспользуемые компоненты)
```
Ui/Pages/Controls/
├── SideBar.xaml             # Боковая панель навигации
├── NotificationOverlay.xaml # Всплывающие уведомления
└── SkinView.cs              # Кастомный control для скинов
```

#### 5.4 Resources (стили и ресурсы)
```
assets/Resources/
├── CommonResources.xaml     # Общие ресурсы
├── Typography.xaml          # Стили текста
├── Buttons.xaml             # Стили кнопок
├── Cards.xaml               # Стили карточек
├── Inputs.xaml              # Стили полей ввода
└── ScrollsAndMedia.xaml     # Стили скроллов и медиа

assets/Themes/
├── DarkTheme.xaml           # Тёмная тема
└── LightTheme.xaml          # Светлая тема
```

---

## 🔄 Поток данных (Data Flow)

### Пример: Вход пользователя

```
1. UI Layer (LoginPage)
   ↓
   Пользователь вводит Email и Password
   ↓
   Нажимает кнопку "Войти"

2. ViewModel Layer (LoginViewModel)
   ↓
   LoginCommand.Execute()
   ↓
   LoginCommand = new AsyncRelayCommand(LoginAsync)

3. Application Service Layer (AuthService)
   ↓
   await AuthService.LoginAsync(loginRequest)

4. Infrastructure Layer (AuthClient)
   ↓
   POST /api/auth/login {email, password}
   ↓
   Получение JWT токена

5. Infrastructure Layer (SecureTokenStore)
   ↓
   Сохранение токена в защищённом хранилище

6. Back to ViewModel
   ↓
   UpdateProperty(IsAuthenticated = true)

7. Back to UI
   ↓
   Привязка XAML обновляется
   ↓
   NavigationService перенаправляет на MainWindow
```

---

## 🔌 Dependency Injection (DI) конфигурация

**Файл**: `App.xaml.cs`

```csharp
_host = Host.CreateDefaultBuilder()
	.ConfigureServices((context, services) =>
	{
		// Infrastructure
		services.AddSingleton<IAppConfig, DefaultAppConfig>();
		services.AddSingleton<ITokenStore, SecureTokenStore>();
		services.AddSingleton<IDialogService, DialogService>();
		services.AddSingleton<INavigationService, NavigationService>();

		// Logging
		services.AddLogging(builder =>
		{
			builder.AddProvider(new FileLoggerProvider());
		});

		// Network
		services.AddHttpClient<AuthClient>();
		services.AddHttpClient<UserClient>();
		services.AddHttpClient<ServerClient>();
		services.AddHttpClient<NewsClient>();
		services.AddHttpClient<SessionClient>();

		// Services
		services.AddSingleton<AuthService>();
		services.AddSingleton<LauncherService>();
		services.AddSingleton<JavaManager>();
		services.AddSingleton<ForgeManager>();
		services.AddSingleton<ModPackManager>();
		services.AddSingleton<ThemeManager>();
		services.AddSingleton<SkinService>();

		// ViewModels
		services.AddSingleton<LoginWindowViewModel>();
		services.AddSingleton<MainWindowViewModel>();
		services.AddSingleton<LoginViewModel>();
		services.AddSingleton<RegisterViewModel>();
		// ... остальные ViewModels
	})
	.Build();
```

---

## 🎯 Ключевые дизайн-паттерны

### 1. Singleton Pattern
Для сервисов, которые должны быть одним экземпляром на всё приложение:
```csharp
services.AddSingleton<AuthService>();
services.AddSingleton<ThemeManager>();
```

### 2. Factory Pattern
В BaseApiService для создания правильного HTTP клиента:
```csharp
services.AddHttpClient<AuthClient>()
	.ConfigureHttpClient(client => 
	{
		client.BaseAddress = new Uri(config.ApiBaseUrl);
		client.DefaultRequestHeaders.Add("User-Agent", "RuniaLauncher");
	});
```

### 3. Observer Pattern (MVVM)
Через ObservableObject и INotifyPropertyChanged:
```csharp
public class ViewModel : ObservableObject
{
	private string _property;

	public string Property
	{
		get => _property;
		set => SetProperty(ref _property, value);  // Уведомляет UI
	}
}
```

### 4. Command Pattern
Через IRelayCommand для обработки действий:
```csharp
public IRelayCommand LoginCommand { get; }

public ViewModel()
{
	LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
}
```

### 5. Repository Pattern
BaseApiService служит как Repository для API:
```csharp
public class UserRepository : BaseApiService
{
	public async Task<User> GetUserAsync(int id)
	{
		return await GetAsync<User>($"users/{id}");
	}
}
```

---

## 🧪 Тестируемость (Testability)

Архитектура спроектирована для удобного тестирования:

```csharp
// Мокирование сервис для тестирования ViewModel
public class LoginViewModelTests
{
	private readonly MockAuthService _mockAuthService;
	private readonly LoginViewModel _viewModel;

	public LoginViewModelTests()
	{
		_mockAuthService = new MockAuthService();
		_viewModel = new LoginViewModel(_mockAuthService);
	}

	[Test]
	public async Task Login_WithValidCredentials_ShouldSucceed()
	{
		// Arrange
		_mockAuthService.SetupLoginSuccess();
		_viewModel.Email = "test@example.com";
		_viewModel.Password = "password";

		// Act
		await _viewModel.LoginCommand.ExecuteAsync(null);

		// Assert
		Assert.IsFalse(_viewModel.IsLoading);
		Assert.IsNull(_viewModel.ErrorMessage);
	}
}
```

---

## 🎓 Заключение

Архитектура Runia обеспечивает:
- **Разделение ответственности** - каждый слой имеет чёткую задачу
- **Тестируемость** - зависимости внедряются, легко создавать моки
- **Масштабируемость** - новые страницы/сервисы добавляются без изменения существующих
- **Поддерживаемость** - чистый и понятный код

🏗️ **Итог**: N-Slayered MVVM архитектура с DI контейнером обеспечивает профессиональный и гибкий фундамент для разработки приложения!
