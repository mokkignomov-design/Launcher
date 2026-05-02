using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Runia.ViewModels.Auth;

public partial class RegisterViewModel(AuthService auth, IDialogService dialog, INavigationService nav) : ObservableObject
{
    private readonly AuthService _auth = auth;
    private readonly IDialogService _dialog = dialog;
    private readonly INavigationService _nav = nav;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;
    private bool _isPasswordLengthOk;
    public bool IsPasswordLengthOk
    {
        get => _isPasswordLengthOk;
        set
        {
            if (SetProperty(ref _isPasswordLengthOk, value))
            {
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // --- ОБРАБОТЧИКИ ---

    partial void OnPasswordChanged(string value)
    {
        IsPasswordLengthOk = !string.IsNullOrEmpty(value) && value.Length >= 6;

        Debug.WriteLine($"[RegisterVM] Password changed. OK: {IsPasswordLengthOk}");
    }

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task Register(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var result = await _auth.RegisterAsync(Username, Email, Password, ct);
            if (result == AuthResult.Success)
            {
                _dialog.ShowMessage("Аккаунт успешно создан! Теперь подтвердите почту.", "Успех");
                _nav.NavigateTo<ConfirmEmailViewModel>(Email);
            }
            else
            {
                _dialog.ShowError($"Не удалось зарегистрировать: {result}");
            }
        }
        catch (Exception ex)
        {
            _dialog.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRegister()
    {
        bool hasUser = !string.IsNullOrWhiteSpace(Username);
        bool hasEmail = !string.IsNullOrWhiteSpace(Email) && Email.Contains('@');
        bool hasPass = IsPasswordLengthOk;

        return hasUser && hasEmail && hasPass && !IsLoading;
    }

    [RelayCommand]
    private void BackToLogin() => _nav.NavigateTo<LoginViewModel>();
}