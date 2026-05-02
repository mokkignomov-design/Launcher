using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System.Diagnostics;



namespace Runia.ViewModels.Windows
{
    public partial class LoginWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? currentPageViewModel;

        private readonly AuthService _auth;
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;

        public LoginWindowViewModel(AuthService auth, IDialogService dialog, INavigationService nav)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            Debug.WriteLine("[LoginWindowViewModel] Constructor called");
        }

        [RelayCommand]
        private static void DragMove()
        {
            Debug.WriteLine("[LoginWindowViewModel.DragMove] Requested");
        }

        [RelayCommand]
        private void Close()
        {
            Debug.WriteLine("[LoginWindowViewModel.Close] Requested");
            _dialog.CloseLoginWindow();
        }

        [RelayCommand]
        private void ShowMain()
        {
            Debug.WriteLine("[LoginWindowViewModel.ShowMain] Requested");
            _dialog.ShowMainWindow();
        }


        [RelayCommand]
        public async Task Initialize(object hostControl)
        {
            Debug.WriteLine("[LoginWindowViewModel.Initialize] Called with hostControl: " + (hostControl?.GetType().Name ?? "null"));

            try
            {
                if (hostControl is System.Windows.Controls.ContentControl cc)
                {
                    _nav.SetHost(cc);

                    try { _nav.RegisterMapping<Runia.ViewModels.Auth.LoginViewModel, Runia.Ui.PagesUser.LoginPage>(); } catch { }
                    try { _nav.RegisterMapping<Runia.ViewModels.Auth.RegisterViewModel, Runia.Ui.PagesUser.RegisterPage>(); } catch { }
                    try { _nav.RegisterMapping<Runia.ViewModels.Auth.ForgotPasswordViewModel, Runia.Ui.PagesUser.ForgotPasswordPage>(); } catch { }
                    try { _nav.RegisterMapping<Runia.ViewModels.Auth.ConfirmEmailViewModel, Runia.Ui.PagesUser.ConfirmEmailPage>(); } catch { }

                    Debug.WriteLine("[LoginWindowViewModel] Navigation host set for LoginWindow");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginWindowViewModel] Failed to set navigation host: {ex}");
            }

            var result = await _auth.LoadAndRefreshTokensAsync();
            if (result == Runia.Services.Auth.AuthResult.Success)
            {
                Debug.WriteLine("[LoginWindowViewModel.Initialize] Auto-login success - switching windows");
                _dialog.ShowMainWindow();
                _dialog.CloseLoginWindow(); 
                return;
            }

            if (_nav != null)
            {
                try
                {
                    _nav.NavigateTo<Runia.ViewModels.Auth.LoginViewModel>();
                    Debug.WriteLine("[LoginWindowViewModel] Navigated to LoginViewModel via NavigationService");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LoginWindowViewModel] Navigation to LoginViewModel failed: {ex}");
                }
            }
            else
            {
                if (App.Services?.GetService(typeof(Runia.ViewModels.Auth.LoginViewModel)) is Runia.ViewModels.Auth.LoginViewModel loginVm)
                {
                    CurrentPageViewModel = loginVm;
                    Debug.WriteLine("[LoginWindowViewModel] CurrentPageViewModel set to LoginViewModel");
                }
                else
                {
                    Debug.WriteLine("[LoginWindowViewModel] LoginViewModel not found in DI");
                }
            }
        }
    }
}
