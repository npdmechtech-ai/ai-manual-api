using AiManual.API.Models;

public class StepResponse
{
    public int StepNo { get; set; }

    public string FormattedText { get; set; } = string.Empty;

    public List<Tool> Tools { get; set; } = new();
    public List<string> Images { get; set; } = new();

    public string Warning { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}