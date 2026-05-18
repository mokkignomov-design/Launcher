# Рекомендации по переходу от старой архитектуры к Network Clients

## Что было сделано

Создана новая архитектура **Network/Clients** с четырьмя специализированными клиентами:

### 📁 Структура папок
```
Services/
└── Network/
    ├── Clients/
    │   ├── AuthClient.cs      ✅ Аутентификация
    │   ├── ServerClient.cs    ✅ Серверы
    │   ├── NewsClient.cs      ✅ Новости
    │   └── UserClient.cs      ✅ Данные пользователя
    ├── ApiResult.cs           ✅ Типизированный результат
    ├── ARCHITECTURE_GUIDE.md  📖 Подробный гайд
    └── QUICK_REFERENCE.md     📖 Шпаргалка
```

### ✅ Обновленные ViewModels
1. **ServerViewModel** - теперь использует `ServerClient` вместо `IAppConfig`
2. **HomeViewModel** - теперь использует `NewsClient` вместо `NewsService`
3. **UserViewModel** - теперь использует `UserClient` для загрузки профиля

### ✅ Обновленные сервисы
- **BaseApiService** - добавлены методы `PutAsync` и `DeleteAsync`
- **App.xaml.cs** - зарегистрированы все 4 новых клиента в DI контейнере

---

## План миграции остальных ViewModels

### Этап 1: Критические ViewModels (неделя 1)
```csharp
// SecurityUserViewModel - использует UserClient
[RelayCommand]
private async Task ChangePasswordAsync(string current, string newPassword)
{
    var result = await _userClient.ChangePasswordAsync(current, newPassword);
    if (result.IsSuccess)
    {
        DialogService.ShowSuccess("Пароль изменен");
    }
}

// AuthViewModels - используют AuthClient
public class LoginViewModel
{
    [RelayCommand]
    private async Task LoginAsync()
    {
        var result = await _authClient.LoginAsync(Username, Password);
        if (result.IsSuccess && result.Data != null)
        {
            // Сохранить токен
            // Переход на главное окно
        }
    }
}
```

### Этап 2: Вторичные ViewModels (неделя 2)
```csharp
// NewsDetailsViewModel - использует NewsClient
private async Task LoadNewsDetailAsync(string newsId)
{
    var result = await _newsClient.GetNewsDetailAsync(newsId);
    if (result.IsSuccess)
    {
        NewsDetail = result.Data;
    }
}

// ServerDetailsViewModel - использует ServerClient
private async Task LoadServerInfoAsync(string serverId)
{
    var result = await _serverClient.GetServerByIdAsync(serverId);
    if (result.IsSuccess)
    {
        ServerInfo = result.Data;
    }
}
```

### Этап 3: Дополнительные сервисы (неделя 3)
```csharp
// PaymentHistoryUserViewModel - использует UserClient
private async Task LoadActivityAsync()
{
    var result = await _userClient.GetUserActivityAsync(page: 1, pageSize: 20);
    if (result.IsSuccess)
    {
        Activities = new ObservableCollection<UserActivity>(result.Data ?? []);
    }
}

// Друзья - использует UserClient
private async Task LoadFriendsAsync()
{
    var result = await _userClient.GetFriendsAsync();
    if (result.IsSuccess)
    {
        Friends = new ObservableCollection<UserFriend>(result.Data ?? []);
    }
}
```

---

## Пример миграции: SecurityUserViewModel

### ❌ До (старый код)
```csharp
public class SecurityUserViewModel
{
    private readonly IAppConfig _config;
    private readonly BaseApiService _apiService;

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        try
        {
            var response = await _apiService.PostAsync<ChangePasswordDto, object>(
                $"{_config.ApiBaseUrl}/api/users/change-password",
                new { CurrentPassword, NewPassword }
            );

            if (!response.IsSuccess)
            {
                ShowError("Ошибка изменения пароля");
                return;
            }

            ShowSuccess("Пароль изменен");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }
}
```

