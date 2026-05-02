# 🎨 UI/UX - Пользовательский интерфейс

Документация по дизайну, структуре и компонентам пользовательского интерфейса.

## 📋 Обзор

UI слой включает:
- **Окна (Windows)**: Главные окна приложения
- **Страницы (Pages)**: Содержимое внутри окон
- **Компоненты (Controls)**: Переиспользуемые элементы
- **Темы (Themes)**: Оформление и стили
- **XAML разметка**: Описание интерфейса

## 🏗️ Структура интерфейса

```
Ui/
├── Windows/                           # Главные окна
│   ├── LoginWindow.xaml              # Окно входа
│   ├── LoginWindow.xaml.cs
│   ├── MainWindow.xaml               # Главное окно
│   ├── MainWindow.xaml.cs
│   ├── MessageWindow.xaml            # Окно сообщений
│   └── MessageWindow.xaml.cs
│
├── Pages/
│   ├── Auth/                         # Страницы аутентификации
│   │   ├── LoginPage.xaml
│   │   ├── LoginPage.xaml.cs
│   │   ├── RegisterPage.xaml
│   │   ├── RegisterPage.xaml.cs
│   │   ├── ConfirmEmailPage.xaml
│   │   ├── ConfirmEmailPage.xaml.cs
│   │   ├── ForgotPasswordPage.xaml
│   │   ├── ForgotPasswordPage.xaml.cs
│   │   ├── ResetPasswordPage.xaml
│   │   └── ResetPasswordPage.xaml.cs
│   │
│   ├── Main/                         # Основные страницы
│   │   ├── HomePage.xaml
│   │   ├── HomePage.xaml.cs
│   │   ├── ServerPage.xaml
│   │   ├── ServerPage.xaml.cs
│   │   ├── UserPage.xaml
│   │   ├── UserPage.xaml.cs
│   │   ├── SettingsPage.xaml
│   │   ├── SettingsPage.xaml.cs
│   │   └── TemplatePages/
│   │       ├── TemplateNewsDetailsPage.xaml
│   │       ├── TemplateNewsDetailsPage.xaml.cs
│   │       ├── TemplateServerDetailsPage.xaml
│   │       └── TemplateServerDetailsPage.xaml.cs
│   │
│   └── Controls/                     # Компоненты
│       ├── SideBar.xaml
│       └── SideBar.xaml.cs
│
└── assets/                           # Ресурсы
    ├── Themes/
    │   ├── CommonResource.xaml       # Общие ресурсы
    │   ├── DarkTheme.xaml           # Тёмная тема
    │   ├── LightTheme.xaml          # Светлая тема
    │   └── Styles.xaml              # Стили компонентов
    ├── Fonts/
    │   ├── Gabriela.ttf
    │   └── Hypereffort.otf
    ├── Icons/
    │   ├── Icon_v2.ico
    │   ├── discord.png
    │   └── telegramm.png
    └── Images/
        ├── Back.png
        ├── Back2.png
        └── news_default.png
```

## 🪟 Главные окна

### 1. LoginWindow - Окно входа

**Файл**: `Ui/Windows/LoginWindow.xaml`

```xaml
<Window x:Class="Runia.Ui.Windows.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Runia - Вход"
        Width="400"
        Height="600"
        WindowStyle="None"
        Background="{DynamicResource BackgroundBrush}"
        DataContext="{Binding LoginWindowViewModel, Source={StaticResource Locator}}">
    <Grid>
        <!-- Контент для страниц аутентификации -->
        <ContentControl Content="{Binding CurrentPage}" />
    </Grid>
</Window>
```

**ViewModel**: `ViewModels/Windows/LoginWindowViewModel.cs`

```csharp
public partial class LoginWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object? currentPage;

    public LoginWindowViewModel(
        INavigationService nav,
        IDialogService dialog)
    {
        _nav = nav;
        _dialog = dialog;

        // Показать страницу входа по умолчанию
        CurrentPage = new LoginPage 
        { 
            DataContext = new LoginViewModel(nav, dialog) 
        };
    }

    [RelayCommand]
    private void NavigateToRegister()
    {
        CurrentPage = new RegisterPage 
        { 
            DataContext = new RegisterViewModel() 
        };
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        CurrentPage = new LoginPage 
        { 
            DataContext = new LoginViewModel() 
        };
    }
}
```

### 2. MainWindow - Главное окно

**Файл**: `Ui/Windows/MainWindow.xaml`

