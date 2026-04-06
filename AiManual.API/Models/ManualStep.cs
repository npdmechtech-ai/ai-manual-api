using System.Text.Json.Serialization;

namespace AiManual.API.Models
{
    // Your existing file is ManualStep.cs — we keep the filename
    // but add an alias so all service code works with "Step"
    public class Step
    {
        [JsonPropertyName("stepNo")]
        public int StepNo { get; set; }

        [JsonPropertyName("heading")]
        public string Heading { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("tools")]
        public List<Tool> Tools { get; set; } = new List<Tool>();

        [JsonPropertyName("components")]
        public List<string> Components { get; set; } = new List<string>();

        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = new List<string>();

        [JsonPropertyName("warning")]
        public string Warning { get; set; } = "";

        [JsonPropertyName("note")]
        public string Note { get; set; } = "";
    }
}