# Функциональность: Загрузка и кэширование скинов

## Описание
Реализована полная система управления скинами с поддержкой:
- Загрузки скинов с URL и локальных файлов
- Автоматического кэширования скинов
- Запоминания последнего выбранного скина
- Автозагрузки при запуске приложения
- Очистки старого кэша (старше 30 дней)

## Компоненты

### 1. SkinService (`Runia\Services\Common\SkinService.cs`)
Основной сервис для работы со скинами.

**Функции:**
- `GetSkinAsync(username, skinUrl)` - загружает скин с URL и кэширует его
- `GetCachedSkinAsync(username)` - возвращает кэшированный скин если существует
- `SetLastSelectedSkinAsync(username, localPath)` - сохраняет последний выбранный скин
- `GetLastSelectedSkinAsync()` - загружает последний выбранный скин
- `CleanOldCacheAsync()` - очищает кэш старше 30 дней

**Расположение кэша:**
```
%APPDATA%\Runia\Cache\Skins\
```

**Файл последнего выбранного скина:**
```
%APPDATA%\Runia\Cache\last_selected_skin.json
```

### 2. SkinView обновления (`Runia\Ui\Pages\Controls\SkinView.cs`)
- Добавлен DependencyProperty `SkinPath` для загрузки скина из файла
- Добавлен метод `LoadSkinFromPath(string path)`
- **Изменено время автовращения с 4 секунд на 23 секунды**

**Использование в XAML:**
```xaml
<controls:SkinView 
    SkinSource="{Binding SkinSource}" 
    SkinPath="{Binding SkinPath}" 
    Margin="0,80,0,80"/>
```

### 3. UserViewModel обновления (`Runia\ViewModels\Main\UserViewModel.cs`)
- Интеграция с `ISkinService`
- Добавлен `SkinPath` ObservableProperty
- Методы `LoadFromUrlAsyncCommand` и `LoadFromFileAsyncCommand` теперь асинхронные
- Автоматическая загрузка последнего скина при запуске приложения
- Сохранение каждого выбранного скина в `last_selected_skin.json`

### 4. SkinDownloadViewModel (`Runia\ViewModels\Main\SkinDownloadViewModel.cs`)
ViewModel для загрузки скинов по нику игрока с бэка.

**Команды:**
- `DownloadSkinAsyncCommand` - загружает скин по нику
- `CleanCacheAsyncCommand` - очищает старый кэш

**Пример использования:**
```csharp
// Загрузить скин по нику
viewModel.Username = "Steve";
await viewModel.DownloadSkinAsyncCommand.ExecuteAsync(null);
```

### 5. Модели данных (`Runia\Domain\Models\SkinModel.cs`)
- `SkinData` - информация о скине
- `SkinCacheEntry` - запись о кэшированном скине
- `LastSelectedSkin` - данные последнего выбранного скина

## Конфигурация

### Регистрация в DI контейнере
В `App.xaml.cs` уже добавлены регистрации:

```csharp
// Skin service for downloading and caching skins
services.AddSingleton<ISkinService, SkinService>();

// ViewModel
services.AddTransient<Runia.ViewModels.Main.SkinDownloadViewModel>();
```

## Использование

### Из UserViewModel
```csharp
// Загрузить скин из URL
await UserViewModel.LoadFromUrlAsyncCommand.ExecuteAsync(null);

// Загрузить скин из файла
await UserViewModel.LoadFromFileAsyncCommand.ExecuteAsync(null);
```

### Из SkinDownloadViewModel
```csharp
var viewModel = serviceProvider.GetRequiredService<SkinDownloadViewModel>();
viewModel.Username = "SteveUsername";
await viewModel.DownloadSkinAsyncCommand.ExecuteAsync(null);
```

### Прямое использование SkinService
```csharp
var skinService = serviceProvider.GetRequiredService<ISkinService>();

// Загрузить скин с URL
string localPath = await skinService.GetSkinAsync("Steve", "https://example.com/skin.png");

// Запомнить как последний выбранный
await skinService.SetLastSelectedSkinAsync("Steve", localPath);

// Получить последний выбранный
var lastSkin = await skinService.GetLastSelectedSkinAsync();

// Очистить кэш
await skinService.CleanOldCacheAsync();
```

## Интеграция с API бэка

В `SkinDownloadViewModel` используется пример с Minotar API:
```csharp
string skinUrl = $"https://minotar.net/skin/{Username}";
```

### Для интеграции с вашим API:

1. Обновите метод в `SkinDownloadViewModel`:
```csharp
[RelayCommand]
private async Task DownloadSkinAsync()
{
    // Замените на ваш API URL
    string skinUrl = $"https://your-api.com/api/skins/{Username}";

    var localPath = await _skinService.GetSkinAsync(Username, skinUrl);
    // ...
}
```

2. Или создайте метод получения URL скина из API:
```csharp
private async Task<string> GetSkinUrlFromApiAsync(string username)
{
    var client = _httpFactory.CreateClient();
    var response = await client.GetAsync($"https://your-api.com/api/users/{username}");
    var json = await response.Content.ReadAsStringAsync();
    // Парсьте JSON и извлеките skinUrl
    return skinUrl;
}
```

## Примечания

- Кэш автоматически очищается при вызове `CleanOldCacheAsync()`
- Скины кэшируются по имени пользователя в формате `.png`
- Память также кэшируется для быстрого доступа (ConcurrentDictionary)
- Все операции асинхронные и безопасны для потоков
- При загрузке скина из XAML используется `SkinPath` вместо `SkinSource` для автоматической привязки

## Автовращение

- **Время до начала автовращения: 23 секунды** (было 4 секунды)
- Автовращение приостанавливается при захвате мыши
- Скорость вращения: 0.4° за кадр (60 FPS)
