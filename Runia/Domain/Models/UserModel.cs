using System.Text.Json.Serialization;

namespace Runia.Domain.Models
{
    public class UserProfile
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "Player";

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
        [JsonPropertyName("playedHours")]
        public double PlayedHours { get; set; }

        [JsonPropertyName("kills")]
        public int Kills { get; set; }

        [JsonPropertyName("deaths")]
        public int Deaths { get; set; }
    }

    public class UserActivity
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#55FF55";
    }
}