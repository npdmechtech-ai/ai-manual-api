using System.Text.Json.Serialization;

namespace AiManual.API.Models
{
    // Root wrapper that matches your JSON structure
    public class ManualData
    {
        [JsonPropertyName("component")]
        public string Component { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("steps")]
        public List<Step> Steps { get; set; } = new List<Step>();
    }

    // Tool class lives here since it belongs to the data model
    public class Tool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("size")]
        public string Size { get; set; } = "";

        [JsonPropertyName("image")]
        public string Image { get; set; } = "";
    }
}