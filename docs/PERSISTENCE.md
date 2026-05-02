# 💾 Хранение данных в Runia

Документация по локальному хранилищу, кэшированию и управлению данными приложения.

## 📋 Обзор

Система хранения данных включает:
- **Локальное хранилище** - JSON файлы на диске
- **Безопасное хранилище** - Защифрованные токены (DPAPI)
- **Кэш в памяти** - Временные данные
- **Конфигурация** - Параметры приложения

## 🏗️ Структура хранилища

```
%APPDATA%\Runia\
├── SecureTokens/            # Защищённые JWT токены
│   └── secure_token.dat     # Зашифрованный токен (DPAPI)
├── Data/                    # Локальные данные
│   ├── user_profile.json
│   ├── servers_cache.json
│   ├── modpacks.json
│   └── installed_mods.json
├── Config/                  # Конфигурация
│   └── app_settings.json
└── Cache/                   # Временный кэш
    └── images/              # Кэш изображений
```

## 🔑 Основные компоненты

### 1. LocalDataService - Локальное хранилище

**Файл**: `Services/Core/Infrastructure/LocalDataService.cs`

```csharp
public interface ILocalDataService
{
    /// <summary>
    /// Сохранить данные в локальное хранилище (JSON)
    /// </summary>
    void SaveData<T>(string key, T data);

    /// <summary>
    /// Загрузить данные из локального хранилища
    /// </summary>
    T? LoadData<T>(string key);

    /// <summary>
    /// Удалить данные из хранилища
    /// </summary>
    void DeleteData(string key);

    /// <summary>
    /// Очистить все данные
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Проверить существование данных
    /// </summary>
    bool DataExists(string key);
}

public class LocalDataService : ILocalDataService
{
    private readonly string _dataPath = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Runia",
            "Data");

    private readonly JsonSerializerOptions _jsonOptions = 
        new JsonSerializerOptions { WriteIndented = true };

    public LocalDataService()
    {
        // Создать директорию если её нет
        Directory.CreateDirectory(_dataPath);
    }

    public void SaveData<T>(string key, T data)
    {
        try
        {
            var path = GetDataPath(key);
            var json = JsonSerializer.Serialize(data, _jsonOptions);

            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving data for key {key}: {ex.Message}");
        }
    }

    public T? LoadData<T>(string key)
    {
        try
        {
            var path = GetDataPath(key);

            if (!File.Exists(path))
                return default;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading data for key {key}: {ex.Message}");
            return default;
        }
    }

    public void DeleteData(string key)
    {
        try
        {
            var path = GetDataPath(key);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting data for key {key}: {ex.Message}");
        }
    }

    public void ClearAll()
    {
        try
        {
            if (Directory.Exists(_dataPath))
                Directory.Delete(_dataPath, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error clearing all data: {ex.Message}");
        }
    }

    public bool DataExists(string key)
    {
        return File.Exists(GetDataPath(key));
    }

    private string GetDataPath(string key)
    {
        // Санитизировать ключ для безопасного использования в имени файла
        var sanitizedKey = string.Concat(
            key.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
        );

        return Path.Combine(_dataPath, $"{sanitizedKey}.json");
    }
}
```

### 2. SecureTokenStore - Защищённое хранилище

**Файл**: `Services/Core/Infrastructure/Network/SecureTokenStore.cs`

```csharp
public interface ITokenStore
{
    /// <summary>
    /// Сохранить токен в защищённое хранилище (DPAPI)
    /// </summary>
    void SaveToken(string token);

    /// <summary>
    /// Получить токен из защищённого хранилища
    /// </summary>
    string? GetToken();

    /// <summary>
    /// Удалить токен
    /// </summary>
    void RemoveToken();

    /// <summary>
    /// Проверить наличие токена
    /// </summary>
    bool HasToken { get; }
}

public class SecureTokenStore : ITokenStore
{
    private const string TokenFileName = "secure_token.dat";
    private static readonly string StoragePath = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Runia",
            "SecureTokens");

    public SecureTokenStore()
    {
        Directory.CreateDirectory(StoragePath);
    }

    public void SaveToken(string token)
    {
        try
        {
            // Преобразить строку в байты
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(token);

            // Зашифровать с использованием DPAPI
            // DataProtectionScope.CurrentUser - только текущий пользователь может расшифровать
            byte[] encryptedData = ProtectedData.Protect(
                dataToEncrypt,
                null,
                DataProtectionScope.CurrentUser);

            // Сохранить зашифрованные данные
            var filePath = Path.Combine(StoragePath, TokenFileName);
            File.WriteAllBytes(filePath, encryptedData);

            Debug.WriteLine("Token saved securely");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving token: {ex.Message}");
        }
    }

    public string? GetToken()
    {
        try
        {
            var filePath = Path.Combine(StoragePath, TokenFileName);

            if (!File.Exists(filePath))
                return null;

            // Загрузить зашифрованные данные
            byte[] encryptedData = File.ReadAllBytes(filePath);

            // Расшифровать
            byte[] decryptedData = ProtectedData.Unprotect(
                encryptedData,
                null,
                DataProtectionScope.CurrentUser);

            // Преобразить обратно в строку
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading token: {ex.Message}");
            return null;
        }
    }

    public void RemoveToken()
    {
        try
        {
            var filePath = Path.Combine(StoragePath, TokenFileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.WriteLine("Token removed");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing token: {ex.Message}");
        }
    }

    public bool HasToken
    {
        get
        {
            var filePath = Path.Combine(StoragePath, TokenFileName);
            return File.Exists(filePath);
        }
    }
}
```