```xaml
<Window x:Class="Runia.Ui.Windows.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Runia - Главное окно"
        Width="1200"
        Height="700"
        Background="{DynamicResource BackgroundBrush}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="250" />      <!-- SideBar -->
            <ColumnDefinition Width="*" />        <!-- Основной контент -->
        </Grid.ColumnDefinitions>

        <!-- SideBar (левая панель) -->
        <local:SideBar Grid.Column="0" />

        <!-- Основной контент -->
        <ContentControl Grid.Column="1" Content="{Binding CurrentPage}" />
    </Grid>
</Window>
```

**ViewModel**: `ViewModels/Windows/MainWindowViewModel.cs`

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object? currentPage;

    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _nav;

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        INavigationService nav)
    {
        _serviceProvider = serviceProvider;
        _nav = nav;

        // Показать главную страницу по умолчанию
        NavigateToHome();
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        CurrentPage = _serviceProvider.GetRequiredService<HomePage>();
    }

    [RelayCommand]
    private void NavigateToServer()
    {
        CurrentPage = _serviceProvider.GetRequiredService<ServerPage>();
    }

    [RelayCommand]
    private void NavigateToUser()
    {
        CurrentPage = _serviceProvider.GetRequiredService<UserPage>();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPage = _serviceProvider.GetRequiredService<SettingsPage>();
    }
}
```

### 3. MessageWindow - Окно сообщений

**Файл**: `Ui/Windows/MessageWindow.xaml`

```xaml
<Window x:Class="Runia.Ui.Windows.MessageWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="{Binding Title}"
        Width="400"
        Height="200"
        WindowStartupLocation="CenterScreen"
        Background="{DynamicResource BackgroundBrush}">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="20">
        <TextBlock Text="{Binding Message}" 
                   FontSize="16"
                   TextWrapping="Wrap"
                   Foreground="{DynamicResource TextBrush}" />
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
            <Button Command="{Binding OkCommand}" Content="OK" Margin="5" />
            <Button Command="{Binding CancelCommand}" Content="Отмена" Margin="5" />
        </StackPanel>
    </StackPanel>
</Window>
```

## 📄 Страницы аутентификации

### 1. LoginPage - Вход

```xaml
<Page x:Class="Runia.Ui.Pages.Auth.LoginPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      Title="Вход">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="15">
        <TextBlock Text="Вход в аккаунт" FontSize="24" FontWeight="Bold" />

        <!-- Email -->
        <TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}"
                 Placeholder="Email"
                 Height="40"
                 Padding="10" />

        <!-- Пароль -->
        <PasswordBox local:PasswordBoxAttachmentBehavior.Password="{Binding Password}"
                     Height="40"
                     Padding="10" />

        <!-- Кнопки -->
        <Button Command="{Binding LoginCommand}" 
                Content="Вход"
                Height="40"
                Background="{DynamicResource AccentBrush}" />

        <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
            <Button Command="{Binding ForgotPasswordCommand}" 
                    Content="Забыли пароль?"
                    Style="{StaticResource HyperlinkButton}" />
            <Button Command="{Binding RegisterCommand}" 
                    Content="Зарегистрироваться"
                    Style="{StaticResource HyperlinkButton}" />
        </StackPanel>

        <!-- Сообщение об ошибке -->
        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="Red"
                   TextWrapping="Wrap" />
    </StackPanel>
</Page>
```

### 2. RegisterPage - Регистрация

```xaml
<Page x:Class="Runia.Ui.Pages.Auth.RegisterPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      Title="Регистрация">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="15" Width="350">
        <TextBlock Text="Создание аккаунта" FontSize="24" FontWeight="Bold" />

        <!-- Email -->
        <TextBlock Text="Email:" />
        <TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}"
                 Height="40"
                 Padding="10" />
        <TextBlock Text="{Binding EmailError}" Foreground="Red" FontSize="12" />

        <!-- Пароль -->
        <TextBlock Text="Пароль:" />
        <PasswordBox local:PasswordBoxAttachmentBehavior.Password="{Binding Password}"
                     Height="40"
                     Padding="10" />
        <TextBlock Text="Минимум 8 символов" FontSize="12" Foreground="Gray" />

        <!-- Подтверждение пароля -->
        <TextBlock Text="Подтверждение пароля:" />
        <PasswordBox local:PasswordBoxAttachmentBehavior.Password="{Binding ConfirmPassword}"
                     Height="40"
                     Padding="10" />
        <TextBlock Text="{Binding PasswordError}" Foreground="Red" FontSize="12" />

        <!-- Кнопки -->
        <Button Command="{Binding RegisterCommand}" 
                Content="Создать аккаунт"
                Height="40"
                Background="{DynamicResource AccentBrush}" />

        <Button Command="{Binding BackToLoginCommand}" 
                Content="← Вернуться к входу"
                Style="{StaticResource HyperlinkButton}"
                HorizontalAlignment="Left" />

        <!-- Общее сообщение об ошибке -->
        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="Red"
                   TextWrapping="Wrap" />
    </StackPanel>
