using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Runia.Services.Common;
using Runia.Services.Auth;
using Runia.Services.Core.Infrastructure.Network.Clients;
using System.Globalization;
using System.Text.Json;
using System.Diagnostics;

namespace Runia.ViewModels.Main
{
    public partial class SecurityUserViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly ILogger<SecurityUserViewModel> _logger;
        private readonly AuthClient _authClient;
        private readonly UserClient _userClient;
        private readonly AuthService _authService;
        private readonly UserViewModel _profileViewModel;

        public SecurityUserViewModel(
            IDialogService dialog,
            ILogger<SecurityUserViewModel> logger,
            AuthClient authClient,
            UserClient userClient,
            AuthService authService,
            UserViewModel profileViewModel)
        {
            _dialog = dialog;
            _logger = logger;
            _authClient = authClient;
            _userClient = userClient;
            _authService = authService;
            _profileViewModel = profileViewModel;

            _logger.LogInformation("[SecurityUserViewModel] Initializing...");
            _ = InitializeAsync();
        }

        [ObservableProperty] private string primaryEmail = string.Empty;
        [ObservableProperty] private string accountError = string.Empty;
        [ObservableProperty] private bool isEmailChangeFormOpen;
        [ObservableProperty] private int emailChangeStep = 1;
        [ObservableProperty] private string newEmail = string.Empty;
        [ObservableProperty] private string currentPassword = string.Empty;
        [ObservableProperty] private string emailChangeCode = string.Empty;
        [ObservableProperty] private bool isRequestingEmailChange;
        [ObservableProperty] private bool isConfirmingEmailChange;
        [ObservableProperty] private string emailChangeError = string.Empty;
        [ObservableProperty] private string emailChangeMessage = string.Empty;
        [ObservableProperty] private int activeSessions;
        [ObservableProperty] private bool isLoadingSessions;
        [ObservableProperty] private bool isSessionCountAvailable;
        [ObservableProperty] private string sessionsError = string.Empty;
        [ObservableProperty] private List<SessionInfo> sessions = [];
        [ObservableProperty] private string resetEmail = string.Empty;
        [ObservableProperty] private string resetCode = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;
        [ObservableProperty] private bool isRequestingResetCode;
        [ObservableProperty] private bool isChangingPassword;
        [ObservableProperty] private int passwordResetStep = 1;
        [ObservableProperty] private string passwordResetError = string.Empty;
        [ObservableProperty] private string passwordResetMessage = string.Empty;

        public bool IsPasswordResetEmailStep => PasswordResetStep == 1;
        public bool IsPasswordResetCodeStep => PasswordResetStep == 2;
        public bool IsPasswordResetPasswordStep => PasswordResetStep == 3;
        public bool IsEmailChangeDetailsStep => EmailChangeStep == 1;
        public bool IsEmailChangeConfirmationStep => EmailChangeStep == 2;

        partial void OnPasswordResetStepChanged(int value)
        {
            OnPropertyChanged(nameof(IsPasswordResetEmailStep));
            OnPropertyChanged(nameof(IsPasswordResetCodeStep));
            OnPropertyChanged(nameof(IsPasswordResetPasswordStep));
        }

        partial void OnEmailChangeStepChanged(int value)
        {
            OnPropertyChanged(nameof(IsEmailChangeDetailsStep));
            OnPropertyChanged(nameof(IsEmailChangeConfirmationStep));
        }

        private async Task InitializeAsync()
        {
            await LoadAccountAsync();
            await LoadSessionsAsync();
        }

        private async Task LoadAccountAsync()
        {
            try
            {
                var result = await _userClient.GetCurrentUserProfileAsync();
                if (!result.IsSuccess || result.Data is null)
                {
                    AccountError = result.ErrorMessage ?? "Не удалось загрузить данные аккаунта.";
                    return;
                }

                PrimaryEmail = result.Data.PrimaryEmail ?? "Не указана";
                _profileViewModel.PrimaryEmail = PrimaryEmail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load account details");
                AccountError = "Не удалось загрузить данные аккаунта.";
            }
        }

        [RelayCommand]
        private void StartEmailChange()
        {
            NewEmail = string.Empty;
            CurrentPassword = string.Empty;
            EmailChangeCode = string.Empty;
            EmailChangeError = string.Empty;
            EmailChangeMessage = string.Empty;
            EmailChangeStep = 1;
            IsEmailChangeFormOpen = true;
        }

