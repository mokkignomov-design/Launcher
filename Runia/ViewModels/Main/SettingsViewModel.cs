using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Common;
using System.Linq;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace Runia.ViewModels.Main
{
    /// <summary>
    /// SettingsViewModel управляет настройками приложения.
    /// 
    /// ВНИМАНИЕ: Username, AccessToken и RefreshToken являются защищенными и управляются ТОЛЬКО AuthService.
    /// Они НЕ должны храниться в этом ViewModel и НЕ должны быть доступны для редактирования пользователем.
    /// Все критические данные аутентификации хранятся в защищенном хранилище (SecureTokenStore).
    /// </summary>
    public partial class SettingsViewModel(IDialogService dialog) : ObservableObject
    {
        private readonly IDialogService _dialog = dialog;

        [ObservableProperty] private bool _isDarkMode = Runia.Properties.Settings.Default.LastTheme == "Dark";
        [ObservableProperty] private bool _showConsole = Runia.Properties.Settings.Default.ShowConsole;
        [ObservableProperty] private bool _isFullscreen = Runia.Properties.Settings.Default.IsFullscreen;
        [ObservableProperty] private bool _autoClose = Runia.Properties.Settings.Default.AutoCloseLauncher;
        [ObservableProperty] private int _ramAmount = Runia.Properties.Settings.Default.RamAmount;
        [ObservableProperty] private string _jvmArgs = Runia.Properties.Settings.Default.JvmArguments;
        [ObservableProperty] private string _gamePath = Runia.Properties.Settings.Default.GameRootPath;

    partial void OnIsDarkModeChanged(bool value)
    {
        Runia.Properties.Settings.Default.LastTheme = value ? "Dark" : "Light";
        Save();

        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
 
                var olds = dicts.Where(d => d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml"))).ToList();
                foreach (var o in olds) dicts.Remove(o);

                var theme = value ? "DarkTheme.xaml" : "LightTheme.xaml";
                var uri = new Uri($"/Runia;component/assets/Themes/{theme}", UriKind.Relative);
                var newDict = new System.Windows.ResourceDictionary { Source = uri };
                dicts.Add(newDict);
            });
        }
        catch { }
        OnPropertyChanged(nameof(ThemeModeText));
        OnPropertyChanged(nameof(ThemeHintText));
    }

        public string ThemeModeText => IsDarkMode ? "Темная тема" : "Светлая тема";
        public string ThemeHintText => IsDarkMode ? "Используется темная тема" : "Используется светлая тема";
        partial void OnShowConsoleChanged(bool value) { Runia.Properties.Settings.Default.ShowConsole = value; Save(); }
        partial void OnIsFullscreenChanged(bool value) { Runia.Properties.Settings.Default.IsFullscreen = value; Save(); }
        partial void OnAutoCloseChanged(bool value) { Runia.Properties.Settings.Default.AutoCloseLauncher = value; Save(); }
        partial void OnRamAmountChanged(int value) { Runia.Properties.Settings.Default.RamAmount = value; Save(); }
        partial void OnJvmArgsChanged(string value) { Runia.Properties.Settings.Default.JvmArguments = value; Save(); }

        [RelayCommand]
        private void BrowsePath()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите папку для файлов игры",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                GamePath = dialog.SelectedPath;
                Runia.Properties.Settings.Default.GameRootPath = GamePath;
                Save();
            }
        }

        private static void Save() => Runia.Properties.Settings.Default.Save();
    }
}