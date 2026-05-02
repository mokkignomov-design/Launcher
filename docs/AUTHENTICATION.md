# 🔐 Система аутентификации Runia

Документация по системе аутентификации, регистрации, сброса пароля и управлению сеансами.

## 📋 Обзор

Система аутентификации включает следующие функции:
- ✅ **Регистрация** - Создание нового аккаунта
- ✅ **Вход** - Аутентификация существующего пользователя
- ✅ **Подтверждение email** - Верификация адреса электронной почты
- ✅ **Забыли пароль** - Инициирование процесса восстановления
- ✅ **Сброс пароля** - Установка нового пароля
- ✅ **JWT токены** - Безопасная аутентификация между запросами

## 🏗️ Архитектура аутентификации

```
┌──────────────────────────────────────────┐
│         UI Layer (XAML Pages)            │
│  ├─ LoginPage.xaml                       │
│  ├─ RegisterPage.xaml                    │
│  ├─ ForgotPasswordPage.xaml             │
│  ├─ ResetPasswordPage.xaml              │
│  └─ ConfirmEmailPage.xaml               │
└─────────────────┬──────────────────────────┘
                  │ DataBinding
┌─────────────────▼──────────────────────────┐
│      ViewModel Layer                       │
│  ├─ LoginViewModel                         │
│  ├─ RegisterViewModel                      │
│  ├─ ForgotPasswordViewModel               │
│  ├─ ResetPasswordViewModel                │
│  └─ ConfirmEmailViewModel                 │
└─────────────────┬──────────────────────────┘
                  │ Dependency Injection
┌─────────────────▼──────────────────────────┐
│      Services Layer                        │
│  ├─ AuthService                            │
│  └─ AuthManager                            │
└─────────────────┬──────────────────────────┘
                  │
┌─────────────────▼──────────────────────────┐
│   Infrastructure Layer                     │
│  ├─ BaseApiService (HTTP)                  │
│  ├─ JwtHandler (Token Management)          │
│  ├─ SecureTokenStore (Secure Storage)      │
│  └─ LocalDataService (Local Data)          │
└──────────────────────────────────────────┘
```

## 📝 Основные компоненты

### AuthService

Основной сервис для всех операций аутентификации.

**Файл**: `Services/Auth/AuthService.cs`

```csharp
public class AuthService
{
    // Регистрация нового пользователя
    public async Task<Result<AuthResponse>> RegisterAsync(
        string email, 
        string password, 
        string confirmPassword)

    // Вход в аккаунт
    public async Task<Result<AuthResponse>> LoginAsync(
        string email, 
        string password)

    // Подтверждение email
    public async Task<Result<bool>> ConfirmEmailAsync(
        string email, 
        string confirmationCode)

    // Забыли пароль
    public async Task<Result<bool>> ForgotPasswordAsync(
        string email)

    // Сброс пароля
    public async Task<Result<bool>> ResetPasswordAsync(
        string email, 
        string resetCode, 
        string newPassword)

    // Выход
    public async Task LogoutAsync()

    // Получение текущего пользователя
    public User? GetCurrentUser()

    // Проверка, авторизован ли пользователь
    public bool IsAuthenticated
}
```

### AuthManager

Менеджер для управления состоянием аутентификации.

**Файл**: `Services/Auth/AuthManager.cs`

```csharp
public class AuthManager
{
    // Управление состоянием аутентификации
    public event EventHandler? AuthStatusChanged;

    public void SetAuthenticated(User user, string token);
    public void ClearAuthentication();
}
```

### SecureTokenStore

Безопасное хранилище для JWT токенов.

**Файл**: `Services/Core/Infrastructure/Network/SecureTokenStore.cs`

```csharp
public interface ITokenStore
{
    void SaveToken(string token);
    string? GetToken();
    void RemoveToken();
    bool HasToken { get; }
}

public class SecureTokenStore : ITokenStore
{
    // Используется DPAPI для шифрования токенов в DPAPI Data Store
    // Расположение: %APPDATA%\Runia\SecureTokens\
}
```

### JwtHandler

Обработчик для автоматического добавления токена в HTTP заголовки.

**Файл**: `Services/Core/Infrastructure/Network/JwtHandler.cs`

```csharp
public class JwtHandler : DelegatingHandler
{
    // Перехватывает все HTTP запросы
    // Добавляет Authorization: Bearer <token> заголовок
    // Обновляет токен при необходимости
}
```

## 🔄 Процессы аутентификации

### 1. Регистрация

**Процесс**:
```
1. Пользователь заполняет форму регистрации
   ├─ Email
   ├─ Пароль (минимум 8 символов)
   └─ Подтверждение пароля

2. Валидация на клиенте
   ├─ Email валидация (format@example.com)
   ├─ Пароль должен совпадать с подтверждением
   └─ Минимальная длина пароля

3. Отправка POST запроса на API
   └─→ POST /api/auth/register
       └─ Body: { email, password, confirmPassword }

4. API обрабатывает запрос
   ├─ Валидирует данные
   ├─ Проверяет, не существует ли email
   ├─ Хэширует пароль
   └─ Создаёт новый аккаунт

5. Отправка кода подтверждения на email

6. Переход на страницу ConfirmEmailPage
   └─ Пользователь вводит код из письма
```