        [RelayCommand]
        private void CancelEmailChange()
        {
            IsEmailChangeFormOpen = false;
            NewEmail = string.Empty;
            CurrentPassword = string.Empty;
            EmailChangeCode = string.Empty;
            EmailChangeError = string.Empty;
            EmailChangeMessage = string.Empty;
            EmailChangeStep = 1;
        }

        [RelayCommand]
        private void BackToEmailChangeDetails()
        {
            EmailChangeError = string.Empty;
            EmailChangeMessage = string.Empty;
            EmailChangeCode = string.Empty;
            CurrentPassword = string.Empty;
            EmailChangeStep = 1;
        }

        [RelayCommand]
        private async Task RequestEmailChangeCodeAsync()
        {
            EmailChangeError = string.Empty;
            EmailChangeMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(CurrentPassword))
            {
                EmailChangeError = "Введите новую почту и текущий пароль.";
                return;
            }

            IsRequestingEmailChange = true;
            try
            {
                var result = await _authClient.RequestEmailChangeAsync(NewEmail.Trim(), CurrentPassword);
                if (!result.IsSuccess)
                {
                    EmailChangeError = result.ErrorMessage ?? "Не удалось запросить смену почты.";
                    return;
                }

                CurrentPassword = string.Empty;
                EmailChangeCode = string.Empty;
                EmailChangeStep = 2;
                EmailChangeMessage = string.IsNullOrWhiteSpace(result.Data?.Message)
                    ? $"Введите код, отправленный на {NewEmail.Trim()}."
                    : result.Data.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request account email change");
                EmailChangeError = "Не удалось отправить код подтверждения.";
            }
            finally
            {
                IsRequestingEmailChange = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmEmailChangeAsync()
        {
            EmailChangeError = string.Empty;
            EmailChangeMessage = string.Empty;
            var code = EmailChangeCode.Trim();
            if (code.Length != 6 || code.Any(character => character < '0' || character > '9'))
            {
                EmailChangeError = "Введите шестизначный код из письма.";
                return;
            }

            IsConfirmingEmailChange = true;
            try
            {
                var result = await _authClient.ConfirmEmailChangeAsync(code);
                if (!result.IsSuccess || result.Data is null)
                {
                    EmailChangeError = result.ErrorMessage ?? "Не удалось подтвердить новую почту.";
                    return;
                }

                await _authService.ReplaceTokensAsync(result.Data);
                var profileResult = await _userClient.GetCurrentUserProfileAsync();
                IsEmailChangeFormOpen = false;
                EmailChangeCode = string.Empty;
                CurrentPassword = string.Empty;
                EmailChangeStep = 1;

                if (profileResult.IsSuccess && profileResult.Data is not null)
                {
                    PrimaryEmail = profileResult.Data.PrimaryEmail ?? "Не указана";
                    _profileViewModel.PrimaryEmail = PrimaryEmail;
                    AccountError = string.Empty;
                    EmailChangeMessage = "Основная почта подтверждена и обновлена.";
                }
                else
                {
                    EmailChangeMessage = "Почта подтверждена, но не удалось обновить её маску в профиле. Обновите страницу позже.";
                    _logger.LogWarning("Email change was confirmed, but the current profile could not be refreshed: {Error}", profileResult.ErrorMessage);
                }

                NewEmail = string.Empty;
                await LoadSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm account email change");
                EmailChangeError = "Не удалось завершить смену почты. Повторите попытку.";
            }
            finally
            {
                IsConfirmingEmailChange = false;
            }
        }

        [RelayCommand]
        private async Task LoadSessionsAsync()
        {
            IsLoadingSessions = true;
            IsSessionCountAvailable = false;
            SessionsError = string.Empty;
            try
            {
                var result = await _authClient.GetSessionsAsync();
                if (!result.IsSuccess)
                {
                    SessionsError = result.ErrorMessage ?? "Не удалось загрузить список сессий.";
                    _logger.LogWarning("GET /api/auth/sessions failed: {Error}", SessionsError);
                    Debug.WriteLine($"[SecurityUserViewModel] GET /api/auth/sessions failed: {SessionsError}");
                    return;
                }

                var sessionArray = FindSessionArray(result.Data);
                if (sessionArray is null)
                {
                    SessionsError = "Сервер вернул неизвестный формат списка сессий.";
                    var shape = DescribeSessionShape(result.Data);
                    _logger.LogWarning("GET /api/auth/sessions returned an unsupported response shape: {Shape}", shape);
                    Debug.WriteLine($"[SecurityUserViewModel] GET /api/auth/sessions response shape: {shape}");
                    return;
                }

                var loadedSessions = new List<SessionInfo>();
                var invalidItems = 0;
                foreach (var item in sessionArray.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object
                        || !TryReadSessionId(item, out var sessionId))
                    {
                        invalidItems++;
                        continue;
                    }

                    loadedSessions.Add(new SessionInfo
                    {
                        SessionId = sessionId,
                        DeviceName = ReadString(item, "deviceName", "device", "userAgent") ?? "Устройство",
                        Location = ReadString(item, "location", "ipAddress", "ip") ?? "—",
                        LastActive = FormatLastActive(ReadString(item, "lastActive", "lastUsedAt", "createdAt"))
                    });
                }

                if (invalidItems > 0 && loadedSessions.Count == 0)
                {
                    SessionsError = "Не удалось распознать записи сессий в ответе сервера.";
                    var shape = DescribeSessionShape(result.Data);
                    _logger.LogWarning("GET /api/auth/sessions records could not be parsed: {Shape}", shape);
                    Debug.WriteLine($"[SecurityUserViewModel] GET /api/auth/sessions records could not be parsed: {shape}");
                    return;
                }

                Sessions = loadedSessions;
                ActiveSessions = loadedSessions.Count;
                IsSessionCountAvailable = invalidItems == 0;
                _logger.LogInformation("GET /api/auth/sessions returned {Count} parseable sessions.", loadedSessions.Count);
                Debug.WriteLine($"[SecurityUserViewModel] GET /api/auth/sessions returned {loadedSessions.Count} parseable sessions.");
                if (invalidItems > 0)
                {
                    SessionsError = $"Не удалось показать записей: {invalidItems}.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load account sessions");
                SessionsError = "Не удалось загрузить список сессий.";
            }
            finally
            {
                IsLoadingSessions = false;
            }
        }

