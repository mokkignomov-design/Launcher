# 🧭 Навигация в Runia

Документация по системе навигации между окнами и страницами приложения.

## 📋 Обзор

Система навигации в Runia отвечает за:
- Переключение между окнами (LoginWindow, MainWindow, MessageWindow)
- Переключение между страницами внутри окна
- Передачу параметров между страницами
- Управление жизненным циклом окон

## 🏗️ Архитектура навигации

```
┌──────────────────────────────┐
│    Navigation Service        │
│  (INavigationService)        │
└────────┬─────────────────────┘
         │
         ├─→ Window Navigation
         │   ├─ LoginWindow
         │   ├─ MainWindow
         │   └─ MessageWindow
         │
         └─→ Page Navigation (внутри MainWindow)
             ├─ HomePage
             ├─ ServerPage
             ├─ UserPage
             ├─ SettingsPage
             └─ Detail Pages
                 ├─ NewsDetailsPage
                 └─ ServerDetailsPage
```

## 🎯 Основные компоненты

### INavigationService - Интерфейс

**Файл**: `Services/Core/Navigation/INavigationService.cs`

```csharp
public interface INavigationService
{
    /// <summary>
    /// Навигация на новое окно без параметров
    /// </summary>
    void Navigate<T>() where T : Window;

    /// <summary>
    /// Навигация на новое окно с параметром
    /// </summary>
    void Navigate<T>(object? parameter) where T : Window;

    /// <summary>
    /// Навигация по типу
    /// </summary>
    void Navigate(Type windowType);

    /// <summary>
    /// Навигация на новую страницу
    /// </summary>
    void NavigateTo<T>() where T : Page;

    /// <summary>
    /// Выполнить после успешной навигации
    /// </summary>
    event EventHandler<NavigatedEventArgs>? Navigated;
}
```

### NavigationService - Реализация

**Файл**: `Services/Core/Navigation/NavigationService.cs`

```csharp
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDialogService _dialogService;

    public NavigationService(
        IServiceProvider serviceProvider, 
        IDialogService dialogService)
    {
        _serviceProvider = serviceProvider;
        _dialogService = dialogService;
    }

    public void Navigate<T>() where T : Window
    {
        // Получить окно из DI контейнера
        var window = _serviceProvider.GetRequiredService<T>();

        // Закрыть текущее окно
        CloseCurrentWindow();

        // Показать новое окно
        window.Show();
    }

    private void CloseCurrentWindow()
    {
        var current = Application.Current?.MainWindow;
        if (current != null && current != Application.Current?.Windows[0])
        {
            current.Close();
        }
    }
}
```

## 🪟 Навигация между окнами

### Типы окон

```
LoginWindow
    ├─ Появляется при первом запуске
    ├─ Показывает страницы аутентификации
    └─ Закрывается после успешного входа

MainWindow
    ├─ Главное окно приложения
    ├─ Содержит SideBar для навигации
    ├─ Содержит ContentControl для страниц
    └─ Открывается после входа

MessageWindow
    ├─ Всплывающее окно для сообщений
    ├─ Информационные сообщения
    ├─ Сообщения об ошибках
    └─ Подтверждение действий
```

### Пример: Навигация с входа на главное окно

```csharp
// В LoginViewModel.cs
[RelayCommand]
private async Task Login()
{
    var result = await _auth.LoginAsync(Username, Password);

    if (result.IsSuccess)
    {
        // Навигация на MainWindow
        _nav.Navigate<MainWindow>();
    }
}
```

### Пример: Открытие MessageWindow

```csharp
// Показать информационное сообщение
_nav.Navigate<MessageWindow>(
    new MessageWindowViewModel 
    { 
        Title = "Успешно!",
        Message = "Операция выполнена"
    });
```

## 📄 Навигация между страницами

### Механизм навигации между страницами

Внутри `MainWindow` используется `ContentControl` для отображения текущей страницы:

```xaml
<!-- MainWindow.xaml -->
<ContentControl Content="{Binding CurrentPage}" />
```

```csharp
// MainWindowViewModel.cs
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object? currentPage;

    [RelayCommand]
    private void NavigateToHome()
    {
        CurrentPage = new HomePage 
        { 
            DataContext = new HomeViewModel() 
        };
    }

    [RelayCommand]
    private void NavigateToServer()
    {
        CurrentPage = new ServerPage 
        { 
            DataContext = new ServerViewModel() 
        };
    }
}
```

### Типы страниц

#### Страницы аутентификации (Auth Pages)

Располагаются в `Ui/Pages/Auth/`:

