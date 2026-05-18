
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Runia.Domain.Models
{
    public class ServerPack : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _title = string.Empty;
        private string _mcVersion = "1.20.1";
        private string _forgeVersion = string.Empty;
        private string _zipUrl = string.Empty;
        private string _imageUrl = string.Empty;
        private string _modsHash = string.Empty;
        private string _shortDesc = string.Empty;
        private string _fullDesc = string.Empty;
        private int _online;
        private List<string> _mods = [];

        [JsonPropertyName("id")]
        public string Id { get => _id; set { if (_id == value) return; _id = value; OnPropertyChanged(nameof(Id)); } }

        [JsonPropertyName("title")]
        public string Title { get => _title; set { if (_title == value) return; _title = value; OnPropertyChanged(nameof(Title)); } }

        [JsonPropertyName("mcVersion")]
        public string McVersion { get => _mcVersion; set { if (_mcVersion == value) return; _mcVersion = value; OnPropertyChanged(nameof(McVersion)); } }

        [JsonPropertyName("forgeVersion")]
        public string ForgeVersion { get => _forgeVersion; set { if (_forgeVersion == value) return; _forgeVersion = value; OnPropertyChanged(nameof(ForgeVersion)); } }

        [JsonPropertyName("zipUrl")]
        public string ZipUrl { get => _zipUrl; set { if (_zipUrl == value) return; _zipUrl = value; OnPropertyChanged(nameof(ZipUrl)); } }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (_imageUrl == value) return;
                _imageUrl = value ?? string.Empty;
                OnPropertyChanged(nameof(ImageUrl));
                OnPropertyChanged(nameof(DisplayImageUrl));
            }
        }

        [JsonPropertyName("modsHash")]
        public string ModsHash { get => _modsHash; set { if (_modsHash == value) return; _modsHash = value ?? string.Empty; OnPropertyChanged(nameof(ModsHash)); } }

        [JsonPropertyName("shortDesc")]
        public string ShortDesc { get => _shortDesc; set { if (_shortDesc == value) return; _shortDesc = value; OnPropertyChanged(nameof(ShortDesc)); } }

        [JsonPropertyName("fullDesc")]
        public string FullDesc { get => _fullDesc; set { if (_fullDesc == value) return; _fullDesc = value; OnPropertyChanged(nameof(FullDesc)); } }

        [JsonPropertyName("online")]
        public int Online { get => _online; set { if (_online == value) return; _online = value; OnPropertyChanged(nameof(Online)); } }

        [JsonPropertyName("mods")]
        public List<string> Mods { get => _mods; set { if (_mods == value) return; _mods = value ?? []; OnPropertyChanged(nameof(Mods)); } }

        public string Version
        {
            get => McVersion;
            set => McVersion = value;
        }

        [JsonIgnore]
        public string DisplayImageUrl => string.IsNullOrWhiteSpace(ImageUrl)
            ? "pack://application:,,,/Runia;component/assets/Images/news_default.png"
            : ImageUrl;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
