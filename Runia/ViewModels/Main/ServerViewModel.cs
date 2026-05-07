using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Domain.Models;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Navigation;
using Runia.Services.Core.Launcher; // Добавил для лаунчера
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Runia.ViewModels.Main
{
    public partial class ServerViewModel : ObservableObject
    {
        private readonly INavigationService navigationService;
        private readonly IDialogService dialogService;
        private readonly IAppConfig _config;
        private readonly LauncherService _launcher; // Добавляем сервис запуска

        public ServerViewModel(INavigationService nav, IDialogService dialog, IAppConfig config, LauncherService launcher)
        {
            navigationService = nav;
            dialogService = dialog;
            _config = config;
            _launcher = launcher; // Инициализируем

            Debug.WriteLine("[ServerViewModel] Constructor called");
            LoadServers();
        }

        private ObservableCollection<Runia.Domain.Models.ServerPack> _servers = [];
        public ObservableCollection<Runia.Domain.Models.ServerPack> Servers
        {
            get => _servers;
            set => SetProperty(ref _servers, value);
        }

        [ObservableProperty]
        private bool isLoading;

        // Команда для запуска игры ПРЯМО из списка
        [RelayCommand]
        private async Task JoinServer(ServerPack server)
        {
            if (server == null) return;

            try
            {
                // Достаем главную вью-модель из DI контейнера
                if (App.Services?.GetService(typeof(Runia.ViewModels.Windows.MainWindowViewModel)) is Runia.ViewModels.Windows.MainWindowViewModel mainVm)
                {
                    await mainVm.StartGameGlobalAsync(server.Title);
                }
            }
            catch (Exception ex)
            {
                dialogService?.ShowError($"Не удалось запустить {server.Title}: {ex.Message}", "Ошибка запуска");
            }
        }

        private async void LoadServers()
        {
            Debug.WriteLine("[ServerViewModel] Loading servers from config...");
            IsLoading = true;
            try
            {
                var packs = await _config.GetServersAsync();
                Servers.Clear();
                foreach (var pack in packs)
                {
                    Servers.Add(pack);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ServerViewModel] Error loading servers: {ex}");
                dialogService?.ShowError("Не удалось загрузить список серверов.", "Ошибка");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void SelectServer(Runia.Domain.Models.ServerPack server)
        {
            if (server == null) return;

            if (App.Services?.GetService(typeof(ServerDetailsViewModel)) is ServerDetailsViewModel detailsVm)
            {
                detailsVm.SetSelectedServer(server);
                navigationService?.NavigateTo(detailsVm);
            }
        }
    }
}