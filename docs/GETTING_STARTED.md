# 🚀 Начало работы с Runia

Этот документ содержит пошаговые инструкции для установки, конфигурации и первого запуска приложения Runia.

## 📋 Требования

### Системные требования
- **ОС**: Windows 7 SP1 или выше (рекомендуется Windows 10/11)
- **RAM**: 2 GB минимум, 4 GB рекомендуется
- **Диск**: 500 MB свободного места

### Требования к разработке
- **.NET SDK**: .NET 9 или выше
- **Visual Studio**: 2022 или выше (Community Edition достаточна)
- **Git**: Для клонирования репозитория

## ⬇️ Установка

### 1. Клонирование репозитория

```powershell
git clone https://github.com/yuranaumov2007-boop/GoGoGaGA.git
cd Runia
```

### 2. Открытие в Visual Studio

```powershell
# Откройте файл решения
.\Runia.sln
```

Или откройте Visual Studio и выберите `File → Open Project/Solution → Runia.sln`

### 3. Восстановление зависимостей

```powershell
# Visual Studio автоматически восстановит NuGet пакеты при открытии
# Или вручную через Package Manager Console:
# PM> Update-Package
```

### 4. Первый запуск

1. В Visual Studio выберите конфигурацию: **Debug** или **Release**
2. Выберите целевую платформу: **x64** или **x86**
3. Нажмите **F5** или кнопку **Start** для запуска

## ⚙️ Конфигурация

### API URL

По умолчанию приложение подключается к API, установленному в `DefaultAppConfig`:

**Файл**: `Services/Core/Infrastructure/Configuration/DefaultAppConfig.cs`

```csharp
public class DefaultAppConfig : IAppConfig
{
    public string ApiBaseUrl => "https://your-api-url.com"; // Измените это значение
}
```

### Изменение API URL

```csharp
// Для локальной разработки:
public string ApiBaseUrl => "http://localhost:5000/api";

// Для продакшена:
public string ApiBaseUrl => "https://api.yourdomain.com";
```

### Локальное хранилище данных

Приложение использует локальную папку для хранения данных:

```
%APPDATA%\Runia\
├── config.json
├── cache/
└── logs/
```

## 🔑 Переменные окружения

Вы можете использовать переменные окружения для установки параметров:

```powershell
# Установите API URL через переменную окружения
$env:RUNIA_API_URL = "http://localhost:5000/api"

# Запустите приложение
dotnet run
```

## 🧪 Первый запуск - Пошаговая инструкция

1. **Запустите приложение** (F5)
2. **Окно входа** появится на экране
   - Если это первый запуск, нажмите "Зарегистрироваться"
3. **Страница регистрации**
   - Введите email, пароль (минимум 8 символов)
   - Нажмите "Создать аккаунт"
4. **Подтверждение email**
   - Введите код подтверждения из письма
   - Нажмите "Подтвердить"
5. **Окно входа**
   - Введите ваш email и пароль
   - Нажмите "Вход"
6. **Главное окно**
   - Откроется главное окно приложения

## 🐛 Решение проблем при первом запуске

### Проблема: "Unable to connect to API"

**Решение**:
1. Проверьте API URL в `DefaultAppConfig`
2. Убедитесь, что API сервер запущен
3. Проверьте подключение к интернету
4. Смотрите логи в консоли Visual Studio

### Проблема: "Certificate validation failed"

**Решение** (для локальной разработки):
```csharp
// Временно отключите проверку сертификата (НЕ ДЛЯ ПРОДАКШЕНА!)
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
```

### Проблема: "Failed to restore NuGet packages"

**Решение**:
```powershell
# Очистите кэш NuGet
nuget locals all -clear

# Восстановите пакеты
dotnet restore
```

### Проблема: "Permission denied" при запуске

**Решение**:
```powershell
# Запустите Visual Studio от имени администратора
# Или в PowerShell:
Start-Process powershell -Verb RunAs
```

## 🔨 Сборка проекта

### Сборка в режиме Debug

```powershell
dotnet build --configuration Debug
```

### Сборка в режиме Release

```powershell
dotnet build --configuration Release
```

### Публикация приложения

```powershell
dotnet publish --configuration Release --runtime win-x64 --self-contained
```

## 📋 Структура папок для запуска

После первого запуска будут созданы следующие папки:

```
C:\Users\YourUsername\AppData\Local\Runia\
├── SecureTokens/          # Защищённые токены
├── Config/                # Конфигурационные файлы
└── Cache/                 # Кэш данных
```

## ✅ Проверка установки

Выполните эти проверки, чтобы убедиться, что всё работает:

- [ ] Приложение запускается без ошибок
- [ ] Окно входа отображается корректно
- [ ] Кнопка "Зарегистрироваться" работает
- [ ] Форма регистрации валидирует input
- [ ] После регистрации запрашивается подтверждение email
- [ ] Кнопка "Вход" работает после подтверждения
- [ ] Главное окно загружается после входа

## 🎯 Следующие шаги

После успешной установки и первого запуска:

1. **Изучите архитектуру**: [Архитектура](./ARCHITECTURE.md)
2. **Разберитесь с аутентификацией**: [Система аутентификации](./AUTHENTICATION.md)
3. **Начните разработку**: [UI/UX](./UI_UX.md)

## 💡 Советы для разработчиков

- Используйте **Debug** конфигурацию при разработке
- Установите точки останова (breakpoints) для отладки
- Используйте **Output** окно для просмотра логов
- Регулярно сохраняйте изменения с помощью Git

## 📚 Дополнительные ресурсы

- [Документация WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Документация .NET 9](https://learn.microsoft.com/en-us/dotnet/core/release-notes/preview/preview9)
- [MVVM Community Toolkit](https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/introduction)

---

**Готовы начать?** Перейдите к [Архитектуре](./ARCHITECTURE.md) для понимания структуры проекта.
