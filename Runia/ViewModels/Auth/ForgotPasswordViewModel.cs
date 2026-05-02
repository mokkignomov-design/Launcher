using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System.Diagnostics;


namespace Runia.ViewModels.Auth
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private readonly AuthService _auth;
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;

        public ForgotPasswordViewModel(AuthService auth, IDialogService dialog, INavigationService nav)
        {
            _auth = auth;
            _dialog = dialog;
            _nav = nav;
            Debug.WriteLine("[ForgotPasswordViewModel] Constructor called");
        }

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [RelayCommand]
        private async Task ResetPassword()
        {
            Debug.WriteLine("[ResetPasswordCommand] Called");

            if (string.IsNullOrWhiteSpace(email))
            {
                Debug.WriteLine("[ResetPasswordCommand] Email is empty");
                _dialog.ShowError("Введите email", "Ошибка");
                return;
            }

            if (!email.Contains('@') || !email.Contains('.'))
            {
                Debug.WriteLine("[ResetPasswordCommand] Email validation failed - invalid format");
                _dialog.ShowError("Неверный формат почты. Email должен содержать '@' и '.'", "Ошибка");
                return;
            }

            IsLoading = true;
            try
            {
                Debug.WriteLine($"[ResetPasswordCommand] Sending reset code to {email}");
                var result = await _auth.RequestPasswordResetAsync(email);
                if (result == AuthResult.Success)
                {
                    _dialog.ShowMessage("Проверочный код отправлен на вашу почту", "Успех");
                    _nav?.NavigateTo<Runia.ViewModels.Auth.ResetPasswordViewModel>();
                }
                else if (result == AuthResult.NetworkError)
                {
                    _dialog.ShowError("Сеть недоступна. Проверьте подключение.", "Ошибка");
                }
                else
                {
                    _dialog.ShowError("Не удалось отправить код. Попробуйте позже.", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ResetPasswordCommand] Exception: {ex}");
                _dialog.ShowError(ex.Message, "Ошибка");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void BackToLogin()
        {
            Debug.WriteLine("[BackToLoginCommand] Called from ForgotPasswordViewModel");
            if (App.Services?.GetService(typeof(LoginViewModel)) is LoginViewModel loginVm)
            {
                Debug.WriteLine("[BackToLoginCommand] Navigating to LoginViewModel");
                _nav?.NavigateTo(loginVm);
            }
            else
            {
                Debug.WriteLine("[BackToLoginCommand] LoginViewModel not found");
            }
        }
    }
}