</Page>
```

### 3. ConfirmEmailPage - Подтверждение email

```xaml
<Page x:Class="Runia.Ui.Pages.Auth.ConfirmEmailPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      Title="Подтверждение email">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="15" Width="350">
        <TextBlock Text="Подтверждение email" FontSize="24" FontWeight="Bold" />

        <TextBlock Text="На вашу почту отправлен код подтверждения."
                   TextWrapping="Wrap"
                   Foreground="Gray" />

        <!-- Email -->
        <TextBlock Text="Email:" />
        <TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}"
                 Height="40"
                 IsReadOnly="True"
                 Padding="10" />

        <!-- Код подтверждения -->
        <TextBlock Text="Код подтверждения:" />
        <TextBox Text="{Binding ConfirmationCode, UpdateSourceTrigger=PropertyChanged}"
                 Height="40"
                 Padding="10"
                 Placeholder="Введите 6-значный код" />

        <!-- Кнопки -->
        <Button Command="{Binding ConfirmCommand}" 
                Content="Подтвердить"
                Height="40"
                Background="{DynamicResource AccentBrush}" />

        <Button Command="{Binding ResendCodeCommand}" 
                Content="Отправить код заново"
                Style="{StaticResource HyperlinkButton}"
                HorizontalAlignment="Center" />

        <!-- Сообщение об ошибке -->
        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="Red"
                   TextWrapping="Wrap" />
    </StackPanel>
</Page>
```

## 📄 Основные страницы

### 1. HomePage - Главная страница

```xaml
<Page x:Class="Runia.Ui.Pages.Main.HomePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      Title="Главная">
    <Grid Padding="20">
        <StackPanel>
            <TextBlock Text="Добро пожаловать в Runia!"
                       FontSize="32"
                       FontWeight="Bold" />

            <TextBlock Text="Управляйте вашими игровыми серверами"
                       FontSize="16"
                       Margin="0,10,0,0"
                       Foreground="Gray" />

            <!-- Статистика -->
            <Grid Margin="0,40,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>

                <!-- Карточка статистики -->
                <Border Grid.Column="0" 
                        Background="{DynamicResource CardBackground}"
                        Padding="20"
                        CornerRadius="10"
                        Margin="10">
                    <StackPanel>
                        <TextBlock Text="Активных серверов"
                                   FontSize="14"
                                   Foreground="Gray" />
                        <TextBlock Text="{Binding ActiveServersCount}"
                                   FontSize="28"
                                   FontWeight="Bold" />
                    </StackPanel>
                </Border>

                <!-- Остальные карточки аналогично -->
            </Grid>
        </StackPanel>
    </Grid>
</Page>
```

### 2. ServerPage - Список серверов

```xaml
<Page x:Class="Runia.Ui.Pages.Main.ServerPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      Title="Серверы">
    <Grid Padding="20">
        <StackPanel>
            <TextBlock Text="Мои серверы" FontSize="28" FontWeight="Bold" />

            <ListBox ItemsSource="{Binding Servers}" 
                     SelectedItem="{Binding SelectedServer}">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Background="{DynamicResource CardBackground}"
                                Padding="15"
                                Margin="10"
                                CornerRadius="8">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*" />
                                    <ColumnDefinition Width="Auto" />
                                </Grid.ColumnDefinitions>

                                <StackPanel Grid.Column="0">
                                    <TextBlock Text="{Binding Name}" 
                                               FontSize="16"
                                               FontWeight="Bold" />
                                    <TextBlock Text="{Binding Address}" 
                                               FontSize="12"
                                               Foreground="Gray" />
                                </StackPanel>

                                <Button Grid.Column="1" 
                                        Content="Подробнее"
                                        Command="{Binding SelectCommand, 
                                            RelativeSource={RelativeSource AncestorType=ListBox}}" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </StackPanel>
    </Grid>
</Page>
```

## 🎨 Темы оформления

### CommonResource.xaml - Общие ресурсы

```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">

    <!-- Шрифты -->
    <FontFamily x:Key="MainFont">pack://application:,,,/assets/Fonts/Gabriela.ttf#Gabriela</FontFamily>
    <FontFamily x:Key="SecondaryFont">Arial</FontFamily>

    <!-- Стандартные размеры -->
    <system:Double x:Key="FontSizeSmall">12</system:Double>
    <system:Double x:Key="FontSizeNormal">14</system:Double>
    <system:Double x:Key="FontSizeLarge">16</system:Double>
    <system:Double x:Key="FontSizeXLarge">24</system:Double>

    <!-- Стандартные отступы -->
    <Thickness x:Key="PaddingSmall">5</Thickness>
    <Thickness x:Key="PaddingNormal">10</Thickness>
    <Thickness x:Key="PaddingLarge">20</Thickness>

    <!-- Стили кнопок -->
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Padding" Value="15,10" />
        <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}" />
        <Setter Property="Background" Value="{DynamicResource AccentBrush}" />
        <Setter Property="Foreground" Value="White" />
    </Style>

    <Style x:Key="HyperlinkButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Foreground" Value="{DynamicResource AccentBrush}" />
        <Setter Property="Cursor" Value="Hand" />
    </Style>

