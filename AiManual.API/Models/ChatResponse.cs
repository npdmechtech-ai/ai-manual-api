namespace AiManual.API.Models
{
    public class ChatResponse
    {
        public string Heading { get; set; } = string.Empty;

        public List<StepResponse> Steps { get; set; } = new();
    }
}