# 👨‍💻 Руководство для разработчиков (Contributing Guide)

Спасибо за интерес к Runia Launcher! 🎮 Этот документ поможет вам начать внесение вклада в проект.

---

## 📋 Оглавление

- [Кодекс поведения](#-кодекс-поведения)
- [Как начать](#-как-начать)
- [Процесс разработки](#-процесс-разработки)
- [Стандарты кода](#-стандарты-кода)
- [Git Workflow](#-git-workflow)
- [Pull Requests](#-pull-requests)
- [Тестирование](#-тестирование)
- [Документирование](#-документирование)

---

## 🤝 Кодекс поведения

Этот проект придерживается **Contributor Covenant Code of Conduct**.

### Ожидаемое поведение
✅ Будьте вежливы и уважительны  
✅ Слушайте отзывы конструктивно  
✅ Сосредоточьтесь на том, что лучше для сообщества  
✅ Помогайте другим контрибьютерам  

### Недопустимое поведение
❌ Оскорбления и дразнение  
❌ Дискриминация  
❌ Публичные или частные преследования  
❌ Опубликование личной информации других  

---

## 🚀 Как начать

### 1. Форк репозитория

```bash
# На GitHub нажмите кнопку "Fork"
# Или через CLI
gh repo fork mokkignomov-design/Launcher --clone
```

### 2. Клонирование вашего форка

```bash
git clone https://github.com/ВАШ_ЮЗЕРНЕЙМ/Launcher.git
cd Launcher
```

### 3. Добавление upstream

```bash
git remote add upstream https://github.com/mokkignomov-design/Launcher.git
git fetch upstream
```

### 4. Синхронизация с основной веткой

```bash
git checkout main
git rebase upstream/main
```

### 5. Установка зависимостей

```bash
dotnet restore
```

### 6. Сборка проекта

```bash
dotnet build
```

### 7. Запуск приложения

```bash
dotnet run --project Runia/Runia.csproj
```

---

## 💡 Процесс разработки

### Выбор задачи

**Найти задачу для себя:**

1. Посмотрите [Issues на GitHub](https://github.com/mokkignomov-design/Launcher/issues)
2. Фильтруйте по меткам:
   - 🆕 `good first issue` - для новичков
   - 🐛 `bug` - исправления ошибок
   - ✨ `enhancement` - новые возможности
   - 📚 `documentation` - документация

3. Оставьте комментарий под issue: "Я беру эту задачу"

### Создание branch для работы

```bash
# Синхронизируемся с основной веткой
git fetch upstream
git checkout main
git rebase upstream/main

# Создаём новую ветку
# Конвенция: type/short-description
git checkout -b feature/add-multiplayer-support
# или
git checkout -b bugfix/fix-login-crash
# или
git checkout -b docs/update-readme
```

**Типы веток:**
- `feature/` - новая возможность
- `bugfix/` - исправление ошибки
- `docs/` - документация
- `refactor/` - рефакторинг
- `chore/` - техническое обслуживание
- `test/` - добавление тестов

---

## 📝 Стандарты кода

### Именование

```csharp
// ✅ Правильно
public class AuthenticationService { }
private readonly IUserRepository _userRepository;
public string UserName { get; set; }
private int _maxRetryAttempts = 3;

// ❌ Неправильно
public class authService { }
private readonly IUserRepository userRepository;
public string username { get; set; }
private int MaxRetryAttempts = 3;
```

**Конвенции:**
- `PascalCase` - классы, методы, свойства, namespaces
- `camelCase` - локальные переменные, параметры
- `_camelCase` - приватные поля
- `UPPER_CASE` - константы

### C# 12 и .NET 9 особенности

Используйте современные возможности:

```csharp
// ✅ Collection expressions
var items = new List<string> { "a", "b", "c" };
// vs
var items = ["a", "b", "c"];  // ✅ Предпочтительно

// ✅ Primary constructors
public class Service(ILogger logger)
{
	private readonly ILogger _logger = logger;
}

// ✅ Required properties
public class User
{
	public required string Email { get; set; }
}

// ✅ File-scoped namespaces
namespace Runia.Services;  // Вместо namespace Runia.Services { }

// ✅ Record types для DTOs
public record UserDto(string Email, string Username);

// ✅ Null-coalescing operator
string name = user?.Name ?? "Unknown";

// ✅ Switch expressions
var message = status switch
{
	UserStatus.Active => "Активен",
	UserStatus.Banned => "Заблокирован",
	_ => "Неизвестно"
};
```

### Форматирование кода

```csharp
// ✅ Правильное форматирование
public class UserService(IUserRepository repository, ILogger logger)
{
	private readonly IUserRepository _repository = repository;
	private readonly ILogger _logger = logger;

	public async Task<User?> GetUserAsync(int id)
	{
		try
		{
			_logger.LogInformation("Getting user with id {Id}", id);

			var user = await _repository.GetByIdAsync(id);

			return user;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting user");
			throw;
		}
	}
}

// ✅ Обработка исключений
public async Task<ApiResult<T>> GetAsync<T>(string endpoint)
{
	try
	{
		var response = await _httpClient.GetAsync(endpoint);

		if (!response.IsSuccessStatusCode)
		{
			return new ApiResult<T>(
				false,
				default,
				$"HTTP {response.StatusCode}"
			);
		}

		var content = await response.Content.ReadAsStringAsync();
		var data = JsonSerializer.Deserialize<T>(content);

		return new ApiResult<T>(true, data);
	}
	catch (HttpRequestException ex)
	{
		_logger.LogError(ex, "Network error");
		return new ApiResult<T>(false, default, "Network error");
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Unexpected error");
		return new ApiResult<T>(false, default, "Internal error");
	}
}
```

### XML Документация

```csharp
/// <summary>
/// Аутентифицирует пользователя с помощью email и пароля.
/// </summary>
/// <param name="email">Email адрес пользователя</param>
/// <param name="password">Пароль пользователя</param>
/// <returns>Результат попытки входа с токеном доступа при успехе</returns>
/// <exception cref="ArgumentNullException">Если email или password равны null</exception>
/// <remarks>
/// Этот метод делает HTTP запрос к API сервера.
/// Успешная аутентификация сохранит токен в SecureTokenStore.
/// </remarks>
public async Task<ApiResult<LoginResponse>> LoginAsync(
	string email, 
	string password)
{
	// реализация...
}
```

### Стили кода

Используйте `.editorconfig` для консистентности:

```ini
# .editorconfig
root = true

[*.cs]
# Indent style
indent_style = space
indent_size = 4

# New line preferences
end_of_line = crlf
insert_final_newline = true

# this preferences
dotnet_style_qualification_for_field = false
dotnet_style_qualification_for_property = false

# var preferences
csharp_style_var_for_built_in_types = false
csharp_style_var_when_type_is_apparent = true
csharp_style_var_elsewhere = false
```

---

## 🌳 Git Workflow

### Commit сообщения

Используйте Conventional Commits:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Примеры:**

```
feat(auth): add email verification
↑─────────────────────────────────
│ Тип feat + область auth + описание

fix(launcher): resolve game crash on startup
↑─────────────────────────────────────────
│ Исправление баги +描述

docs(readme): update installation instructions

refactor(services): improve dependency injection

test(homepage): add unit tests for view model

chore(deps): update nuget packages
```

**Типы commits:**
- `feat:` - новая возможность
- `fix:` - исправление баги
- `docs:` - документация
- `style:` - форматирование (не меняет логику)
- `refactor:` - переработка кода
- `perf:` - оптимизация производительности
- `test:` - добавление тестов
- `chore:` - техническое обслуживание (зависимости и т.д.)

### Commits должны быть атомарными

```bash
# ✅ Хорошо
git commit -m "feat(auth): add password validation"
git commit -m "feat(auth): add email verification"
git commit -m "test(auth): add auth service tests"

# ❌ Плохо
git commit -m "Add auth, update UI, fix bugs, update docs"
```

### Перед push: убедитесь что код собирается

```bash
dotnet clean
dotnet build -c Release
```

### Push ветки

```bash
git push origin feature/add-multiplayer-support
```

---

## 🔄 Pull Requests

### Шаблон PR

```markdown
## Описание
Краткое описание того, что делает этот PR.

## Связанная Issue
Fixes #123

## Тип изменения
- [ ] 🐛 Исправление баги
- [ ] ✨ Новая возможность  
- [ ] 📚 Документация
- [ ] 🔄 Рефакторинг
- [ ] ⚡ Оптимизация

## Как я это тестировал?
Описание шагов для тестирования изменений.

## Чек-лист
- [ ] Код следует стандартам стиля проекта
- [ ] Я выполнил самообзор кода
- [ ] Я оставил комментарии для сложных частей
- [ ] Я обновил документацию
- [ ] Я добавил тесты для новой логики
- [ ] Тесты проходят локально с моими изменениями
- [ ] Не добавлены новые warnings
```

### Требования к PR

✅ **Должно быть:**
- Чёткое описание изменений
- Ссылка на связанную issue (`Fixes #123`)
- Бранч синхронизирован с main (`git rebase upstream/main`)
- Код собирается без ошибок
- Все тесты проходят

❌ **Не должно быть:**
- Множество нереляционных изменений в одном PR
- Конфликты с main веткой
- Debug кода или закомментированных участков
- Больших console.log или Debug.WriteLine

### Процесс review

1. Автор создаёт PR
2. CI проверяет сборку и тесты
3. Мейнтейнеры делают code review
4. Автор адресует комментарии и pushs изменений
5. Одобрение и merge в main

---

## 🧪 Тестирование

### Писать тесты для новой логики

```csharp
using Xunit;
using Moq;

namespace Runia.Tests.Services.Auth;

public class AuthServiceTests
{
	private readonly MockAuthClient _mockAuthClient;
	private readonly AuthService _authService;

	public AuthServiceTests()
	{
		_mockAuthClient = new Mock<IAuthClient>();
		_authService = new AuthService(_mockAuthClient.Object);
	}

	[Fact]
	public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
	{
		// Arrange
		var request = new LoginRequest 
		{ 
			Email = "test@example.com", 
			Password = "password" 
		};
		var response = new LoginResponse { Token = "token123" };

		_mockAuthClient
			.Setup(x => x.LoginAsync(request))
			.ReturnsAsync(new ApiResult<LoginResponse>(true, response));

		// Act
		var result = await _authService.LoginAsync(request);

		// Assert
		Assert.True(result);
		_mockAuthClient.Verify(x => x.LoginAsync(request), Times.Once);
	}

	[Fact]
	public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFailure()
	{
		// Arrange
		var request = new LoginRequest 
		{ 
			Email = "test@example.com", 
			Password = "wrong" 
		};

		_mockAuthClient
			.Setup(x => x.LoginAsync(request))
			.ReturnsAsync(new ApiResult<LoginResponse>(false, null, "Invalid credentials"));

		// Act
		var result = await _authService.LoginAsync(request);

		// Assert
		Assert.False(result);
	}
}
```

### Запуск тестов локально

```bash
# Все тесты
dotnet test

# Конкретный проект
dotnet test ./Runia.Tests/

# С деталізацією
dotnet test /p:CollectCoverage=true

# Как в CI
dotnet test --configuration Release --no-build
```

### Code Coverage

```bash
# Генерировать отчёт coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Минимальный порог coverage
dotnet test /p:CollectCoverage=true /p:Threshold=70
```

---

## 📖 Документирование

### Добавьте документацию для новых возможностей

```markdown
## Новая возможность: Мультиплеер

### Описание
Добавлена поддержка серверов с несколькими игроками.

### Использование

```csharp
var multiplayer = new MultiplayerManager();
await multiplayer.JoinServerAsync("server-ip", 25565);
```

### Конфигурация
Смотрите [appsettings.json](../README.md#-конфигурация)
```

### Комментарии в коде

```csharp
// ✅ Хороший комментарий
// Используем exponential backoff для retry механизма при сетевых ошибках
for (int i = 0; i < maxRetries; i++)
{
	var delay = TimeSpan.FromSeconds(Math.Pow(2, i));
	await Task.Delay(delay);
}

// ❌ Плохой комментарий
// i = 0
for (int i = 0; i < maxRetries; i++)

// ✅ Объяснение "почему", а не "что"
// Пропускаем первый элемент потому что это заголовок CSV файла
var dataRows = lines.Skip(1);
```

---

## 📋 Чек-лист перед Pull Request

```bash
# 1. Убедитесь что ветка актуальна
git fetch upstream
git rebase upstream/main

# 2. Протестируйте локально
dotnet clean
dotnet build -c Release
dotnet test

# 3. Проверьте стиль кода
# (используйте Ctrl+K, Ctrl+D в Visual Studio для auto-format)

# 4. Пересмотрите свои изменения
git diff upstream/main

# 5. Убедитесь в атомарности commits
git log upstream/main..HEAD --oneline

# 6. Push и создайте PR
git push origin feature/your-feature
```

---

## 🎯 Цели проекта

Runia Launcher стремится быть:
- **Функциональным** - полноценный лаунчер для Minecraft
- **Безопасным** - защита данных пользователя
- **Производительным** - быстрая загрузка и запуск
- **Удобным** - инттуитивный интерфейс
- **Поддерживаемым** - чистый и разумный код

Ваши PR должны соответствовать этим целям! 🎮

---

## ❓ Вопросы?

- 🐛 Баги: [Issues на GitHub](https://github.com/mokkignomov-design/Launcher/issues)
- 💬 Обсуждения: [GitHub Discussions](https://github.com/mokkignomov-design/Launcher/discussions)
- 📧 Email: contact@example.com

---

## 📜 Лицензия

Внося код в этот проект, вы соглашаетесь с тем, что ваш код будет лицензирован под той же лицензией, что и проект.

---

**Спасибо за вклад в Runia Launcher! 🙏**

Мы ценим каждое улучшение, исправление и новую идею! 🚀