```
├─ LoginPage.xaml
│  └─ LoginViewModel.cs
│  
├─ RegisterPage.xaml
│  └─ RegisterViewModel.cs
│  
├─ ForgotPasswordPage.xaml
│  └─ ForgotPasswordViewModel.cs
│  
├─ ResetPasswordPage.xaml
│  └─ ResetPasswordViewModel.cs
│  
└─ ConfirmEmailPage.xaml
   └─ ConfirmEmailViewModel.cs
```

**Используются в**: LoginWindow (LoginWindowViewModel)

#### Основные страницы (Main Pages)

Располагаются в `Ui/Pages/Main/`:

```
├─ HomePage.xaml           (Главная страница)
│  └─ HomeViewModel.cs
│  
├─ ServerPage.xaml         (Список серверов)
│  └─ ServerViewModel.cs
│  
├─ UserPage.xaml           (Профиль пользователя)
│  └─ UserViewModel.cs
│  
├─ SettingsPage.xaml       (Настройки)
│  └─ SettingsViewModel.cs
│  
└─ TemplatePages/          (Детальные страницы)
   ├─ TemplateNewsDetailsPage.xaml
   │  └─ NewsDetailsViewModel.cs
   │
   └─ TemplateServerDetailsPage.xaml
      └─ ServerDetailsViewModel.cs
```

**Используются в**: MainWindow (MainWindowViewModel)

## 🔄 Процессы навигации

### Процесс 1: Аутентификация

```
App.xaml.cs
    ↓ OnStartup()
LoginWindow (показывается)
    ↓
LoginPage
    ├─ RegisterButton → RegisterPage
    ├─ ForgotPasswordButton → ForgotPasswordPage
    └─ LoginButton + success
        ↓
    NavigationService.Navigate<MainWindow>()
        ↓
    LoginWindow закрывается
    MainWindow показывается
```

### Процесс 2: Навигация между основными страницами

```
MainWindow (всегда активно)
    ↓ SideBar Click
SidebarViewModel signals
    ↓
MainWindowViewModel
    ├─ NavigateToHomeCommand → CurrentPage = HomePage
    ├─ NavigateToServerCommand → CurrentPage = ServerPage
    ├─ NavigateToUserCommand → CurrentPage = UserPage
    └─ NavigateToSettingsCommand → CurrentPage = SettingsPage
        ↓
ContentControl обновляется
    ↓
Page отображается
```

### Процесс 3: Детальная страница

```
ServerPage (список серверов)
    ↓ Клик на сервер
ServerViewModel.SelectServerCommand(server)
    ↓
MainWindowViewModel.NavigateToDetailsCommand
    ↓
CurrentPage = TemplateServerDetailsPage
    ↓
ServerDetailsViewModel инициализируется с выбранным сервером
```

## 🎮 Примеры использования

### Пример 1: Простая навигация между страницами

```csharp
// В SidebarViewModel.cs
[RelayCommand]
private void NavigateToHome()
{
    _mainVM.NavigateToHome();
}

// В MainWindowViewModel.cs
[RelayCommand]
private void NavigateToHome()
{
    CurrentPage = _serviceProvider.GetRequiredService<HomePage>();
}
```

### Пример 2: Навигация с параметрами

```csharp
// Передача параметра
[RelayCommand]
private void ViewServerDetails(Server server)
{
    var page = new TemplateServerDetailsPage();
    var vm = _serviceProvider.GetRequiredService<ServerDetailsViewModel>();
    vm.SetServer(server);  // Передача параметра
    page.DataContext = vm;
    CurrentPage = page;
}

// В ViewModel
public partial class ServerDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private Server? selectedServer;

    public void SetServer(Server server)
    {
        SelectedServer = server;
        LoadServerData();
    }
}
```

### Пример 3: Навигация с диалогом

```csharp
// Показать диалог перед навигацией
[RelayCommand]
private async Task NavigateWithConfirmation()
{
    var confirmed = await _dialog.ShowConfirmAsync(
        "Вы уверены?", 
        "Ваши изменения будут потеряны");

    if (confirmed)
    {
        _nav.Navigate<MainWindow>();
    }
}
```

## 🛠️ Система SideBar (боковое меню)

### Структура SideBar

```
┌──────────────────┐
│    SideBar       │
├──────────────────┤
│  🏠 Home         │
├──────────────────┤
│  🖥️  Server      │
├──────────────────┤
│  👤 User         │
├──────────────────┤
│  ⚙️  Settings    │
├──────────────────┤
│  🚪 Logout       │
└──────────────────┘
```

