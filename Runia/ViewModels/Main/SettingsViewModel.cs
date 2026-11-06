using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Services.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace Runia.ViewModels.Main
{
    /// <summary>
    /// SettingsViewModel управляет настройками приложения и проверкой обновлений.
    /// </summary>
    public partial class SettingsViewModel(IDialogService dialog) : ObservableObject
    {
        private readonly IDialogService _dialog = dialog;
        private static readonly string UpdaterPath = Path.Combine(AppContext.BaseDirectory, "Updater.exe");
        private const string GitHubApiUrl = "https://api.github.com/repos/mokkignomov-design/Launcher/releases/latest";

        [ObservableProperty] private bool isDarkMode = Runia.Properties.Settings.Default.LastTheme == "Dark";
        [ObservableProperty] private bool showConsole = Runia.Properties.Settings.Default.ShowConsole;
        [ObservableProperty] private bool isFullscreen = Runia.Properties.Settings.Default.IsFullscreen;
        [ObservableProperty] private bool autoClose = Runia.Properties.Settings.Default.AutoCloseLauncher;
        [ObservableProperty] private int ramAmount = Runia.Properties.Settings.Default.RamAmount;
        [ObservableProperty] private string jvmArgs = Runia.Properties.Settings.Default.JvmArguments;
        [ObservableProperty] private string gamePath = Runia.Properties.Settings.Default.GameRootPath;

        // Версия и обновления
        [ObservableProperty] private string currentVersion = GetVersion();
        [ObservableProperty] private string latestVersion = "Проверка...";
        [ObservableProperty] private bool isCheckingUpdates = false;
        [ObservableProperty] private bool updateAvailable = false;
        [ObservableProperty] private bool autoUpdate = Runia.Properties.Settings.Default.AutoUpdate;

        public SettingsViewModel() : this(null!) { }

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

        partial void OnShowConsoleChanged(bool value) { Runia.Properties.Settings.Default.ShowConsole = value; Save(); }
        partial void OnIsFullscreenChanged(bool value) { Runia.Properties.Settings.Default.IsFullscreen = value; Save(); }
        partial void OnAutoCloseChanged(bool value) { Runia.Properties.Settings.Default.AutoCloseLauncher = value; Save(); }
        partial void OnRamAmountChanged(int value) { Runia.Properties.Settings.Default.RamAmount = value; Save(); }
        partial void OnJvmArgsChanged(string value) { Runia.Properties.Settings.Default.JvmArguments = value; Save(); }
        partial void OnAutoUpdateChanged(bool value)
        {
            Runia.Properties.Settings.Default.AutoUpdate = value;
            Save();
        }

        public string ThemeModeText => IsDarkMode ? "Тёмная тема" : "Светлая тема";
        public string ThemeHintText => IsDarkMode ? "Используется тёмная тема" : "Используется светлая тема";

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

        /// <summary>
        /// Проверяет обновления на GitHub
        /// </summary>
        [RelayCommand]
        private async Task CheckForUpdates()
        {
            IsCheckingUpdates = true;
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Runia-Launcher");

                var response = await httpClient.GetAsync(GitHubApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    LatestVersion = "Ошибка проверки";
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString();
                var version = tagName?.TrimStart('v') ?? "неизвестно";

                LatestVersion = version;
                UpdateAvailable = CompareVersions(CurrentVersion, version) < 0;
            }
            catch (Exception ex)
            {
                LatestVersion = "Ошибка: " + ex.Message;
            }
            finally
            {
                IsCheckingUpdates = false;
            }
        }

        /// <summary>
        /// Запускает апдейтер для обновления лаунчера
        /// </summary>
        [RelayCommand]
        private void UpdateLauncher()
        {
            if (!File.Exists(UpdaterPath))
            {
                MessageBox.Show("Updater.exe не найден!", "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = UpdaterPath,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    Arguments = "apply"
                };

                Process.Start(processInfo);

                // Даём время на запуск апдейтера
                Task.Delay(1000).ContinueWith(_ =>
                {
                    System.Windows.Application.Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске обновления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString(3) ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        private static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.').Select(p => int.TryParse(p, out var result) ? result : 0).ToArray();
            var parts2 = v2.Split('.').Select(p => int.TryParse(p, out var result) ? result : 0).ToArray();

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                int p1 = i < parts1.Length ? parts1[i] : 0;
                int p2 = i < parts2.Length ? parts2[i] : 0;

                if (p1 < p2) return -1;
                if (p1 > p2) return 1;
            }

            return 0;
        }

        private static void Save() => Runia.Properties.Settings.Default.Save();
    }
}
