using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Domain.Models;
using Runia.Services.Core.Infrastructure.Network;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Runia.Services.Core.Infrastructure.Network.Clients;

namespace Runia.ViewModels.Main
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;
        private readonly NewsClient _newsClient;
        private readonly IImageDownloader _imageDownloader;

        [ObservableProperty]
        private ObservableCollection<NewsItem> newsItems = [];

        public HomeViewModel(
            IDialogService dialog,
            INavigationService nav,
            NewsClient newsClient,
            IImageDownloader imageDownloader
        )
        {
            _dialog = dialog;
            _nav = nav;
            _newsClient = newsClient;
            _imageDownloader = imageDownloader;

            Debug.WriteLine("[HomeViewModel] Constructor called");
            _ = InitDataAsync();
        }
        public static ICommand OpenUrlCommand => new RelayCommand<string>(url =>
        {
            if (!string.IsNullOrEmpty(url))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true 
                });
            }
        });

        private async Task InitDataAsync()
        {
            // Ensure the generated NewsItems property (backing field newsItems) is initialized
            newsItems ??= [];

            if (newsItems.Count > 0)
            {
                Debug.WriteLine("[HomeViewModel] InitDataAsync skipped: NewsItems already populated");
                return;
            }
            try
            {
                var result = await _newsClient.GetLatestNewsAsync(page: 1, pageSize: 10);

                if (!result.IsSuccess)
                {
                    Debug.WriteLine($"[HomeViewModel] Ошибка загрузки новостей: {result.ErrorMessage}");
                    _dialog?.ShowError(result.ErrorMessage ?? "Не удалось загрузить новости", "Ошибка");
                    return;
                }

                if (result.Data?.Items == null || !result.Data.Items.Any())
                {
                    Debug.WriteLine("[HomeViewModel] No news returned from API");
                    return;
                }

                newsItems.Clear();
                foreach (var item in result.Data.Items)
                {
                    if (item == null) continue;
                    newsItems.Add(item);
                }

                Debug.WriteLine($"[HomeViewModel] Successfully loaded {newsItems.Count} news items");

                // Start background downloads for images. Each item will receive LocalImagePath when available.
                foreach (var item in newsItems)
                {
                    if (item != null)
                    {
                        _ = LoadImageAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeViewModel] Ошибка загрузки: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ViewNewsDetails(NewsItem news)
        {
            if (news == null) return;
            _nav.NavigateTo<NewsDetailsViewModel>(news);
        }

        private async Task LoadImageAsync(NewsItem item)
        {
            try
            {
                var path = await _imageDownloader.GetLocalPathAsync(item.ImageUrl);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    // Set local path on model; UI layer will convert this path to ImageSource
                    item.LocalImagePath = path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeViewModel] Failed to load image for {item.Title}: {ex.Message}");
            }
        }
    }
}