### 3. DataManager - Управление данными

**Файл**: `Services/Core/Infrastructure/Persistence/DataManager.cs`

```csharp
public class DataManager
{
    private readonly ILocalDataService _localStorage;
    private readonly BaseApiService _apiService;
    private readonly Dictionary<string, object?> _memoryCache = new();
    private readonly Dictionary<string, DateTime> _cacheTimes = new();
    private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromHours(1);

    public DataManager(
        ILocalDataService localStorage,
        BaseApiService apiService)
    {
        _localStorage = localStorage;
        _apiService = apiService;
    }

    /// <summary>
    /// Получить данные с кэшированием (память → диск → API)
    /// </summary>
    public async Task<T?> GetDataAsync<T>(
        string key,
        string apiEndpoint,
        TimeSpan? cacheExpiration = null)
    {
        var expiration = cacheExpiration ?? _defaultCacheExpiration;

        // 1. Проверить кэш в памяти
        if (_memoryCache.TryGetValue(key, out var cachedData))
        {
            if (_cacheTimes.TryGetValue(key, out var cacheTime))
            {
                if (DateTime.UtcNow - cacheTime < expiration)
                {
                    Debug.WriteLine($"Data retrieved from memory cache: {key}");
                    return (T?)cachedData;
                }
            }
        }

        try
        {
            // 2. Загрузить из API
            var result = await _apiService.GetAsync<T>(apiEndpoint);

            if (result.IsSuccess && result.Data != null)
            {
                // Сохранить в кэш памяти
                _memoryCache[key] = result.Data;
                _cacheTimes[key] = DateTime.UtcNow;

                // Сохранить на диск
                _localStorage.SaveData(key, result.Data);

                Debug.WriteLine($"Data retrieved from API: {key}");
                return result.Data;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching from API: {ex.Message}");
        }

        // 3. Загрузить с диска как fallback
        var diskData = _localStorage.LoadData<T>(key);
        if (diskData != null)
        {
            _memoryCache[key] = diskData;
            _cacheTimes[key] = DateTime.UtcNow;
            Debug.WriteLine($"Data retrieved from disk: {key}");
        }

        return diskData;
    }

    /// <summary>
    /// Сохранить данные локально и отправить на сервер
    /// </summary>
    public async Task<Result<bool>> SaveDataAsync<T>(
        string key,
        T data,
        string apiEndpoint)
    {
        try
        {
            // Сохранить локально
            _localStorage.SaveData(key, data);
            _memoryCache[key] = data;
            _cacheTimes[key] = DateTime.UtcNow;

            // Отправить на сервер
            var result = await _apiService.PostAsync<T, bool>(apiEndpoint, data);

            return result;
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                NetworkError.Unknown,
                ex.Message);
        }
    }

    /// <summary>
    /// Инвалидировать кэш для конкретного ключа
    /// </summary>
    public void InvalidateCache(string key)
    {
        _memoryCache.Remove(key);
        _cacheTimes.Remove(key);
        _localStorage.DeleteData(key);
        Debug.WriteLine($"Cache invalidated for: {key}");
    }

    /// <summary>
    /// Инвалидировать весь кэш
    /// </summary>
    public void InvalidateAllCache()
    {
        _memoryCache.Clear();
        _cacheTimes.Clear();
        _localStorage.ClearAll();
        Debug.WriteLine("All cache invalidated");
    }

    /// <summary>
    /// Получить статистику кэша
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        return new CacheStatistics
        {
            MemoryCacheItems = _memoryCache.Count,
            TotalCachedKeys = _cacheTimes.Count,
            OldestCacheEntry = _cacheTimes.Values.MinOrDefault(),
            NewestCacheEntry = _cacheTimes.Values.MaxOrDefault()
        };
    }
}

public class CacheStatistics
{
    public int MemoryCacheItems { get; set; }
    public int TotalCachedKeys { get; set; }
    public DateTime OldestCacheEntry { get; set; }
    public DateTime NewestCacheEntry { get; set; }
}
```

## 💾 Примеры использования

### Пример 1: Сохранение профиля пользователя

```csharp
public class UserService
{
    private readonly DataManager _dataManager;

    public async Task SaveUserProfileAsync(User user)
    {
        var result = await _dataManager.SaveDataAsync(
            key: "user_profile",
            data: user,
            apiEndpoint: "api/users/profile"
        );

        if (result.IsSuccess)
        {
            Debug.WriteLine("Profile saved successfully");
        }
    }

    public async Task<User?> GetUserProfileAsync()
    {
        return await _dataManager.GetDataAsync<User>(
            key: "user_profile",
            apiEndpoint: "api/users/profile",
            cacheExpiration: TimeSpan.FromMinutes(30)
        );
    }
}
```

