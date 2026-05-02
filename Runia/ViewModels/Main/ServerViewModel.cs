using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Domain.Models;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Runia.ViewModels.Main
{
    public partial class ServerViewModel : ObservableObject
    {
        private readonly INavigationService navigationService;
        private readonly IDialogService dialogService;
        private readonly IAppConfig _config;

        public ServerViewModel(INavigationService nav, IDialogService dialog, IAppConfig config)
        {
            navigationService = nav;
            dialogService = dialog;
            _config = config;

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
                    // Directly use domain ServerPack as the item source for UI
                    Servers.Add(pack);
                }
                Debug.WriteLine($"[ServerViewModel] Loaded {Servers.Count} servers.");
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