### ✅ После (новый код)
```csharp
public class SecurityUserViewModel
{
    private readonly UserClient _userClient;

    public SecurityUserViewModel(UserClient userClient)
    {
        _userClient = userClient;
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        var result = await _userClient.ChangePasswordAsync(CurrentPassword, NewPassword);

        if (result.IsSuccess)
        {
            DialogService.ShowSuccess("Пароль изменен");
        }
        else
        {
            DialogService.ShowError(result.ErrorMessage);
        }
    }
}
```

**Преимущества:**
- ✅ Нет боилерплейта с try-catch
- ✅ Единое место для обработки ошибок
- ✅ Ясная семантика ошибок
- ✅ Нет need для знания URL endpoint'ов

---

## Чек-лист по переходу каждого ViewModel

Когда обновляете любой ViewModel:

1. **Замените зависимости:**
   ```csharp
   // Было
   private readonly IAppConfig _config;
   private readonly BaseApiService _api;

   // Стало
   private readonly ServerClient _serverClient;
   // или любой другой нужный клиент
   ```

2. **Обновите конструктор:**
   ```csharp
   // Было
   public MyViewModel(IAppConfig config) { ... }

   // Стало
   public MyViewModel(ServerClient serverClient) { ... }
   ```

3. **Замените вызовы API:**
   ```csharp
   // Было
   var result = await _api.GetAsync<T>($"{_config.ApiBaseUrl}/api/...");
   if (!result.IsSuccess) { /* handle error */ }

   // Стало
   var result = await _serverClient.GetServersAsync();
   if (!result.IsSuccess) { /* handle error */ }
   ```

4. **Обновите App.xaml.cs:**
   ```csharp
   // Добавьте новый ViewModel с нужным клиентом
   services.AddTransient<MyViewModel>(); // DI автоматически инъектит ServerClient
   ```

---

## Типичные ошибки и как их избежать

### ❌ Ошибка 1: Смешивание старой и новой архитектуры
```csharp
// Плохо: смешивание подходов
public class MyViewModel
{
    private readonly ServerClient _serverClient;
    private readonly BaseApiService _apiService;  // ❌ Не нужно!
}
```

### ✅ Решение:
```csharp
// Хорошо: только специализированный клиент
public class MyViewModel
{
    private readonly ServerClient _serverClient;  // ✅
}
```

### ❌ Ошибка 2: Забыли обновить DI регистрацию
```csharp
// App.xaml.cs
services.AddTransient<MyViewModel>(); // ❌ Ошибка! UserClient не зарегистрирован
```

### ✅ Решение:
```csharp
// App.xaml.cs
services.AddSingleton<UserClient>();  // ✅ Регистрируем клиент
services.AddTransient<MyViewModel>();  // ✅ Теперь работает
```

### ❌ Ошибка 3: Неправильная обработка null данных
```csharp
// Плохо: может быть NullReferenceException
var items = result.Data;  // Может быть null!
foreach (var item in items) { }  // ❌ Crash!
```

### ✅ Решение:
```csharp
// Хорошо: используем null-coalescing
foreach (var item in result.Data ?? [])  // ✅ Если null, используем пустой список
{
}
```

---

## Параллельное запущение нескольких операций

```csharp
[RelayCommand]
private async Task LoadAllDataAsync()
{
    IsLoading = true;
    try
    {
        // Параллельно загружаем все данные
        var profileTask = _userClient.GetCurrentUserProfileAsync();
        var statsTask = _userClient.GetUserStatsAsync();
        var achievementsTask = _userClient.GetAchievementsAsync();

        await Task.WhenAll(profileTask, statsTask, achievementsTask);

        if (profileTask.Result.IsSuccess)
            CurrentProfile = profileTask.Result.Data;

        if (statsTask.Result.IsSuccess)
            UserStats = statsTask.Result.Data;

        if (achievementsTask.Result.IsSuccess)
            Achievements = new(achievementsTask.Result.Data ?? []);
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

## Советы по оптимизации

### 1. Кэширование данных
```csharp
private UserProfile? _cachedProfile;

