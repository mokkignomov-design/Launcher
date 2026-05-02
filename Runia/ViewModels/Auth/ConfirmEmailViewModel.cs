using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System.Diagnostics;
using System;

namespace Runia.ViewModels.Auth;
public partial class ConfirmEmailViewModel(AuthService auth, IDialogService dialog, INavigationService nav, string email) : ObservableObject
{
    private readonly AuthService _auth = auth ?? throw new ArgumentNullException(nameof(auth));
    private readonly IDialogService _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    private readonly INavigationService _nav = nav ?? throw new ArgumentNullException(nameof(nav));
    [ObservableProperty]
    private string _email = email ?? string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _code = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task Confirm()
    {
        Debug.WriteLine($"[ConfirmEmail] Confirming for: {Email}");

        if (string.IsNullOrWhiteSpace(Code) || Code.Length != 6)
        {
            _dialog.ShowError("Введите корректный 6-значный код");
            return;
        }

        Debug.WriteLine($"[ConfirmEmail] Auth.AccessToken: {_auth?.AccessToken}");

        IsLoading = true;
        try
        {
            // Guard against unexpected nulls for injected services. In normal operation these
            // are set in the constructor, but the null-check satisfies the static analyzer
            // and protects at runtime in case of incorrect construction.
            var auth = _auth;
            if (auth == null)
            {
                Debug.WriteLine("[ConfirmEmail] _auth is null - aborting");
                _dialog?.ShowError("Внутренняя ошибка. Повторите попытку.", "Ошибка");
                return;
            }

            var result = await auth.ConfirmEmailAsync(Email, Code);

            if (result == AuthResult.Success)
            {
                _dialog.ShowMessage("Почта подтверждена", "Успех");
                _nav.NavigateTo<LoginViewModel>();
            }
            else
            {
                _dialog.ShowError("Не удалось подтвердить почту");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfirmEmail] Exception: {ex}");
            _dialog.ShowError(ex.Message, "Ошибка");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Resend()
    {
        Debug.WriteLine("[ConfirmEmailViewModel.Resend] Called");
        _dialog.ShowMessage("Письмо с кодом отправлено повторно", "Info");
    }

    private bool CanConfirm()
    {
        return !string.IsNullOrWhiteSpace(Code) && Code.Length == 6 && !IsLoading;
    }

    [RelayCommand]
    private void BackToLogin() => _nav.NavigateTo<LoginViewModel>();
}
