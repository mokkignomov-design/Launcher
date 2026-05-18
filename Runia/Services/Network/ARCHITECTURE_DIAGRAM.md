# Architecture Diagram

## 📐 Общая архитектура

```
┌─────────────────────────────────────────────────────────────────┐
│                        WPF UI Layer                             │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐ │
│  │   Login      │    Home      │   Server     │    User      │ │
│  │   Page       │    Page      │   Page       │    Page      │ │
│  └──────────────┴──────────────┴──────────────┴──────────────┘ │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│              ViewModel Layer (Presentation)                     │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐ │
│  │   Login      │    Home      │   Server     │    User      │ │
│  │   ViewModel  │   ViewModel  │   ViewModel  │   ViewModel  │ │
│  └──────────────┴──────────────┴──────────────┴──────────────┘ │
│                                                                  │
│  ⬇️  Инъекция зависимостей  ⬇️                                    │
│                                                                  │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐ │
│  │   AuthClient │   NewsClient │ ServerClient │  UserClient  │ │
│  │   ✓ Login    │   ✓ News     │  ✓ Servers   │  ✓ Profile   │ │
│  │   ✓ Register │   ✓ Details  │  ✓ Player    │  ✓ Stats     │ │
│  │   ✓ Password │   ✓ Category │  ✓ Join      │  ✓ Friends   │ │
│  └──────────────┴──────────────┴──────────────┴──────────────┘ │
│                                                                  │
│  Каждый клиент отвечает за ОДНО направление API ✨             │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│               Network Infrastructure Layer                      │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              BaseApiService                              │   │
│  │  ┌────────────┬────────────┬────────────┬────────────┐   │   │
│  │  │ GetAsync   │ PostAsync  │ PutAsync   │ DeleteAsync│   │   │
│  │  └────────────┴────────────┴────────────┴────────────┘   │   │
│  │                                                           │   │
│  │  ✓ Centralised logging (ILogger)                        │   │
│  │  ✓ JWT token handling (JwtHandler)                      │   │
│  │  ✓ Error handling & Result wrapping                     │   │
│  │  ✓ Timeout & CancellationToken support                  │   │
│  └──────────────────────────────────────────────────────────┘   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                        HTTP Client
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                    Backend API                                  │
│  /api/auth         /api/servers   /api/news     /api/users      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Поток данных (ViewModel → Client → API)

```
┌──────────────────────────────────────────────────────────────────┐
│ ServerViewModel                                                  │
│                                                                  │
│ [RelayCommand]                                                  │
│ async Task LoadServersAsync()                                   │
│ {                                                               │
│   IsLoading = true  ────────► UI обновляется                   │
│                                                                 │
│   var result = await _serverClient.GetServersAsync() ──────┐   │
│                                                            │   │
│   if (!result.IsSuccess)                                  │   │
│   {                                                        │   │
│     ErrorMessage = result.ErrorMessage                   │   │
│     DialogService.ShowError(...)                         │   │
│     return                                                │   │
│   }                                                        │   │
│                                                            │   │
│   foreach (var server in result.Data ?? [])              │   │
│   {                                                        │   │
│     Servers.Add(server)  ────► UI обновляется            │   │
│   }                                                        │   │
│                                                            │   │
│   IsLoading = false                                        │   │
│ }                                                           │   │
└──────────────────────────────────────────────────────────┼─────┘
                                                            │
┌───────────────────────────────────────────────────────────▼────┐
│ ServerClient                                                    │
│                                                                │
│ async Task<ApiResult<IEnumerable<ServerPack>>>                │
│   GetServersAsync(CancellationToken ct)                       │
│ {                                                              │
│   var result = await _apiService.GetAsync<IEnumerable        │
│     <ServerPack>>("/api/servers", ct)                        │
│                                                                │
│   if (!result.IsSuccess)                                      │
│     return ApiResult<IEnumerable<ServerPack>>                │
│       .Failure(result.ErrorMessage ?? "Ошибка")             │
│                                                                │
│   return ApiResult<IEnumerable<ServerPack>>                  │
│     .Success(result.Data ?? [])                             │
│ }                                                              │
└───────────────────────────────────────────────────────────┬────┘
                                                             │
┌───────────────────────────────────────────────────────────▼────┐
│ BaseApiService                                                  │
│                                                                │
│ async Task<Result<T>> GetAsync<T>(string url, ...)           │
│ {                                                              │
│   try                                                          │
│   {                                                            │
│     using var response = await httpClient                     │
│       .GetAsync(url, ...)                                     │
│                                                                │
│     if (!response.IsSuccessStatusCode)                        │
│     {                                                          │
│       logger.LogWarning(...)  ──► Application Insights        │
│       return Result<T>                                        │
│         .Fail(NetworkError.HttpError, msg)                   │
│     }                                                          │
│                                                                │
│     var data = await response.Content                         │
│       .ReadFromJsonAsync<T>(...)                             │
│     return Result<T>.Success(data)                           │
│   }                                                            │
│   catch (OperationCanceledException ex)                       │
│   {                                                            │
│     logger.LogWarning(ex, "GET cancelled")                   │
│     return Result<T>                                          │
│       .Fail(NetworkError.Timeout, ...)                       │
│   }                                                            │
│   catch (...)  ──► Все исключения обработаны централизованно │
│ }                                                              │
└───────────────────────────────────────────────────────────┬────┘
                                                             │
                        HTTP Request
                                                             │
                  GET /api/servers HTTP/1.1
                  Authorization: Bearer {token}
                  Accept: application/json
                                                             │
