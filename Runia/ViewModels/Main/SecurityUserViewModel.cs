using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;

namespace Runia.ViewModels.Main
{
    public partial class SecurityUserViewModel : ObservableObject
    {
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;
        private readonly ILogger<SecurityUserViewModel> _logger;

        public SecurityUserViewModel(
            INavigationService nav,
            IDialogService dialog,
            ILogger<SecurityUserViewModel> logger)
        {
            _nav = nav;
            _dialog = dialog;
            _logger = logger;

            _logger.LogInformation("[SecurityUserViewModel] Initializing...");
        }

        // --- Данные безопасности ---
        [ObservableProperty] private string email = "g******2@bk.ru";
        [ObservableProperty] private string lastLoginDate = "01.02.2024";
        [ObservableProperty] private int activeSessions = 1;
        [ObservableProperty] private bool twoFactorEnabled;

        // --- Смена пароля ---
        [ObservableProperty] private string currentPassword = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;
        [ObservableProperty] private bool isChangingPassword;

        // --- Смена почты ---
        [ObservableProperty] private string newEmail = string.Empty;
        [ObservableProperty] private bool isChangingEmail;

        // --- Активные сессии ---
        [ObservableProperty] private List<SessionInfo> sessions =
        [
            new SessionInfo { DeviceName = "Chrome на Windows", Location = "Москва", LastActive = "Сейчас", SessionId = "1" },
            new SessionInfo { DeviceName = "Firefox на Ubuntu", Location = "Санкт-Петербург", LastActive = "2 часа назад", SessionId = "2" },
            new SessionInfo { DeviceName = "Safari на iPhone", Location = "Москва", LastActive = "1 день назад", SessionId = "3" }
        ];

        // --- Команды ---
        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                _dialog.ShowError("Заполните все поля", "Ошибка");
                return;
            }

            if (newPassword != confirmPassword)
            {
                _dialog.ShowError("Пароли не совпадают", "Ошибка");
                return;
            }

            IsChangingPassword = true;
            try
            {
                // TODO: Отправить запрос на смену пароля на сервер
                await Task.Delay(1500); // Имитация запроса

                _dialog.ShowMessage("Пароль успешно изменен!", "Успех");
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password");
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка");
            }
            finally
            {
                IsChangingPassword = false;
            }
        }

        [RelayCommand]
        private async Task ChangeEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                _dialog.ShowError("Введите новую почту", "Ошибка");
                return;
            }

            IsChangingEmail = true;
            try
            {
                // TODO: Отправить запрос на смену почты на сервер
                await Task.Delay(1500); // Имитация запроса

                _dialog.ShowMessage("На новую почту отправлена ссылка подтверждения", "Успех");
                NewEmail = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change email");
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка");
            }
            finally
            {
                IsChangingEmail = false;
            }
        }

        [RelayCommand]
        private async Task LogoutAllDevicesAsync()
        {
            var confirmed = _dialog.ShowConfirmation(
                "Вы выйдете из аккаунта на всех устройствах. Продолжить?",
                "Подтверждение");

            if (!confirmed) return;

            try
            {
                // TODO: Отправить запрос на выход со всех устройств
                await Task.Delay(1000);

                _dialog.ShowMessage("Вы вышли из всех сессий", "Успех");
                Sessions = [];
                ActiveSessions = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout all devices");
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private async Task LogoutDeviceAsync(string sessionId)
        {
            try
            {
                // TODO: Отправить запрос на выход с конкретного устройства
                await Task.Delay(800);

                var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session != null)
                {
                    sessions.Remove(session);
                    ActiveSessions = Math.Max(0, ActiveSessions - 1);
                }

                _dialog.ShowMessage("Сессия завершена", "Успех");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout device");
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        [RelayCommand]
        private void EnableTwoFactor()
        {
            // TODO: Реализовать включение 2FA
            _dialog.ShowMessage("Двухфакторная аутентификация не реализована", "TODO");
        }
    }

    public class SessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string LastActive { get; set; } = string.Empty;
    }
}
