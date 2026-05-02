using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Runia.ViewModels.Auth
{
    public partial class ResetPasswordViewModel : ObservableObject
    {
        private readonly AuthService _auth;
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;

        public ResetPasswordViewModel(AuthService auth, IDialogService dialog, INavigationService nav)
        {
            _auth = auth;
            _dialog = dialog;
            _nav = nav;
            Debug.WriteLine("[ResetPasswordViewModel] Constructor called");
        }

        [ObservableProperty]
        private string code = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [RelayCommand]
        private async Task ConfirmReset()
        {
            Debug.WriteLine("[ConfirmResetCommand] Called");
            
            if (string.IsNullOrWhiteSpace(code))
            {
                Debug.WriteLine("[ConfirmResetCommand] Code is empty");
                _dialog.ShowError("Введите код подтверждения", "Ошибка");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                Debug.WriteLine("[ConfirmResetCommand] Password fields are empty");
                _dialog.ShowError("Введите пароль и подтверждение", "Ошибка");
                return;
            }

            if (newPassword != confirmPassword)
            {
                Debug.WriteLine("[ConfirmResetCommand] Passwords don't match");
                _dialog.ShowError("Пароли не совпадают", "Ошибка");
                return;
            }
            IsLoading = true;
            try
            {
                Debug.WriteLine("[ConfirmResetCommand] Resetting password...");

                var email = _auth.LastRequestedResetEmail;
                if (string.IsNullOrWhiteSpace(email))
                {
                    Debug.WriteLine("[ConfirmResetCommand] No email available for reset");
                    _dialog.ShowError("Неизвестный email для сброса. Повторите запрос сброса пароля.", "Ошибка");
                    return;
                }

                var result = await _auth.ResetPasswordAsync(email, Code, NewPassword);
                if (result == AuthResult.Success)
                {
                    _dialog.ShowMessage("Пароль успешно сброшен", "Успех");

                    // Navigate back to Login
                    if (App.Services?.GetService(typeof(Runia.ViewModels.Auth.LoginViewModel)) is Runia.ViewModels.Auth.LoginViewModel loginVm)
                    {
                        Debug.WriteLine("[ConfirmResetCommand] Navigating back to LoginViewModel");
                        _nav?.NavigateTo(loginVm);
                    }
                }
                else if (result == AuthResult.NetworkError)
                {
                    _dialog.ShowError("Сеть недоступна. Проверьте подключение.", "Ошибка");
                }
                else
                {
                    _dialog.ShowError("Не удалось сбросить пароль. Проверьте код и попробуйте снова.", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfirmResetCommand] Exception: {ex}");
                _dialog.ShowError(ex.Message, "Ошибка");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void BackToForgotPassword()
        {
            Debug.WriteLine("[BackToForgotPasswordCommand] Called");
            if (App.Services?.GetService(typeof(ForgotPasswordViewModel)) is ForgotPasswordViewModel forgotVm)
            {
                Debug.WriteLine("[BackToForgotPasswordCommand] Navigating to ForgotPasswordViewModel");
                _nav?.NavigateTo(forgotVm);
            }
            else
            {
                Debug.WriteLine("[BackToForgotPasswordCommand] ForgotPasswordViewModel not found");
            }
        }
    }
}
