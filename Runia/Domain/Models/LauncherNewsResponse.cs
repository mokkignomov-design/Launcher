using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Runia.Domain.Models
{
    public class LauncherNewsResponse
    {
        [JsonPropertyName("news")]
        public List<NewsItem> News { get; set; } = [];
    }
}
