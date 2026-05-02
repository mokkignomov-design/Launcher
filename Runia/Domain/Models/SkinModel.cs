using System.Text.Json.Serialization;

namespace Runia.Domain.Models
{
    public class SkinData
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("skinUrl")]
        public string SkinUrl { get; set; } = string.Empty;

        [JsonPropertyName("isSlim")]
        public bool IsSlim { get; set; }
    }

    public class SkinCacheEntry
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("localPath")]
        public string LocalPath { get; set; } = string.Empty;

        [JsonPropertyName("cachedAt")]
        public DateTime CachedAt { get; set; }

        [JsonPropertyName("isSlim")]
        public bool IsSlim { get; set; }
    }

    public class LastSelectedSkin
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("localPath")]
        public string LocalPath { get; set; } = string.Empty;

        [JsonPropertyName("selectedAt")]
        public DateTime SelectedAt { get; set; } = DateTime.Now;
    }
}
