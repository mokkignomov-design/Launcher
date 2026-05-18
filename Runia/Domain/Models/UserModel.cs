using System.Text.Json.Serialization;

namespace Runia.Domain.Models
{
    public class UserProfile
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "Player";

        [JsonPropertyName("skinUrl")]
        public string? SkinUrl { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("rank")]
        public string Rank { get; set; } = "Игрок";

        [JsonPropertyName("registrationDate")]
        public DateTime RegistrationDate { get; set; }

        [JsonPropertyName("stats")]
        public UserStats Stats { get; set; } = new();

        [JsonPropertyName("activities")]
        public List<UserActivity> Activities { get; set; } = [];
    }

    public class UserStats
    {
        [JsonPropertyName("donationBalance")]
        public decimal DonationBalance { get; set; }

        [JsonPropertyName("playedHours")]
        public double PlayedHours { get; set; }

        [JsonPropertyName("kills")]
        public int Kills { get; set; }

        [JsonPropertyName("deaths")]
        public int Deaths { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("server")]
        public string Server { get; set; } = string.Empty;

        [JsonPropertyName("friendsCount")]
        public int FriendsCount { get; set; }
    }

    public class UserActivity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}