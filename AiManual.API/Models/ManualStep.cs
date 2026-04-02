namespace AiManual.API.Models
{
    public class ManualStep
    {
        public int StepNo { get; set; }
        public string Heading { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<Tool> Tools { get; set; } = new();
        public List<string> Components { get; set; } = new();
        public List<string> Images { get; set; } = new();

        public string Warning { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class Tool
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }
}