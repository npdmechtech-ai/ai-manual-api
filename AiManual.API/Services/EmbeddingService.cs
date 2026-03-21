using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiManual.API.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public EmbeddingService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _apiKey = config["OpenAI:ApiKey"];
        }

        public async Task<float[]> GetEmbedding(string text)
        {
            var requestBody = new
            {
                input = text,
                model = "text-embedding-3-small"
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/embeddings"
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            // 🔥 Safe check
            if (!doc.RootElement.TryGetProperty("data", out var data))
            {
                return new float[1536]; // fallback to avoid crash
            }

            var embedding = data[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return embedding;
        }
    }
}