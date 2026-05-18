using System.Text.Json.Serialization;

namespace Runia.Domain.Models
{
    public class SessionResponse
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("temporaryKey")]
        public string TemporaryKey { get; set; } = string.Empty;

        [JsonPropertyName("expiresInSeconds")]
        public int ExpiresInSeconds { get; set; }
    }
}
