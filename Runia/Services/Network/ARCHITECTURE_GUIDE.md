# Архитектура Network Clients

## Обзор

В папке `Services/Network/Clients` находятся специализированные клиенты для работы с различными направлениями API:

- **AuthClient** - Аутентификация (login, register, reset password)
- **ServerClient** - Работа с серверами (список, информация, присоединение)
- **NewsClient** - Работа с новостями (список, детали, подписки)
- **UserClient** - Данные пользователя (профиль, статистика, достижения, друзья)

## Структура результата

Каждый клиент возвращает типизированный результат:

```csharp
// С данными
ApiResult<T> result = await serverClient.GetServersAsync();
if (result.IsSuccess)
{
    var servers = result.Data;  // IEnumerable<ServerPack>
}
else
{
    var errorMessage = result.ErrorMessage;  // Текст ошибки
}

// Без данных (например, операции, которые просто выполняются)
ApiResult result = await userClient.AddFriendAsync("username");
if (result.IsSuccess)
{
    // Успех
}
else
{
    var errorMessage = result.ErrorMessage;
}
```

## Примеры использования

### ServerClient - Получение списка серверов

```csharp
public class ServerViewModel
{
    private readonly ServerClient _serverClient;

    public ServerViewModel(ServerClient serverClient)
    {
        _serverClient = serverClient;
    }

    [RelayCommand]
    private async Task LoadServersAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _serverClient.GetServersAsync();

            if (!result.IsSuccess)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            Servers.Clear();
            foreach (var server in result.Data ?? [])
            {
                Servers.Add(server);
            }
        }
        finally { IsLoading = false; }
    }
}
```

### NewsClient - Получение новостей с пагинацией

```csharp
public class NewsViewModel
{
    private readonly NewsClient _newsClient;

    public NewsViewModel(NewsClient newsClient)
    {
        _newsClient = newsClient;
    }

    [RelayCommand]
    private async Task LoadLatestNewsAsync()
    {
        var result = await _newsClient.GetLatestNewsAsync(page: 1, pageSize: 10);

        if (result.IsSuccess && result.Data != null)
        {
            News = new ObservableCollection<NewsItem>(result.Data.Items);
            TotalPages = result.Data.TotalPages;
        }
    }
}
```

### UserClient - Получение профиля

```csharp
public class UserViewModel
{
    private readonly UserClient _userClient;

    public UserViewModel(UserClient userClient)
    {
        _userClient = userClient;
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        var profileResult = await _userClient.GetCurrentUserProfileAsync();
        var statsResult = await _userClient.GetUserStatsAsync();

        if (profileResult.IsSuccess && statsResult.IsSuccess)
        {
            CurrentProfile = profileResult.Data;
            UserStats = statsResult.Data;
        }
    }

    [RelayCommand]
    private async Task UpdateProfileAsync(string bio)
    {
        var result = await _userClient.UpdateProfileAsync(
            new UpdateUserProfileRequest { Bio = bio }
        );

        if (result.IsSuccess)
        {
            CurrentProfile = result.Data;
            DialogService.ShowSuccess("Профиль обновлен");
        }
        else
        {
            DialogService.ShowError(result.ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task AddFriendAsync(string username)
    {
        var result = await _userClient.AddFriendAsync(username);

        if (result.IsSuccess)
        {
            DialogService.ShowSuccess($"{username} добавлен в друзья");
        }
        else
        {
            DialogService.ShowError(result.ErrorMessage);
        }
    }
}
```

### AuthClient - Логин и регистрация

```csharp
public class LoginViewModel
{
    private readonly AuthClient _authClient;

    public LoginViewModel(AuthClient authClient)
    {
        _authClient = authClient;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        var result = await _authClient.LoginAsync(Username, Password);

        if (result.IsSuccess && result.Data != null)
        {
            // Сохранить токен
            await TokenStore.SaveTokenAsync(result.Data.AccessToken);
            NavigateTo("MainWindow");
        }
        else
        {
            ShowError(result.ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var result = await _authClient.RegisterAsync(Username, Email, Password);

        if (result.IsSuccess)
        {
            DialogService.ShowSuccess("Учетная запись создана. Проверьте email.");
        }
        else
        {
            DialogService.ShowError(result.ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task RequestPasswordResetAsync()
    {
        var result = await _authClient.RequestPasswordResetAsync(Email);

        if (result.IsSuccess)
        {
            DialogService.ShowSuccess("Инструкция отправлена на email");
        }
        else
        {
            DialogService.ShowError(result.ErrorMessage);
        }
    }
}
```

## Преимущества такой архитектуры

1. **Разделение ответственности** - Каждый клиент отвечает за одно направление API
2. **Чистые ViewModels** - Они получают готовые данные, не знают о HTTP
3. **Легко тестировать** - Можно мокировать каждый клиент отдельно
4. **Обработка ошибок в одном месте** - Все HTTP логирование и обработка в клиентах
5. **Переиспользуемость** - Клиенты можно использовать везде в приложении
6. **Масштабируемость** - Легко добавлять новые клиенты для новых направлений API

## Как добавить новый клиент

1. Создайте класс в папке `Services/Network/Clients`:

```csharp
public class ItemClient
{
    private readonly BaseApiService _apiService;
    private const string BaseEndpoint = "/api/items";

    public ItemClient(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResult<IEnumerable<Item>>> GetItemsAsync(CancellationToken ct = default)
    {
        var result = await _apiService.GetAsync<IEnumerable<Item>>(BaseEndpoint, ct);

        if (!result.IsSuccess)
        {
            return ApiResult<IEnumerable<Item>>.Failure(
                result.ErrorMessage ?? "Ошибка загрузки предметов"
            );
        }

        return ApiResult<IEnumerable<Item>>.Success(result.Data ?? []);
    }
}
```

2. Зарегистрируйте в `App.xaml.cs`:

```csharp
services.AddSingleton<ItemClient>();
```

3. Используйте в ViewModels:

```csharp
public class ItemViewModel
{
    private readonly ItemClient _itemClient;

    public ItemViewModel(ItemClient itemClient)
    {
        _itemClient = itemClient;
    }
}
```

## Обработка ошибок

```csharp
var result = await _serverClient.GetServersAsync();

if (!result.IsSuccess)
{
    // result.ErrorMessage содержит текст ошибки
    // Внутри клиента уже произведено логирование в BaseApiService
    _dialogService.ShowError(result.ErrorMessage ?? "Неизвестная ошибка");
    return;
}

// Работаем с данными
foreach (var server in result.Data ?? [])
{
    // ...
}
```

## Работа с отменой операций

Все методы поддерживают `CancellationToken`:

```csharp
private CancellationTokenSource? _loadingCts;

[RelayCommand]
private async Task LoadAsync()
{
    _loadingCts = new CancellationTokenSource();
    try
    {
        var result = await _serverClient.GetServersAsync(_loadingCts.Token);
        // ...
    }
    catch (OperationCanceledException)
    {
        // Операция была отменена
    }
}

[RelayCommand]
private void CancelLoading()
{
    _loadingCts?.Cancel();
}
```