### SideBar ViewModel

**Файл**: `ViewModels/Main/SidebarViewModel.cs`

```csharp
public partial class SidebarViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly MainWindowViewModel _mainVM;

    [ObservableProperty]
    private string selectedMenuItemName = "Home";

    [RelayCommand]
    private void NavigateToHome()
    {
        SelectedMenuItemName = "Home";
        _mainVM.NavigateToHome();
    }

    [RelayCommand]
    private void NavigateToServer()
    {
        SelectedMenuItemName = "Server";
        _mainVM.NavigateToServer();
    }

    [RelayCommand]
    private void Logout()
    {
        _nav.Navigate<LoginWindow>();
    }
}
```

## 📊 Иерархия навигации

```
Application (App.xaml)
    ↓
OnStartup Event
    ↓
    ├─→ ServiceProvider initialized
    ├─→ Themes loaded
    └─→ LoginWindow shown

LoginWindow
    ├─ LoginWindowViewModel
    │   ├─ CurrentPage
    │   ├─ Navigation to Auth Pages
    │   ├─ Navigate<MainWindow> on success
    │   └─ Show Register/ForgotPassword
    │
    └─ Content Area (ContentControl)
        └─ Current Auth Page
            ├─ LoginPage
            ├─ RegisterPage
            ├─ ConfirmEmailPage
            ├─ ForgotPasswordPage
            └─ ResetPasswordPage

MainWindow (после успешного входа)
    ├─ MainWindowViewModel
    │   ├─ CurrentPage (main content)
    │   └─ Navigation Commands
    │
    ├─ SideBar (левая панель)
    │   ├─ SidebarViewModel
    │   └─ Navigation Buttons
    │
    └─ ContentControl (основная область)
        └─ Current Main Page
            ├─ HomePage
            ├─ ServerPage
            ├─ UserPage
            ├─ SettingsPage
            └─ Detail Pages
                ├─ NewsDetailsPage
                └─ ServerDetailsPage
```

## ⚙️ Регистрация навигации в DI

**Файл**: `App.xaml.cs`

```csharp
// Регистрация окон
services.AddSingleton<LoginWindow>();
services.AddSingleton<MainWindow>();
services.AddSingleton<MessageWindow>();

// Регистрация auth pages
services.AddSingleton<LoginPage>();
services.AddSingleton<RegisterPage>();
services.AddSingleton<ConfirmEmailPage>();
services.AddSingleton<ForgotPasswordPage>();
services.AddSingleton<ResetPasswordPage>();

// Регистрация main pages
services.AddSingleton<HomePage>();
services.AddSingleton<ServerPage>();
services.AddSingleton<UserPage>();
services.AddSingleton<SettingsPage>();

// Регистрация ViewModels
services.AddSingleton<LoginWindowViewModel>();
services.AddSingleton<MainWindowViewModel>();
services.AddSingleton<SidebarViewModel>();
services.AddSingleton<LoginViewModel>();
services.AddSingleton<RegisterViewModel>();

// Регистрация сервиса
services.AddSingleton<INavigationService, NavigationService>();
```

## 🚀 Добавление новой страницы и навигации

### Шаг 1: Создайте XAML страницу

```xaml
<!-- NewPage.xaml -->
<Page x:Class="Runia.Ui.Pages.Main.NewPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      Title="New Page">
    <Grid>
        <TextBlock Text="Hello from NewPage!" />
    </Grid>
</Page>
```

### Шаг 2: Создайте ViewModel

```csharp
// NewViewModel.cs
public partial class NewViewModel : ObservableObject
{
    [RelayCommand]
    private void DoSomething()
    {
        // Логика
    }
}
```

### Шаг 3: Зарегистрируйте в App.xaml.cs

```csharp
services.AddSingleton<NewPage>();
services.AddSingleton<NewViewModel>();
```

### Шаг 4: Добавьте команду в MainWindowViewModel

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    [RelayCommand]
    private void NavigateToNew()
    {
        CurrentPage = _serviceProvider.GetRequiredService<NewPage>();
    }
}
```

### Шаг 5: Добавьте кнопку в SideBar

```xaml
<!-- SideBar.xaml -->
<Button Command="{Binding NavigateToNewCommand}" Content="New" />
```

## 🔗 Связанные документы

- [Архитектура](./ARCHITECTURE.md)
- [UI/UX](./UI_UX.md)
- [Система аутентификации](./AUTHENTICATION.md)

---

**Следующий шаг**: Изучите [UI/UX](./UI_UX.md) для создания красивых интерфейсов.