</ResourceDictionary>
```

### DarkTheme.xaml - Тёмная тема

```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">

    <!-- Основные цвета -->
    <Color x:Key="BackgroundColor">#1E1E1E</Color>
    <Color x:Key="ForegroundColor">#FFFFFF</Color>
    <Color x:Key="AccentColor">#007ACC</Color>
    <Color x:Key="CardBackgroundColor">#2D2D30</Color>
    <Color x:Key="BorderColor">#3E3E42</Color>

    <!-- Кисти -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="TextBrush" Color="{StaticResource ForegroundColor}" />
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="CardBackground" Color="{StaticResource CardBackgroundColor}" />

</ResourceDictionary>
```

### LightTheme.xaml - Светлая тема

```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">

    <!-- Основные цвета -->
    <Color x:Key="BackgroundColor">#FFFFFF</Color>
    <Color x:Key="ForegroundColor">#000000</Color>
    <Color x:Key="AccentColor">#0078D4</Color>
    <Color x:Key="CardBackgroundColor">#F5F5F5</Color>
    <Color x:Key="BorderColor">#E0E0E0</Color>

    <!-- Кисти -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="TextBrush" Color="{StaticResource ForegroundColor}" />
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="CardBackground" Color="{StaticResource CardBackgroundColor}" />

</ResourceDictionary>
```

## 🧩 Компоненты

### SideBar - Боковое меню

**Файл**: `Ui/Pages/Controls/SideBar.xaml`

```xaml
<UserControl x:Class="Runia.Ui.Pages.Controls.SideBar"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             Background="{DynamicResource CardBackground}">
    <Grid Padding="15">
        <StackPanel Spacing="10">
            <!-- Заголовок -->
            <TextBlock Text="Меню" FontSize="18" FontWeight="Bold" Margin="0,0,0,20" />

            <!-- Кнопки навигации -->
            <Button Command="{Binding NavigateToHomeCommand}" 
                    Content="🏠 Главная"
                    Height="40"
                    HorizontalContentAlignment="Left" />

            <Button Command="{Binding NavigateToServerCommand}" 
                    Content="🖥️ Серверы"
                    Height="40"
                    HorizontalContentAlignment="Left" />

            <Button Command="{Binding NavigateToUserCommand}" 
                    Content="👤 Профиль"
                    Height="40"
                    HorizontalContentAlignment="Left" />

            <Button Command="{Binding NavigateToSettingsCommand}" 
                    Content="⚙️ Настройки"
                    Height="40"
                    HorizontalContentAlignment="Left" />

            <!-- Выход -->
            <Separator Margin="0,20" />
            <Button Command="{Binding LogoutCommand}" 
                    Content="🚪 Выход"
                    Height="40"
                    HorizontalContentAlignment="Left"
                    Foreground="Red" />
        </StackPanel>
    </Grid>
</UserControl>
```

## 📊 Лучшие практики UI

### 1. Data Binding

```xaml
<!-- Хорошо: Используйте MVVM -->
<TextBox Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" />

<!-- Плохо: Избегайте code-behind логики -->
<TextBox x:Name="usernameBox" />
```

### 2. XAML стили

```xaml
<!-- Хорошо: Используйте ресурсы -->
<Button Style="{StaticResource PrimaryButton}" Content="OK" />

<!-- Плохо: Не дублируйте стили -->
<Button Background="#007ACC" Foreground="White" Content="OK" />
```

### 3. Валидация input

```xaml
<!-- Используйте Validation Rules -->
<TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged, 
    ValidatesOnDataErrors=True}">
    <Validation.ErrorTemplate>
        <ControlTemplate>
            <StackPanel>
                <AdornedElementPlaceholder />
                <ItemsControl ItemsSource="{Binding}" />
            </StackPanel>
        </ControlTemplate>
    </Validation.ErrorTemplate>
</TextBox>
```

## 🔗 Связанные документы

- [Навигация](./NAVIGATION.md)
- [Архитектура](./ARCHITECTURE.md)
- [API для разработчиков](./API_REFERENCE.md)

---

**Следующий шаг**: Изучите [Навигацию](./NAVIGATION.md) для добавления новых страниц.
