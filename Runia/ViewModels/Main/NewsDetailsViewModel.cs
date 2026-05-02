using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Runia.Domain.Models;
using Runia.Services.Core.Navigation;

namespace Runia.ViewModels.Main
{
    public partial class NewsDetailsViewModel : ObservableObject
    {
        private readonly INavigationService _nav;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayImageUrl))] 
        private NewsItem _selectedNews = null!;

        public string DisplayImageUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SelectedNews?.LocalImagePath))
                    return SelectedNews!.LocalImagePath!;

                if (!string.IsNullOrWhiteSpace(SelectedNews?.ImageUrl))
                    return SelectedNews!.ImageUrl;

                return "pack://application:,,,/Runia;component/assets/Images/news_default.png";
            }
        }

        public NewsDetailsViewModel(INavigationService nav, NewsItem news)
        {
            _nav = nav;
            SelectedNews = news;
        }

        [RelayCommand]
        private void GoBack() => _nav.NavigateTo<HomeViewModel>();
    }
}