┌───────────────────────────────────────────────────────────▼────┐
│ Backend API Response                                            │
│                                                                │
│ HTTP/1.1 200 OK                                               │
│ Content-Type: application/json                               │
│                                                                │
│ [                                                              │
│   {                                                            │
│     "id": "1",                                                │
│     "title": "Skyblock",                                      │
│     "mcVersion": "1.20.1",                                    │
│     "online": 256,                                            │
│     ...                                                        │
│   },                                                           │
│   {                                                            │
│     "id": "2",                                                │
│     "title": "Creative",                                      │
│     ...                                                        │
│   }                                                            │
│ ]                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Dependency Injection (DI) Setup

```
┌───────────────────────────────────────────────────────────┐
│ App.xaml.cs OnStartup()                                   │
│                                                            │
│ Host.CreateDefaultBuilder()                              │
│   .ConfigureServices((ctx, services) =>                  │
│   {                                                       │
│     // 1. Инфраструктура                                │
│     services.AddSingleton<IAppConfig, DefaultAppConfig> │
│                                                           │
│     // 2. Регистрируем HttpClient с JWT Handler         │
│     services.AddTransient<JwtHandler>                    │
│     services.AddHttpClient<BaseApiService>(/*...*/       │
│       .AddHttpMessageHandler<JwtHandler>                 │
│                                                           │
│     // 3. Регистрируем Network Clients ⬇️                 │
│     services.AddSingleton<AuthClient>                    │
│     services.AddSingleton<ServerClient>                  │
│     services.AddSingleton<NewsClient>                    │
│     services.AddSingleton<UserClient>                    │
│                                                           │
│     // 4. Регистрируем ViewModels                        │
│     services.AddTransient<ServerViewModel>               │
│     // DI автоматически инъектит ServerClient            │
│     services.AddTransient<HomeViewModel>                 │
│     // DI автоматически инъектит NewsClient              │
│     services.AddSingleton<UserViewModel>                 │
│     // DI автоматически инъектит UserClient              │
│   })                                                      │
│   .Build()                                               │
│                                                           │
│ ⚡ Результат: Вся сеть автоматически связана             │
│ ⚡ ViewModels получают нужные клиенты через конструктор   │
│ ⚡ Если нужен новый клиент, просто добавляем параметр     │
│                                                            │
└───────────────────────────────────────────────────────────┘
```

---

## 📊 Типы результатов

```
┌─────────────────────────────────────────┐
│     ApiResult<T> Success Case           │
│                                         │
│  var result = await client.GetAsync()  │
│                                         │
│  if (result.IsSuccess)                 │
│  {                                      │
│    T data = result.Data  ─────► Данные │
│    // Работаем с данными               │
│  }                                      │
│                                         │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│     ApiResult<T> Failure Case           │
│                                         │
│  var result = await client.GetAsync()  │
│                                         │
│  if (!result.IsSuccess)                │
│  {                                      │
│    string error =                       │
│      result.ErrorMessage ──────► Ошибка│
│    // Показываем пользователю           │
│  }                                      │
│                                         │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│     ApiResult Success (без данных)      │
│                                         │
│  var result = await client.              │
│    AddFriendAsync("username")           │
│                                         │
│  if (result.IsSuccess)                 │
│  {                                      │
│    // Операция выполнена успешно        │
│  }                                      │
│  else                                   │
│  {                                      │
│    // Ошибка                            │
│  }                                      │
│                                         │
└─────────────────────────────────────────┘
```

---

## 🎬 Жизненный цикл операции

```
1. ViewModel вызывает ClientMethod()
   ↓
2. Клиент валидирует входные параметры
   ↓
3. Клиент вызывает BaseApiService.GetAsync()
   ↓
4. BaseApiService выполняет HTTP запрос
   ↓
5. Проверка статуса ответа
   ├─ ✓ 200-299 → Десериализация → Success()
   ├─ ✗ 401 → Fail(Unauthorized)
   ├─ ✗ 4xx-5xx → Fail(HttpError)
   └─ ✗ Exception → Catch & Fail(...)
   ↓
6. BaseApiService логирует (ILogger)
   ↓
7. Клиент оборачивает Result в ApiResult
   ↓
8. ViewModel получает типизированный результат
   ↓
9. ViewModel обновляет состояние (IsLoading, Items, Error)
   ↓
10. UI обновляется автоматически (через ObservableObject)
```

---

## 🔐 Безопасность

```
┌─────────────────────────────────────────┐
│ Request с JWT токеном                  │
│                                         │
│ GET /api/servers                        │
│ Authorization: Bearer eyJhbGc...        │  ◄── JwtHandler
│ Accept: application/json                │     добавляет
│                                         │     автоматически
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Обработка невалидного токена            │
│                                         │
│ HTTP/1.1 401 Unauthorized               │
│                                         │
│ BaseApiService:                         │
│ if (status == 401)                      │
│   return Fail(NetworkError.Unauthorized)│
│                                         │
│ Client оборачивает:                     │
│ return ApiResult.Failure("Unauthorized")│
│                                         │
│ ViewModel перенаправляет на Login       │
│                                         │
└─────────────────────────────────────────┘
```

---

## 📈 Масштабируемость: Добавление нового клиента

```
Была архитектура:          Стала:
ViewModels              ViewModels
  ↓                       ↓
BaseApiService    →    Clients (4)
  ↓                    (Auth, Server, News, User)
HTTP                       ↓
                       BaseApiService
                           ↓
                         HTTP

Добавляем новый клиент ItemClient:

ViewModels
  ↓
Clients (5)
(Auth, Server, News, User, Item) ← новый!
  ↓
BaseApiService
  ↓
HTTP

✓ ВСЁ ОСТАЛЬНОЕ НЕ ИЗМЕНИЛОСЬ!
✓ Скалируется линейно
✓ Не нарушает существующий код
```

---

**Диаграмма создана:** Январь 2025
**Версия архитектуры:** 1.0
**Язык:** C# 13 / .NET 9
