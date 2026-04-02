namespace AiManual.API.Models
{
    public class VectorData
    {
        public ManualStep Step { get; set; } = new();
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}