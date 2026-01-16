using System.Text.Json.Serialization;

namespace arsX.Sol_Explorer.Models
{
    public class SiteProfile
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("short")]
        public string? Short { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
