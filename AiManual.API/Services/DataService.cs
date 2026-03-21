using System.Text.Json;
using AiManual.API.Models;

namespace AiManual.API.Services
{
    public class DataService
    {
        private readonly ManualData _manualData;
        private readonly List<VectorData> _vectorData = new();

        public DataService()
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "parking_brake_replacement_procedure.json"
            );

            if (!File.Exists(path))
                throw new Exception($"JSON file not found at: {path}");

            var json = File.ReadAllText(path);

            _manualData = JsonSerializer.Deserialize<ManualData>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new ManualData();
        }

        // 🔥 Generate embeddings for all steps
        public async Task InitializeEmbeddings(EmbeddingService embeddingService)
        {
            foreach (var step in _manualData.Steps)
            {
                var text = $"{step.Heading} {step.Description} {string.Join(" ", step.Components)}";

                var embedding = await embeddingService.GetEmbedding(text);

                _vectorData.Add(new VectorData
                {
                    Step = step,
                    Embedding = embedding
                });
            }
        }

        public List<VectorData> GetVectorData()
        {
            return _vectorData;
        }

        public List<ManualStep> GetAllSteps()
        {
            return _manualData.Steps;
        }
    }
}