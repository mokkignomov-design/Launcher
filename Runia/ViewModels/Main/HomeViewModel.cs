using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Domain.Models;
using Runia.Services.Core.Infrastructure.Network;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Runia.ViewModels.Main
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;
        private readonly NewsService _newsService;
        private readonly IImageDownloader _imageDownloader;



        [ObservableProperty]
        private ObservableCollection<NewsItem> newsItems = [];
        public HomeViewModel(
            IDialogService dialog,
            INavigationService nav,
            NewsService newsService,
            IImageDownloader imageDownloader
        )
        {
            _dialog = dialog;
            _nav = nav;
            _newsService = newsService;
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
            if (NewsItems != null && NewsItems.Count > 0)
            {
                Debug.WriteLine("[HomeViewModel] InitDataAsync skipped: NewsItems already populated");
                return;
            }
            try
            {
                var news = await _newsService.GetLatestNewsAsync();

                if (news == null)
                {
                    Debug.WriteLine("[HomeViewModel] No news returned from service");
                    return;
                }

                NewsItems ??= [];

                NewsItems.Clear();
                foreach (var item in news)
                {
                    if (item == null) continue;
                    NewsItems.Add(item);
                }

                // Start background downloads for images. Each item will receive LocalImagePath when available.
                foreach (var item in NewsItems)
                {
                    if (item == null) continue;
                    _ = LoadImageAsync(item);
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