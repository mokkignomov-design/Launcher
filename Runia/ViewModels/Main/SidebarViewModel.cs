using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System;
using System.Diagnostics;

namespace Runia.ViewModels.Main
{
    public partial class SidebarViewModel : ObservableObject
    {
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;
        private readonly IServiceProvider _services;
        private readonly AuthService _auth;

        [ObservableProperty] private bool _isProfileSelected;
        [ObservableProperty] private bool _isHomeSelected;
        [ObservableProperty] private bool _isServersSelected;
        [ObservableProperty] private bool _isSettingsSelected;

        public SidebarViewModel(INavigationService nav, IDialogService dialog, IServiceProvider services, AuthService auth)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            IsHomeSelected = true;
        }

        #region Навигация

        [RelayCommand]
        private void GoHome()
        {
            if (_nav.CurrentViewModel is HomeViewModel) return;
            _nav.NavigateTo<HomeViewModel>();
        }

        [RelayCommand]
        private void GoProfile()
        {
            try
            {
                _nav.NavigateTo<UserViewModel>();
            }
            catch (Exception ex) { Debug.WriteLine($"[Sidebar.GoProfile] Exception: {ex}"); }
        }

        [RelayCommand]
        private void GoServers()
        {
            try
            {
                _nav.NavigateTo<ServerViewModel>();
            }
            catch (Exception ex) { Debug.WriteLine($"[Sidebar.GoServers] Exception: {ex}"); }
        }

        [RelayCommand]
        private void GoSettings()
        {
            try
            {
                _nav.NavigateTo<SettingsViewModel>();
            }
            catch (Exception ex) { Debug.WriteLine($"[Sidebar.GoSettings] Exception: {ex}"); }
        }

        #endregion

        #region Сессия

        [RelayCommand]
        private void Logout()
        {
            try
            {
                Debug.WriteLine("[Sidebar] Logout");
                _auth.Logout();
                _dialog.ShowLoginWindow();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sidebar.Logout] Exception: {ex}");
            }
        }

        #endregion
    }
}