namespace AiManual.API.Models
{
    // Keep filename as VectorData.cs but class is VectorEntry
    public class VectorEntry
    {
        public Step Step { get; set; } = new Step();
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string EmbeddingText { get; set; } = "";
    }
}