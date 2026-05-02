using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using Runia.Ui.Windows;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace Runia.ViewModels.Auth
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _auth;
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;

        public LoginViewModel(AuthService auth, IDialogService dialog, INavigationService nav)
        {
            _auth = auth;
            _dialog = dialog;
            _nav = nav;
            Debug.WriteLine("[LoginViewModel] Constructor called");
        }

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private bool isLoading;

        partial void OnUsernameChanged(string value)
        {
            Debug.WriteLine($"[LoginViewModel.OnUsernameChanged] New value: {value}");
            // Пересчитываем CanExecute для всех команд при изменении Username
        }

        partial void OnPasswordChanged(string value)
        {
            Debug.WriteLine($"[LoginViewModel.OnPasswordChanged] New value: {value}");
            // Пересчитываем CanExecute для всех команд при изменении Password
        }

        [RelayCommand]
        private async Task Login()
        {
            Debug.WriteLine($"Login Clicked. User: {Username}, Pass length: {Password?.Length}, Remember: {RememberMe}");

            // ✅ ИСПРАВЛЕНО: Использование свойств Username и Password вместо приватных полей
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Debug.WriteLine("[LoginCommand] Validation failed - empty fields");
                _dialog.ShowError("Заполните все поля");
                return;
            }

            IsLoading = true;
            try
            {
                Debug.WriteLine("[LoginCommand] Attempting login...");
                var result = await _auth.LoginAsync(Username, Password);
                Debug.WriteLine($"[LoginCommand] Login result: {result}");

                if (result == AuthResult.Success)
                {
                    Debug.WriteLine("[LoginCommand] Login successful!");
                    _auth.SaveTokens(RememberMe);
                
                    _dialog.ShowMainWindow();
                }
                else
                {
                    Debug.WriteLine("[LoginCommand] Login failed");
                    _dialog.ShowError("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginCommand] Exception: {ex}");
                _dialog.ShowError(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanLogin() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        [RelayCommand]
        private void LoginDebug()
        {
            Debug.WriteLine("[LoginDebugCommand] Called - bypassing auth");
            _dialog.ShowMainWindow();
        }

        [RelayCommand]
        private void Register()
        {
            try
            {
                Debug.WriteLine("[RegisterCommand] Called");
                _nav?.NavigateTo<Runia.ViewModels.Auth.RegisterViewModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RegisterCommand] Exception: {ex}");
                _dialog.ShowError("Не удалось открыть регистрацию: " + ex.Message);
            }
        }

        [RelayCommand]
        private void ForgotPassword()
        {
            try
            {
                Debug.WriteLine("[ForgotPasswordCommand] Called");
                _nav?.NavigateTo<Runia.ViewModels.Auth.ForgotPasswordViewModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ForgotPasswordCommand] Exception: {ex}");
                _dialog.ShowError("Не удалось открыть восстановление: " + ex.Message);
            }
        }
    }
}