**ViewModel**: `RegisterViewModel.cs`

```csharp
[RelayCommand]
private async Task Register()
{
    if (!ValidateInput()) return;

    var result = await _auth.RegisterAsync(Email, Password, ConfirmPassword);

    if (result.IsSuccess)
    {
        // Переход на страницу подтверждения email
        _nav.Navigate<ConfirmEmailPage>();
    }
    else
    {
        // Показать ошибку
        await _dialog.ShowErrorAsync(result.ErrorMessage);
    }
}
```

### 2. Подтверждение Email

**Процесс**:
```
1. Пользователь получает письмо с кодом подтверждения

2. Открывает ConfirmEmailPage
   └─ Вводит код подтверждения (обычно 6 цифр)

3. Отправка запроса на подтверждение
   └─→ POST /api/auth/confirm-email
       └─ Body: { email, confirmationCode }

4. API валидирует код

5. Аккаунт активирован

6. Переход на LoginPage
```

**ViewModel**: `ConfirmEmailViewModel.cs`

```csharp
[RelayCommand]
private async Task ConfirmEmail()
{
    var result = await _auth.ConfirmEmailAsync(Email, ConfirmationCode);

    if (result.IsSuccess)
    {
        await _dialog.ShowInfoAsync("Email подтверждён!");
        _nav.Navigate<LoginPage>();
    }
}
```

### 3. Вход

**Процесс**:
```
1. Пользователь на странице LoginPage вводит:
   ├─ Email
   └─ Пароль

2. Отправка запроса входа
   └─→ POST /api/auth/login
       └─ Body: { email, password }

3. API проверяет учётные данные

4. API возвращает JWT токен

5. Токен сохраняется в SecureTokenStore

6. Переход на MainWindow

7. Все последующие запросы включают токен
```

**ViewModel**: `LoginViewModel.cs`

```csharp
[RelayCommand]
private async Task Login()
{
    var result = await _auth.LoginAsync(Username, Password);

    if (result.IsSuccess)
    {
        // Токен автоматически сохраняется
        _nav.Navigate<MainWindow>();
    }
    else
    {
        ErrorMessage = result.ErrorMessage ?? "Ошибка входа";
    }
}
```

### 4. Сброс пароля

**Процесс**:
```
1. На странице входа: "Забыли пароль?"

2. Пользователь переходит на ForgotPasswordPage
   └─ Вводит email

3. Отправка запроса
   └─→ POST /api/auth/forgot-password
       └─ Body: { email }

4. API отправляет код сброса на email

5. Переход на ResetPasswordPage
   ├─ Вводит код из письма
   ├─ Вводит новый пароль
   └─ Подтверждает пароль

6. Отправка запроса сброса
   └─→ POST /api/auth/reset-password
       └─ Body: { email, resetCode, newPassword }

7. Пароль успешно изменён

8. Переход обратно на LoginPage
```

**Процесс в коде**:

```csharp
// Забыли пароль (ForgotPasswordViewModel)
[RelayCommand]
private async Task ForgotPassword()
{
    var result = await _auth.ForgotPasswordAsync(Email);
    if (result.IsSuccess)
    {
        _nav.Navigate<ResetPasswordPage>();
    }
}

// Сброс пароля (ResetPasswordViewModel)
[RelayCommand]
private async Task ResetPassword()
{
    var result = await _auth.ResetPasswordAsync(
        Email, 
        ResetCode, 
        NewPassword);

    if (result.IsSuccess)
    {
        _nav.Navigate<LoginPage>();
    }
}
```

### 5. Выход

**Процесс**:
```
1. Пользователь нажимает кнопку "Выход"

2. Очищается токен из SecureTokenStore

3. Очищаются данные пользователя из памяти

4. Переход обратно на LoginWindow
```

## 🔑 Работа с JWT токенами

### Структура JWT токена

```
JWT Token структура: Header.Payload.Signature

Header (Заголовок):
{
    "alg": "HS256",
    "typ": "JWT"
}

Payload (Данные):
{
    "sub": "user123",
    "email": "user@example.com",
    "iat": 1234567890,
    "exp": 1234571490,
    "roles": ["user"]
}

Signature (Подпись):
HMACSHA256(base64(header) + "." + base64(payload), secret)
```

### Жизненный цикл токена

