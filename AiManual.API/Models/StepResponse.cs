namespace AiManual.API.Models
{
    public class StepResponse
    {
        public int StepNo { get; set; }
        public string FormattedText { get; set; } = "";
        public List<Tool> Tools { get; set; } = new List<Tool>();
        public List<string> Images { get; set; } = new List<string>();
        public string Warning { get; set; } = "";
        public string Note { get; set; } = "";
    }
}