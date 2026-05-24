using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Domain.Models;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Navigation;
using Runia.Services.Core.Launcher;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Runia.Services.Core.Infrastructure.Network.Clients;

namespace Runia.ViewModels.Main
{
    public partial class ServerViewModel : ObservableObject
    {
        private readonly INavigationService navigationService;
        private readonly IDialogService dialogService;
        private readonly LauncherService _launcher;
        private readonly ServerClient _serverClient;

        // Для отмены предыдущих запросов
        private CancellationTokenSource? _loadCts;

        public ServerViewModel(INavigationService nav, IDialogService dialog, LauncherService launcher, ServerClient serverClient)
        {
            navigationService = nav;
            dialogService = dialog;
            _launcher = launcher;
            _serverClient = serverClient;

            Debug.WriteLine("[ServerViewModel] Constructor called");
            LoadServersAsync();
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

        private async void LoadServersAsync()
        {
            // Отменяем предыдущий запрос если он еще идет
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();

            Debug.WriteLine("[ServerViewModel] Loading servers from API...");
            IsLoading = true;
            try
            {
                var result = await _serverClient.GetServersAsync(_loadCts.Token);

                if (!result.IsSuccess)
                {
                    Debug.WriteLine($"[ServerViewModel] Error loading servers: {result.ErrorMessage}");
                    dialogService?.ShowError(result.ErrorMessage ?? "Не удалось загрузить список серверов.", "Ошибка");
                    return;
                }

                Servers.Clear();
                foreach (var pack in result.Data ?? [])
                {
                    Servers.Add(pack);
                }

                Debug.WriteLine($"[ServerViewModel] Successfully loaded {Servers.Count} servers");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[ServerViewModel] Load cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ServerViewModel] Unexpected error loading servers: {ex}");
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