        [RelayCommand]
        private async Task RequestResetCodeAsync()
        {
            PasswordResetError = string.Empty;
            PasswordResetMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ResetEmail))
            {
                PasswordResetError = "Введите почту аккаунта.";
                return;
            }

            IsRequestingResetCode = true;
            try
            {
                var result = await _authClient.RequestPasswordResetAsync(ResetEmail.Trim());
                if (!result.IsSuccess)
                {
                    PasswordResetError = result.ErrorMessage ?? "Не удалось отправить код.";
                    return;
                }

                ResetCode = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
                PasswordResetStep = 2;
                PasswordResetMessage = $"Если адрес зарегистрирован, код отправлен на {ResetEmail.Trim()}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request password reset code");
                PasswordResetError = "Не удалось отправить код подтверждения.";
            }
            finally
            {
                IsRequestingResetCode = false;
            }
        }

        [RelayCommand]
        private void ContinueToNewPassword()
        {
            PasswordResetError = string.Empty;
            PasswordResetMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(ResetCode))
            {
                PasswordResetError = "Введите код из письма.";
                return;
            }

            PasswordResetStep = 3;
        }

        [RelayCommand]
        private void BackToResetEmail()
        {
            PasswordResetError = string.Empty;
            PasswordResetMessage = string.Empty;
            PasswordResetStep = 1;
        }

        [RelayCommand]
        private void BackToResetCode()
        {
            PasswordResetError = string.Empty;
            PasswordResetMessage = string.Empty;
            PasswordResetStep = 2;
        }

        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            PasswordResetError = string.Empty;
            PasswordResetMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ResetCode))
            {
                PasswordResetError = "Вернитесь на шаг с кодом и введите его.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword != ConfirmPassword)
            {
                PasswordResetError = "Введите новый пароль и повторите его без ошибок.";
                return;
            }

            IsChangingPassword = true;
            try
            {
                var result = await _authClient.ConfirmPasswordResetAsync(ResetEmail.Trim(), ResetCode.Trim(), NewPassword);
                if (!result.IsSuccess)
                {
                    if (IsInvalidResetCodeError(result.ErrorMessage))
                    {
                        PasswordResetStep = 2;
                        PasswordResetError = "Код неверный или срок его действия истёк. Проверьте код или запросите новый.";
                    }
                    else
                    {
                        PasswordResetError = result.ErrorMessage ?? "Не удалось изменить пароль.";
                    }
                    return;
                }

                _dialog.ShowMessage("Пароль успешно изменён.", "Сброс пароля");
                ResetCode = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
                PasswordResetStep = 1;
                PasswordResetMessage = "Пароль успешно изменён.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password");
                PasswordResetError = "Не удалось изменить пароль.";
            }
            finally
            {
                IsChangingPassword = false;
            }
        }

        [RelayCommand]
        private async Task LogoutAllDevicesAsync()
        {
            var confirmed = _dialog.ShowConfirmation(
                "Вы выйдете из аккаунта на всех устройствах. Продолжить?",
                "Подтверждение");

            if (!confirmed)
            {
                return;
            }

            try
            {
                var result = await _authClient.RevokeAllSessionsAsync();
                if (!result.IsSuccess)
                {
                    _dialog.ShowError(result.ErrorMessage ?? "Не удалось завершить сессии.", "Безопасность");
                    return;
                }

                Sessions = [];
                ActiveSessions = 0;
                IsSessionCountAvailable = true;
                await _authService.LogoutAsync();
                _dialog.ShowLoginWindow();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout all devices");
                _dialog.ShowError("Не удалось завершить все сессии.", "Безопасность");
            }
        }

        [RelayCommand]
        private async Task LogoutDeviceAsync(int sessionId)
        {
            try
            {
                var result = await _authClient.RevokeSessionAsync(sessionId);
                if (!result.IsSuccess)
                {
                    _dialog.ShowError(result.ErrorMessage ?? "Не удалось завершить сессию.", "Безопасность");
                    return;
                }

                Sessions = Sessions.Where(session => session.SessionId != sessionId).ToList();
                ActiveSessions = Sessions.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout device");
                _dialog.ShowError("Не удалось завершить сессию.", "Безопасность");
            }
        }

        private static JsonElement? FindSessionArray(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var name in new[] { "sessions", "activeSessions", "items", "data", "devices", "results" })
            {
                var value = ReadProperty(element, name);
                if (value is not null)
                {
                    var array = FindSessionArray(value.Value);
                    if (array is not null)
                    {
                        return array;
                    }
                }
            }

            return null;
        }

        private static string DescribeSessionShape(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var itemShape = element.GetArrayLength() > 0 && element[0].ValueKind == JsonValueKind.Object
                    ? string.Join(",", element[0].EnumerateObject().Select(property => property.Name))
                    : "no object fields";
                return $"array[{element.GetArrayLength()}]; first item: {itemShape}";
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                return $"object keys: {string.Join(",", element.EnumerateObject().Select(property => property.Name))}";
            }

            return element.ValueKind.ToString();
        }

        private static bool IsInvalidResetCodeError(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            var value = error.ToLowerInvariant();
            var mentionsCode = value.Contains("code", StringComparison.Ordinal)
                || value.Contains("код", StringComparison.Ordinal)
                || value.Contains("token", StringComparison.Ordinal);
            var indicatesInvalid = value.Contains("invalid", StringComparison.Ordinal)
                || value.Contains("incorrect", StringComparison.Ordinal)
                || value.Contains("wrong", StringComparison.Ordinal)
                || value.Contains("expired", StringComparison.Ordinal)
                || value.Contains("does not match", StringComparison.Ordinal)
                || value.Contains("not match", StringComparison.Ordinal)
                || value.Contains("невер", StringComparison.Ordinal)
                || value.Contains("не совпад", StringComparison.Ordinal)
                || value.Contains("истек", StringComparison.Ordinal)
                || value.Contains("просроч", StringComparison.Ordinal);
            return mentionsCode && indicatesInvalid;
        }

        private static bool TryReadSessionId(JsonElement item, out int sessionId)
        {
            sessionId = 0;
            var value = ReadProperty(item, "id") ?? ReadProperty(item, "sessionId");
            if (value is null)
            {
                return false;
            }

            if (value.Value.ValueKind == JsonValueKind.Number)
            {
                return value.Value.TryGetInt32(out sessionId);
            }

            return value.Value.ValueKind == JsonValueKind.String
                && int.TryParse(value.Value.GetString(), out sessionId);
        }

        private static string? ReadString(JsonElement item, params string[] names)
        {
            foreach (var name in names)
            {
                var value = ReadProperty(item, name);
                if (value is null)
                {
                    continue;
                }

                if (value.Value.ValueKind == JsonValueKind.String)
                {
                    var text = value.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
                else if (value.Value.ValueKind == JsonValueKind.Number)
                {
                    return value.Value.GetRawText();
                }
            }

            return null;
        }

        private static JsonElement? ReadProperty(JsonElement item, string name)
        {
            foreach (var property in item.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static string FormatLastActive(string? value)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            {
                return timestamp.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }

            return value ?? "—";
        }
    }

    public class SessionInfo
    {
        public int SessionId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string LastActive { get; set; } = string.Empty;
    }
}
