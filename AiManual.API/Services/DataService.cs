using AiManual.API.Models;
using System.Text.Json;

namespace AiManual.API.Services
{
    public class DataService
    {
        private List<Step> _steps = new List<Step>();
        private List<VectorEntry> _vectors = new List<VectorEntry>();

        public void LoadFromJson(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize using ManualData wrapper — matches your JSON root structure
            var manualData = JsonSerializer.Deserialize<ManualData>(json, options);

            _steps = manualData?.Steps ?? new List<Step>();

            Console.WriteLine($"[Data] Loaded {_steps.Count} steps.");
        }

        public List<Step> GetAllSteps() => _steps;

        public List<VectorEntry> GetVectorData() => _vectors;

        public void SetVectorData(List<VectorEntry> vectors) => _vectors = vectors;

        // Enriched embedding text — heading + description + components + tools + warning
        // This makes RAG scores much stronger than description alone
        public string BuildEmbeddingText(Step step)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(step.Heading))
                parts.Add(step.Heading);

            if (!string.IsNullOrEmpty(step.Description))
                parts.Add(step.Description);

            if (step.Components != null && step.Components.Any())
                parts.Add("Components: " + string.Join(", ", step.Components));

            if (step.Tools != null && step.Tools.Any())
                parts.Add("Tools: " + string.Join(", ",
                    step.Tools.Select(t => $"{t.Name} {t.Size}".Trim())));

            if (!string.IsNullOrEmpty(step.Warning))
                parts.Add("Warning: " + step.Warning);

            return string.Join(". ", parts);
        }
    }
}