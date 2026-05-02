using CommunityToolkit.Mvvm.ComponentModel;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace Runia.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;
        private readonly AuthService _auth;

        [ObservableProperty] private string _userName = "Гость";
        [ObservableProperty] private string _userRank = "Игрок";
        [ObservableProperty] private string _balance = "0";

        public MainWindowViewModel(IDialogService dialog, INavigationService nav, AuthService auth)
        {
            _dialog = dialog;
            _nav = nav;
            _auth = auth;

            UpdateUserInfo();
        }

        [RelayCommand]
        private static void Close()
        {
            Debug.WriteLine("[MainWindowViewModel] Close command invoked");
            try
            {
                var win = System.Windows.Application.Current?.Windows.OfType<Runia.Ui.Windows.MainWindow>().FirstOrDefault();
                win?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindowViewModel] Close failed: {ex}");
            }
        }

        [RelayCommand]
        private static void Minimize()
        {
            Debug.WriteLine("[MainWindowViewModel] Minimize command invoked");
            try
            {
                var win = System.Windows.Application.Current?.Windows.OfType<Runia.Ui.Windows.MainWindow>().FirstOrDefault();
                if (win != null) win.WindowState = System.Windows.WindowState.Minimized;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindowViewModel] Minimize failed: {ex}");
            }
        }

        /// <summary>
        /// Инициализация навигации для главного окна
        /// </summary>
        /// <param name="contentControl">Элемент из XAML (например, MainFrame), где будет контент</param>
     

        private void UpdateUserInfo()
        {
            if (_auth.CurrentUser != null)
            {
                UserName = _auth.CurrentUser.Name;
                UserRank = _auth.CurrentUser.Rank;
                Balance = _auth.CurrentUser.Balance.ToString();
            }
        }
    }
}