### Пример 2: Кэширование списка серверов

```csharp
public class ServerService
{
    private readonly DataManager _dataManager;

    public async Task<List<Server>?> GetServersAsync(int page = 1)
    {
        var servers = await _dataManager.GetDataAsync<List<Server>>(
            key: $"servers_page_{page}",
            apiEndpoint: $"api/servers?page={page}&pageSize=20",
            cacheExpiration: TimeSpan.FromHours(1)
        );

        return servers;
    }

    public void RefreshServers()
    {
        // Инвалидировать кэш серверов
        _dataManager.InvalidateCache("servers_page_1");
        _dataManager.InvalidateCache("servers_page_2");
    }
}
```

### Пример 3: Безопасное сохранение токена

```csharp
public class AuthService
{
    private readonly ITokenStore _tokenStore;

    public void SaveAuthToken(string token)
    {
        // Автоматически шифруется с DPAPI
        _tokenStore.SaveToken(token);
    }

    public string? GetAuthToken()
    {
        // Автоматически расшифровывается
        return _tokenStore.GetToken();
    }

    public void ClearAuthToken()
    {
        _tokenStore.RemoveToken();
    }
}
```

## 🔐 Безопасность хранения

### DPAPI (Data Protection API)

DPAPI используется для шифрования токенов:

```csharp
// Шифрование
byte[] dataToEncrypt = Encoding.UTF8.GetBytes(sensitiveData);
byte[] encryptedData = ProtectedData.Protect(
    dataToEncrypt,
    null,
    DataProtectionScope.CurrentUser);  // Только текущий пользователь

// Расшифровка
byte[] decryptedData = ProtectedData.Unprotect(
    encryptedData,
    null,
    DataProtectionScope.CurrentUser);
var originalData = Encoding.UTF8.GetString(decryptedData);
```

**Особенности**:
- Ключ привязан к текущему пользователю Windows
- Данные сохраняются на диск зашифрованными
- Если пользователь изменится, расшифровать невозможно

### Хранение паролей

**НИКОГДА** не сохраняйте пароли локально!

```csharp
// ❌ НЕПРАВИЛЬНО: Не сохраняйте пароли
_tokenStore.SaveToken(password);  // ОПАСНО!

// ✅ ПРАВИЛЬНО: Сохраняйте только токены
_tokenStore.SaveToken(jwtToken);  // OK
```

## 📊 Структура данных в локальном хранилище

### user_profile.json

```json
{
  "id": "user123",
  "email": "user@example.com",
  "nickname": "MyNickname",
  "avatar": "https://example.com/avatar.jpg",
  "joinedDate": "2024-01-15T10:30:00Z",
  "lastLoginDate": "2024-01-20T14:45:00Z"
}
```

### servers_cache.json

```json
[
  {
    "id": "server1",
    "name": "Main Server",
    "address": "192.168.1.1:25565",
    "players": 25,
    "maxPlayers": 50,
    "status": "online"
  }
]
```

### installed_mods.json

```json
[
  {
    "id": "mod1",
    "name": "Mod Name",
    "version": "1.0.0",
    "installPath": "C:\\Games\\Minecraft\\mods\\",
    "installed": true
  }
]
```

## ⚙️ Регистрация сервисов

**Файл**: `App.xaml.cs`

```csharp
services.AddSingleton<ILocalDataService, LocalDataService>();
services.AddSingleton<ITokenStore, SecureTokenStore>();
services.AddSingleton<DataManager>();
```

## 🧪 Тестирование хранилища

```csharp
[Test]
public void SaveAndLoadData_ShouldSucceed()
{
    var service = new LocalDataService();
    var testData = new { Name = "Test", Value = 42 };

    // Сохранить
    service.SaveData("test_key", testData);

    // Загрузить
    var loaded = service.LoadData<dynamic>("test_key");

    Assert.IsNotNull(loaded);
    Assert.AreEqual("Test", loaded.Name);
}

[Test]
public void SecureTokenStore_ShouldEncryptToken()
{
    var store = new SecureTokenStore();
    var token = "my_secret_token_xyz";

    store.SaveToken(token);
    var retrieved = store.GetToken();

    Assert.AreEqual(token, retrieved);
}

[Test]
public async Task DataManager_ShouldCacheData()
{
    var manager = new DataManager(localStorage, apiService);

    // Первый вызов - загрузит с API
    var data1 = await manager.GetDataAsync<User>(
        "user", 
        "api/users/1");

    // Второй вызов - загрузит из кэша
    var data2 = await manager.GetDataAsync<User>(
        "user", 
        "api/users/1");

    // Должны быть одинаковыми
    Assert.AreEqual(data1?.Id, data2?.Id);
}
```

## 🔗 Связанные документы

- [Сетевое взаимодействие](./NETWORK.md)
- [Сервисы и инфраструктура](./SERVICES.md)
- [Архитектура](./ARCHITECTURE.md)

---

**Примечание**: Для полной защиты данных используйте HTTPS при передаче по сети и DPAPI при локальном хранении.
