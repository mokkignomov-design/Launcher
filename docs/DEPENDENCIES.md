# 🛠️ Зависимости и библиотеки Runia

Документация по используемым NuGet пакетам и фреймворкам.

## 📋 Обзор

Runia использует минимальный набор зависимостей для максимальной производительности и простоты.

## 📦 Список зависимостей

### 1. MVVM Community Toolkit

**NuGet**: `Microsoft.Xaml.Behaviors.Wpf`  
**Версия**: 1.1.39  
**Лицензия**: MIT

```xml
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.39" />
```

**Назначение**: Поддержка MVVM паттерна и behaviors для WPF

**Использование**:
- Behaviors для элементов управления
- Data binding behaviors
- Event-to-Command behaviors

```xaml
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}">
    <b:Interaction.Behaviors>
        <b:EventTriggerBehavior EventName="TextChanged">
            <b:InvokeCommandAction Command="{Binding SearchCommand}" />
        </b:EventTriggerBehavior>
    </b:Interaction.Behaviors>
</TextBox>
```

## 🖼️ Встроенные компоненты .NET

### System.Net.Http

**Встроен в**: .NET 9  
**Назначение**: HTTP запросы и работа с веб-сервисами

```csharp
var client = new HttpClient();
var response = await client.GetAsync("https://api.example.com/data");
```

### System.Text.Json

**Встроен в**: .NET 9  
**Назначение**: Десериализация/сериализация JSON

```csharp
var json = JsonSerializer.Serialize(data);
var obj = JsonSerializer.Deserialize<MyClass>(json);
```

### System.Net.Security

**Встроен в**: .NET 9  
**Назначение**: Обработка HTTPS и SSL/TLS

### System.Security.Cryptography

**Встроен в**: .NET 9  
**Назначение**: Криптография и DPAPI

```csharp
byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
```

## 🎨 WPF Framework

**Встроен в**: .NET 9

WPF (Windows Presentation Foundation) - основной UI фреймворк приложения.

**Используемые пространства имён**:
- `System.Windows` - Основные классы WPF
- `System.Windows.Controls` - Элементы управления (Button, TextBox, etc.)
- `System.Windows.Data` - Data binding
- `System.Windows.Markup` - XAML обработка

## 📊 Архитектура зависимостей

```
Runia Application
    │
    ├─→ WPF Framework (.NET 9)
    │   ├─ System.Windows
    │   ├─ System.Windows.Controls
    │   └─ System.Windows.Data
    │
    ├─→ Network Layer
    │   ├─ System.Net.Http
    │   ├─ System.Net.HttpClient
    │   └─ System.Net.Security (HTTPS)
    │
    ├─→ Data Serialization
    │   ├─ System.Text.Json
    │   └─ System.Text.Encoding
    │
    ├─→ Security
    │   ├─ System.Security.Cryptography
    │   └─ System.Security.Cryptography.ProtectedData
    │
    └─→ MVVM & Behaviors
        └─ Microsoft.Xaml.Behaviors.Wpf
```

## 🔧 Требования к проекту

### Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>assets\Icons\Icon_v2.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.39" />
  </ItemGroup>

</Project>
```

**Объяснение параметров**:
- `OutputType=WinExe` - Исполняемое приложение (не консоль)
- `TargetFramework=net9.0-windows` - .NET 9 для Windows
- `Nullable=enable` - Проверка null-значений
- `ImplicitUsings=enable` - Автоматические using директивы
- `UseWPF=true` - Включить WPF поддержку
- `ApplicationIcon` - Иконка приложения в трее

## 📦 Как добавить новый пакет

### Через NuGet Package Manager

```powershell
# Package Manager Console
Install-Package PackageName -Version X.Y.Z
```

### Через .csproj редактирование

```xml
<ItemGroup>
  <PackageReference Include="PackageName" Version="X.Y.Z" />
</ItemGroup>
```

### Через dotnet CLI

```powershell
dotnet add package PackageName --version X.Y.Z
```

## ⚠️ Рекомендации по зависимостям

### ✅ Хорошо добавить:

- **Newtonsoft.Json** - Альтернатива System.Text.Json (если нужна большая совместимость)
- **Serilog** - Логирование (рекомендуется для production)
- **AutoMapper** - Маппинг объектов
- **FluentValidation** - Валидация данных
- **RestSharp** - HTTP клиент более высокого уровня

### ❌ Избегайте:

- **Entity Framework** - Слишком тяжело для desktop приложения
- **SignalR** - Для web приложений
- **ASP.NET Core** - Для серверных приложений
- **Dependency Injection контейнеры** - Используйте встроенный Microsoft.Extensions.DependencyInjection

## 🔍 Проверка версий пакетов

### Проверить текущую версию

```powershell
# В Package Manager Console
Get-Package
```

### Проверить доступные версии

```powershell
Find-Package PackageName -AllVersions
```

### Обновить пакет

```powershell
Update-Package PackageName -Version X.Y.Z
```

## 📋 Версионирование

### Семантическое версионирование (SemVer)

Все пакеты следуют формату: **MAJOR.MINOR.PATCH**

- **MAJOR** - Несовместимые изменения
- **MINOR** - Новые функции (обратно совместимые)
- **PATCH** - Исправления ошибок

Пример: `1.1.39`
- Мажорная версия: 1
- Минорная версия: 1
- Патч версия: 39

## 🔐 Безопасность пакетов

### Проверка уязвимостей

```powershell
dotnet list package --vulnerable
```

### Обновление до последней безопасной версии

```powershell
dotnet package update --update-all --version-type minor
```

## 📊 Текущие версии в проекте

| Пакет | Версия | Лицензия | Статус |
|-------|--------|---------|--------|
| Microsoft.Xaml.Behaviors.Wpf | 1.1.39 | MIT | ✅ Актуальна |

## 🚀 Производительность

### Размер приложения

```
Текущий размер (Release):
- Без зависимостей: ~2 MB
- С .NET Runtime: ~150 MB (self-contained)
```

### Оптимизация

```xml
<!-- Для уменьшения размера выпуска -->
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <PublishReadyToRun>true</PublishReadyToRun>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

## 📚 Документация по зависимостям

### Microsoft.Xaml.Behaviors.Wpf
- GitHub: https://github.com/Microsoft/XamlBehaviorsWpf
- Документация: https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern

### WPF Framework
- Официальная документация: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- XAML документация: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/xaml/

### System.Text.Json
- Документация: https://learn.microsoft.com/en-us/dotnet/api/system.text.json

### System.Net.Http
- Документация: https://learn.microsoft.com/en-us/dotnet/api/system.net.http

## 🔗 Связанные документы

- [Архитектура](./ARCHITECTURE.md)
- [Начало работы](./GETTING_STARTED.md)

---

**Примечание**: Проект спроектирован с минимальным количеством внешних зависимостей для максимальной стабильности и производительности.
