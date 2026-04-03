namespace AiManual.API.Models
{
    public class ChatResponse
    {
        public string Heading { get; set; } = "";
        public List<StepResponse> Steps { get; set; } = new List<StepResponse>();
    }
}