```
1. Регистрация/Вход
   └─→ Получение токена от API

2. Сохранение
   └─→ SecureTokenStore.SaveToken(token)
       └─ DPAPI шифрование
       └─ Сохранение в %APPDATA%\Runia\

3. Использование
   └─→ JwtHandler автоматически добавляет в заголовки
       └─ Authorization: Bearer <token>

4. Проверка срока действия
   └─→ Если токен истёк, попытка обновления

5. Удаление
   └─→ При выходе: SecureTokenStore.RemoveToken()
```

### Автоматическое добавление токена

```csharp
// JwtHandler.cs - перехватывает все запросы

public class JwtHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...)
    {
        var token = _tokenStore.GetToken();
        if (token != null)
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
```

## 📊 Диаграмма состояний аутентификации

```
┌──────────────────┐
│   Not Logged In  │
└────────┬─────────┘
         │
    ┌────┴─────┬──────────────────┐
    │ Register  │    Login          │ Reset Password
    │           │                  │
    ▼           ▼                  ▼
┌─────────┐ ┌─────────┐      ┌──────────┐
│  Verify │ │ Verify  │      │ Set New  │
│  Email  │ │ Email & │──────│ Password │
│  (Reg)  │ │Password │      └──────────┘
└────┬────┘ └────┬────┘             │
     │           │                  │
     │      ┌────▼─────────────────┘
     │      │
     └──────┼───────────────────┐
            ▼                   │
      ┌──────────────┐          │
      │ Authenticated│          │
      │   (JWT)      │          │
      └──┬───────────┘          │
         │ Logout                │
         │                       │
         └───────────────────────┘
         ▼
      ┌──────────────┐
      │ Not Logged In│
      └──────────────┘
```

## ⚠️ Обработка ошибок

### Типичные ошибки аутентификации

```csharp
public enum AuthError
{
    None,
    InvalidEmail,
    InvalidPassword,
    EmailAlreadyExists,
    UserNotFound,
    IncorrectPassword,
    EmailNotConfirmed,
    InvalidConfirmationCode,
    TokenExpired,
    NetworkError,
    UnknownError
}
```

### Обработка ошибок в ViewModel

```csharp
private async Task HandleLoginError(Result<AuthResponse> result)
{
    var message = result.Error switch
    {
        AuthError.UserNotFound => "Пользователь не найден",
        AuthError.IncorrectPassword => "Неправильный пароль",
        AuthError.EmailNotConfirmed => "Email не подтверждён",
        AuthError.NetworkError => "Проблемы с подключением",
        _ => "Неизвестная ошибка"
    };

    await _dialog.ShowErrorAsync(message);
}
```

## 🔒 Безопасность

### Best Practices (Лучшие практики)

1. **Пароли**: Никогда не хранятся в открытом виде
   - На клиенте: Никогда не логируются
   - На сервере: Хэшируются (bcrypt/PBKDF2)

2. **Токены**: Хранятся в защищённом хранилище
   - Используется DPAPI (Data Protection API)
   - Шифруются перед сохранением

3. **HTTPS**: Все запросы должны быть по HTTPS в продакшене

4. **Validation**: Валидация на обеих сторонах
   - Клиент: Пользовательский опыт
   - Сервер: Безопасность

### Защита от атак

| Атака | Защита |
|-------|--------|
| Man-in-the-Middle | HTTPS + SSL Pinning |
| Token Theft | DPAPI Encryption + Secure Storage |
| Brute Force | Rate Limiting на сервере |
| CSRF | CSRF Token в заголовках |
| XSS | Input Validation + Output Encoding |

## 📋 Требования к паролю

- Минимальная длина: **8 символов**
- Рекомендуется:
  - Хотя бы одна прописная буква (A-Z)
  - Хотя бы одна строчная буква (a-z)
  - Хотя бы одна цифра (0-9)
  - Хотя бы один специальный символ (!@#$%^&*)

## 📧 Email требования

- Валидный формат: `name@example.com`
- Уникален в системе
- Используется для восстановления пароля
- Должен быть подтверждён перед использованием

## 🧪 Тестирование аутентификации

### Тестовые случаи

```csharp
// Тест: Успешная регистрация
[Test]
public async Task Register_WithValidEmail_ShouldSucceed()
{
    var result = await _auth.RegisterAsync(
        "test@example.com", 
        "Password123!", 
        "Password123!");

    Assert.IsTrue(result.IsSuccess);
}

// Тест: Регистрация с существующим email
[Test]
public async Task Register_WithExistingEmail_ShouldFail()
{
    var result = await _auth.RegisterAsync(
        "existing@example.com", 
        "Password123!", 
        "Password123!");

    Assert.IsFalse(result.IsSuccess);
    Assert.AreEqual(AuthError.EmailAlreadyExists, result.Error);
}
```

## 🔗 Связанные документы

- [Архитектура](./ARCHITECTURE.md)
- [Сетевое взаимодействие](./NETWORK.md)
- [UI/UX](./UI_UX.md)

---

**Следующий шаг**: Изучите [Сетевое взаимодействие](./NETWORK.md) для понимания работы с API.
