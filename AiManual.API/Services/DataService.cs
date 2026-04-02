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

        // 🔥 IMPORTANT: Initialize embeddings ONLY ONCE
        public async Task InitializeEmbeddings(EmbeddingService embeddingService)
        {
            if (_vectorData.Any())
                return; // already initialized

            Console.WriteLine("🔥 Generating embeddings...");

            foreach (var step in _manualData.Steps)
            {
                var text = $@"
{step.Heading}
{step.Description}
{string.Join(" ", step.Components)}
{string.Join(" ", step.Tools.Select(t => t.Name))}
";

                var embedding = await embeddingService.GetEmbedding(text);

                _vectorData.Add(new VectorData
                {
                    Step = step,
                    Embedding = embedding
                });
            }

            Console.WriteLine($"✅ Embeddings Ready: {_vectorData.Count}");
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