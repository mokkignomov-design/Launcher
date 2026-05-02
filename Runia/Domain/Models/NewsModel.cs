using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System;

namespace Runia.Domain.Models
{
    public partial class NewsItem : ObservableObject
    {
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("shortDesc")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("date")] public DateTimeOffset Date { get; set; }
        [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = string.Empty;
        [JsonPropertyName("fullContent")] public string FullContent { get; set; } = string.Empty;
        [ObservableProperty]
        [JsonIgnore]
        private string? _localImagePath;

        [JsonIgnore]
        public string DisplayImageUrl =>
            !string.IsNullOrWhiteSpace(LocalImagePath) ? LocalImagePath! :
            (!string.IsNullOrWhiteSpace(ImageUrl) ? ImageUrl : "pack://application:,,,/Runia;component/assets/Images/news_default.png");
    }
}
