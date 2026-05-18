# Быстрый старт Network Clients

## Инъекция в ViewModel

```csharp
public class MyViewModel
{
    private readonly ServerClient _serverClient;
    private readonly NewsClient _newsClient;
    private readonly UserClient _userClient;
    private readonly AuthClient _authClient;

    public MyViewModel(
        ServerClient serverClient,
        NewsClient newsClient,
        UserClient userClient,
        AuthClient authClient
    )
    {
        _serverClient = serverClient;
        _newsClient = newsClient;
        _userClient = userClient;
        _authClient = authClient;
    }
}
```

## Типичный паттерн

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    IsLoading = true;
    try
    {
        var result = await _serverClient.GetServersAsync();

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        Items.Clear();
        foreach (var item in result.Data ?? [])
        {
            Items.Add(item);
        }
    }
    finally { IsLoading = false; }
}
```

## Методы по клиентам

### AuthClient
- `LoginAsync(username, password)` → `AuthResponse`
- `RegisterAsync(username, email, password)` → `AuthResponse`
- `RequestPasswordResetAsync(email)` → success/fail
- `ConfirmPasswordResetAsync(token, newPassword)` → success/fail
- `RefreshTokenAsync(refreshToken)` → `AuthResponse`
- `LogoutAsync()` → success/fail
- `IsUsernameAvailableAsync(username)` → `bool`
- `IsEmailAvailableAsync(email)` → `bool`

### ServerClient
- `GetServersAsync()` → `IEnumerable<ServerPack>`
- `GetServerByIdAsync(serverId)` → `ServerPack`
- `GetPlayerInfoAsync(serverId, playerName)` → `ServerPlayerInfo`
- `JoinServerAsync(serverId)` → `JoinServerResponse`

### NewsClient
- `GetNewsAsync()` → `IEnumerable<NewsItem>`
- `GetLatestNewsAsync(page, pageSize)` → `PaginatedNews`
- `GetNewsDetailAsync(newsId)` → `NewsItem`
- `GetNewsByCategoryAsync(category)` → `IEnumerable<NewsItem>`
- `SubscribeToCategoryAsync(category)` → success/fail
- `UnsubscribeFromCategoryAsync(category)` → success/fail

### UserClient
- `GetCurrentUserProfileAsync()` → `UserProfile`
- `GetUserProfileByNameAsync(username)` → `UserProfile`
- `GetUserStatsAsync()` → `UserStats`
- `GetUserActivityAsync(page, pageSize)` → `IEnumerable<UserActivity>`
- `UpdateProfileAsync(request)` → `UserProfile`
- `GetAchievementsAsync()` → `IEnumerable<Achievement>`
- `GetFriendsAsync()` → `IEnumerable<UserFriend>`
- `AddFriendAsync(username)` → success/fail
- `RemoveFriendAsync(username)` → success/fail
- `ChangePasswordAsync(currentPassword, newPassword)` → success/fail

## Типы результатов

```csharp
// С данными
ApiResult<T> result;
if (result.IsSuccess)
{
    T data = result.Data;           // Данные (может быть null)
}
else
{
    string error = result.ErrorMessage;  // Текст ошибки
}

// Без данных
ApiResult result;
if (result.IsSuccess)
{
    // Успех
}
else
{
    string error = result.ErrorMessage;
}
```

## C# 12 Collection Expressions

При инициализации используются коллекции с `[]`:

```csharp
private ObservableCollection<ServerPack> _servers = [];
public IEnumerable<T> GetItems() => result.Data ?? [];
```
