using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiManual.API.Models;

namespace AiManual.API.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public EmbeddingService(IConfiguration config)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API Key is missing.");
        }

        public async Task<float[]> GetEmbedding(string text)
        {
            var body = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.openai.com/v1/embeddings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Embedding API error: {response.StatusCode}\n{json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
        }

        // ✅ Called ONCE at startup — builds vector store using enriched text
        public async Task BuildVectorStore(DataService dataService)
        {
            var steps = dataService.GetAllSteps();
            var vectors = new List<VectorEntry>();

            foreach (var step in steps)
            {
                // Use the enriched text — NOT just description
                var embeddingText = dataService.BuildEmbeddingText(step);
                var embedding = await GetEmbedding(embeddingText);

                vectors.Add(new VectorEntry
                {
                    Step = step,
                    Embedding = embedding,
                    EmbeddingText = embeddingText  // store for debugging
                });

                Console.WriteLine($"[Embed] Step {step.StepNo}: {step.Heading}");
            }

            dataService.SetVectorData(vectors);
            Console.WriteLine($"[Embed] Done. {vectors.Count} steps indexed.");
        }
    }
}