[RelayCommand]
private async Task LoadProfileAsync()
{
    if (_cachedProfile != null)
    {
        CurrentProfile = _cachedProfile;
        return;
    }

    var result = await _userClient.GetCurrentUserProfileAsync();
    if (result.IsSuccess && result.Data != null)
    {
        _cachedProfile = result.Data;  // Кэшируем
        CurrentProfile = result.Data;
    }
}
```

### 2. Отмена долгих операций
```csharp
private CancellationTokenSource? _loadingCts;

[RelayCommand]
private async Task LoadDataAsync()
{
    _loadingCts = new CancellationTokenSource();
    try
    {
        var result = await _serverClient.GetServersAsync(_loadingCts.Token);
        // ...
    }
    catch (OperationCanceledException)
    {
        // Операция была отменена пользователем
    }
}

[RelayCommand]
private void CancelLoading()
{
    _loadingCts?.Cancel();
}
```

### 3. Логирование и мониторинг
```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    _logger.LogInformation("Starting data load");

    var result = await _serverClient.GetServersAsync();

    if (!result.IsSuccess)
    {
        _logger.LogError("Failed to load servers: {Error}", result.ErrorMessage);
        return;
    }

    _logger.LogInformation("Successfully loaded {Count} servers", result.Data?.Count());
}
```

---

## Проверка миграции

Используйте эту таблицу для отслеживания прогресса:

| ViewModel | Статус | Клиент | Примечание |
|-----------|--------|--------|-----------|
| ServerViewModel | ✅ Готов | ServerClient | Обновлен на LoadServersAsync |
| HomeViewModel | ✅ Готов | NewsClient | Обновлен на GetLatestNewsAsync |
| UserViewModel | ✅ Готов | UserClient | Загрузка профиля при инициализации |
| LoginViewModel | ⏳ TODO | AuthClient | Нужно обновить Login/Register |
| RegisterViewModel | ⏳ TODO | AuthClient | Нужно обновить Register |
| ForgotPasswordViewModel | ⏳ TODO | AuthClient | Нужно обновить ResetPassword |
| ServerDetailsViewModel | ⏳ TODO | ServerClient | Нужно обновить GetServerById |
| NewsDetailsViewModel | ⏳ TODO | NewsClient | Нужно обновить GetNewsDetail |
| SecurityUserViewModel | ⏳ TODO | UserClient | Нужно обновить ChangePassword |
| PaymentHistoryUserViewModel | ⏳ TODO | UserClient | Нужно обновить GetActivity |

---

## Вопросы и ответы

**Q: Могу ли я использовать оба подхода?**
A: Да, но это не рекомендуется. Лучше постепенно мигрировать все на новую архитектуру.

**Q: Что если API endpoint еще не готов?**
A: Создайте Mock клиент для тестирования:
```csharp
public class MockServerClient : ServerClient
{
    public override async Task<ApiResult<IEnumerable<ServerPack>>> GetServersAsync(CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return ApiResult<IEnumerable<ServerPack>>.Success(new[]
        {
            new ServerPack { Id = "1", Title = "Тестовый сервер" }
        });
    }
}
```

**Q: Как тестировать ViewModels?**
A: Создайте Mock клиенты:
```csharp
[Test]
public async Task LoadServers_WhenSuccessful_PopulatesCollection()
{
    var mockClient = new Mock<ServerClient>();
    mockClient
        .Setup(x => x.GetServersAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(ApiResult<IEnumerable<ServerPack>>.Success(new[] { ... }));

    var vm = new ServerViewModel(mockClient.Object, /* ... */);

    Assert.AreEqual(1, vm.Servers.Count);
}
```

---

## Время выполнения миграции

- **ServerViewModel**: 15 минут ✅ Готово
- **HomeViewModel**: 15 минут ✅ Готово
- **UserViewModel**: 20 минут ✅ Готово
- **Остальные ViewModels**: ~2-3 часа на 10 ViewModels

**Итого**: ~4 часа на полную миграцию всех ViewModels
