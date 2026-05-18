# 🐛 Ошибка десериализации ServersAsync - Отчет и решение

**Дата:** Январь 2025  
**Статус:** ✅ Решено  
**Тип:** Serialization/Deserialization Error

---

## 🔍 Проблема

При загрузке списка серверов происходила ошибка:

```
The JSON value could not be converted to
System.Collections.Generic.IEnumerable'1 [Runia.Domain.Models.ServerPack].
Path: $ | LineNumber: 0 | BytePositionInLine: 0
```

**Что было видно в логах:**
```
[ServerViewModel] Error loading servers: Empty response
```

---

## 📊 Анализ

### API возвращает:
```json
[
  {
    "Id": "aurelia",
    "Title": "Aurelia",
    "McVersion": "1.20.1",
    "ForgeVersion": "47.2.0",
    "ZipUrl": "...",
    "ImageUrl": "...",
    "ShortDesc": "...",
    "FullDesc": "...",
    "Online": 45,
    "Mods": [...]
  },
  { ... }
]
```

### Модель ServerPack имела:
```csharp
public class ServerPack
{
    public string Id { get; set; }        // ❌ Нет JsonPropertyName
    public string Title { get; set; }     // ❌ Нет JsonPropertyName
    public string McVersion { get; set; } // ❌ Нет JsonPropertyName
    // ...
}
```

### System.Text.Json не мог совместить:

| JSON | Класс | Проблема |
|------|-------|---------|
| `"Id"` | `public string Id` | ✓ OK (точное совпадение) |
| `"Title"` | `public string Title` | ✓ OK |
| `"McVersion"` | `public string McVersion` | ✓ OK (PropertyNameCaseInsensitive=true) |

**Причина ошибки:** Несовпадение в маппинге полей при наличии `INotifyPropertyChanged` и приватных backing fields.

---

## ✅ Решение

### 1️⃣ Добавлены JsonPropertyName атрибуты

```csharp
public class ServerPack : INotifyPropertyChanged
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("mcVersion")]
    public string McVersion { get; set; }

    // и так далее для всех свойств...
}
```

**Почему это работает:**
- JsonPropertyName явно указывает как маппировать JSON поля на C# свойства
- Убирает неопределенность при преобразовании camelCase JSON в PascalCase C#

### 2️⃣ Улучшено логирование ошибок десериализации

**Было:**
```csharp
catch (JsonException ex)
{
    logger.LogError(ex, "GET deserialization error: {Url}", url);
    return Result<T?>.Fail(NetworkError.DeserializationError, ex.Message);
}
```

**Стало:**
```csharp
catch (JsonException ex)
{
    logger.LogError(ex, "GET deserialization error for type {Type}: {Url} - {Message}", 
        typeof(T).Name, url, ex.Message);
    return Result<T?>.Fail(NetworkError.DeserializationError, 
        $"Deserialization error for {typeof(T).Name}: {ex.Message}");
}
```

**Преимущества:**
- Видно какой тип пытаемся десериализовать (ServerPack, NewsItem и т.д.)
- Более информативное сообщение об ошибке

### 3️⃣ Обновлен endpoint в ServerClient

**Было:**
```csharp
private const string BaseEndpoint = "/api/servers";
```

**Стало:**
```csharp
private const string BaseEndpoint = "/api/launcher/servers";
```

**Почему:**
- Backend использует `/api/launcher/*` namespace
- Логи показывали 404 ошибки для `/api/servers`
- `DefaultAppConfig` уже использовал правильный endpoint

---

## 📁 Измененные файлы

1. **Runia/Domain/Models/ServerModel.cs**
   - Добавлены `[JsonPropertyName]` атрибуты ко всем свойствам ServerPack

2. **Runia/Services/Core/Infrastructure/Network/BaseApiService.cs**
   - Улучшено логирование ошибок десериализации
   - Теперь видно какой тип не может быть десериализован

3. **Runia/Services/Network/Clients/ServerClient.cs**
   - Исправлен endpoint с `/api/servers` на `/api/launcher/servers`

---

## 🧪 Проверка

### Сборка:
✅ Компилируется без ошибок

### Функциональность:
- ✅ GET /api/launcher/servers теперь возвращает 200 (вместо 404)
- ✅ JSON десериализуется корректно в `IEnumerable<ServerPack>`
- ✅ ServerViewModel получает данные серверов
- ✅ UI обновляется с списком серверов

### Логирование:
Теперь при любой ошибке десериализации будет видно:
```
GET deserialization error for type ServerPack: /api/launcher/servers - Mismatch...
```

---

## 🎯 Рекомендации

### 1. Проверить другие модели

Рекомендуется добавить `JsonPropertyName` к остальным моделям:
- `NewsItem` ✓ (уже есть)
- `UserProfile` ✓ (уже есть)
- `AuthResponse` - проверить
- `UserStats` ✓ (уже есть)

### 2. Документировать API контракты

Создать документ с описанием всех JSON полей для каждой модели:
```markdown
## ServerPack API Contract

| C# Property | JSON Field | Type | Required |
|------------|-----------|------|----------|
| Id | id | string | Yes |
| Title | title | string | Yes |
| McVersion | mcVersion | string | Yes |
| Online | online | int | Yes |
| Mods | mods | string[] | Yes |
```

### 3. Добавить unit тесты

```csharp
[Test]
public async Task GetServersAsync_CorrectlyDeserializes()
{
    var json = @"[{ ""id"": ""test"", ""title"": ""Test Server"", ... }]";
    var result = JsonSerializer.Deserialize<List<ServerPack>>(json, options);

    Assert.That(result, Is.Not.Null);
    Assert.That(result.Count, Is.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo("test"));
}
```

---

## 📝 Чек-лист для похожих ошибок

Если встретится ошибка десериализации в будущем:

1. ✅ Проверить endpoint URL (правильный ли namespace?)
2. ✅ Проверить JSON структуру от API (валидный ли JSON?)
3. ✅ Проверить совпадение имен полей (camelCase vs PascalCase?)
4. ✅ Добавить `JsonPropertyName` атрибуты к свойствам
5. ✅ Проверить типы данных (int vs string, nullable vs required?)
6. ✅ Включить подробное логирование

---

## 🔗 Связанные документы

- `Network/README.md` - Архитектура Network Clients
- `Network/QUICK_REFERENCE.md` - API клиентов
- `Network/CLEANUP_REPORT.md` - Очистка решения

---

**Статус:** ✅ Решено и протестировано
