using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Navigation;
using Runia.Services.Core.Launcher;
using Runia.Services.Common;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Runia.ViewModels.Main
{
    public partial class ServerDetailsViewModel(LauncherService launcher, IDialogService dialog, INavigationService nav, ILogger<ServerDetailsViewModel> logger) : ObservableObject
    {
        private readonly LauncherService _launcher = launcher;
        private readonly IDialogService _dialog = dialog;
        private readonly INavigationService _nav = nav;
        private readonly ILogger<ServerDetailsViewModel> _logger = logger;

        [RelayCommand]
       
        private async Task LaunchAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ServerName))
            {
                _dialog.ShowError("Сервер не выбран");
                return;
            }

            try
            {
                // Достаем главную вью-модель из DI
                if (App.Services?.GetService(typeof(Runia.ViewModels.Windows.MainWindowViewModel)) is Runia.ViewModels.Windows.MainWindowViewModel mainVm)
                {
                    await mainVm.StartGameGlobalAsync(ServerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запуска {ServerName}", ServerName);
                _dialog.ShowError($"Ошибка: {ex.Message}");
            }
        }

        [ObservableProperty]
        private string serverName = string.Empty;

        [ObservableProperty]
        private string serverDescription = string.Empty;

        [ObservableProperty]
        private int playerCount;

        [ObservableProperty]
        private int maxPlayers;

        public void SetSelectedServer(Runia.Domain.Models.ServerPack si)
        {
            if (si == null) return;

            ServerName = si.Title;
            ServerDescription = si.ShortDesc;
            PlayerCount = si.Online;
            ImageUrl = si.ImageUrl ?? string.Empty;
            FullDescription = si.FullDesc ?? string.Empty;
            // Если сервер уже содержит список модов от API - используем его сразу,
            // иначе попытаемся подгрузить дополнительные детали из локального appsettings.json
            Mods.Clear();
            if (si.Mods != null && si.Mods.Count > 0)
            {
                foreach (var m in si.Mods) Mods.Add(m ?? string.Empty);
            }

            LoadDetailsForServer(si);
        }

        private string _imageUrl = string.Empty;
        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (SetProperty(ref _imageUrl, value))
                {
                    OnPropertyChanged(nameof(DisplayImageUrl));
                }
            }
        }

        public string DisplayImageUrl => string.IsNullOrWhiteSpace(ImageUrl)
            ? "pack://application:,,,/Runia;component/assets/Images/news_default.png"
            : ImageUrl;

        [ObservableProperty]
        private string fullDescription = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> mods = [];

        private void LoadDetailsForServer(Runia.Domain.Models.ServerPack si)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Services", "Temp", "appsettings.json");
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Temp", "appsettings.json");
                }

                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(text);
                // JSON from backend uses lowercase property names (e.g. "servers", "title", "mods").
                if (doc.RootElement.TryGetProperty("servers", out var serversEl))
                    {
                        foreach (var el in serversEl.EnumerateArray())
                        {
                            var title = el.GetProperty("title").GetString();
                            if (string.Equals(title, si.Title, StringComparison.OrdinalIgnoreCase) ||
                                (el.TryGetProperty("id", out var idEl) && idEl.GetString() == si.Id.ToString()))
                            {
                                if (el.TryGetProperty("fullDesc", out var full)) FullDescription = full.GetString() ?? string.Empty;
                                if (el.TryGetProperty("imageUrl", out var icon)) ImageUrl = icon.GetString() ?? string.Empty;
                                if (el.TryGetProperty("mods", out var modsEl))
                                {
                                    Mods = [];
                                    foreach (var m in modsEl.EnumerateArray()) Mods.Add(m.GetString() ?? string.Empty);
                                }
                                if (el.TryGetProperty("online", out var onlineEl)) PlayerCount = onlineEl.GetInt32();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load local server details");
            }
        }

        [ObservableProperty]
        private bool isLaunching;

        [ObservableProperty]
        private string progress = "ИГРАТЬ";



        public void SetServer(string? serverName)
        {
            ServerName = serverName ?? string.Empty;
        }

        [RelayCommand]
        private void Back()
        {
            if (App.Services?.GetService(typeof(Runia.ViewModels.Main.ServerViewModel)) is Runia.ViewModels.Main.ServerViewModel serverVm)
            {
                _nav?.NavigateTo(serverVm);
            }
        }
    }
}
