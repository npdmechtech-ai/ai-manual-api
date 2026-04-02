namespace AiManual.API.Models
{
    public class ManualData
    {
        public string Component { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public List<ManualStep> Steps { get; set; } = new();
    }
}