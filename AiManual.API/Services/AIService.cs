using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiManual.API.Services
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AIService(IConfiguration config)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            }; 

            _apiKey = config["OpenAI:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(_apiKey))
                throw new Exception("OpenAI API Key is missing.");
        }

        private async Task<string> SendRequest(object requestBody)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"❌ AI Error: {response.StatusCode}\n{json}";

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        public async Task<string> GetAnswer(string question)
        {
            var requestBody = new
            {
                model = "gpt-4.1-mini",
                temperature = 0.3,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"You are a Backhoe Loader Parking Brake expert.

Understand user intent even if wording is different.

Answer about:
- parking brake function
- tools
- sizes (allen key, screws)
- troubleshooting
- location

Do NOT mention cars or trucks."
                    },
                    new
                    {
                        role = "user",
                        content = question
                    }
                }
            };

            return await SendRequest(requestBody);
        }

        public async Task<string> GetHybridAnswer(string question, string context)
        {
            var requestBody = new
            {
                model = "gpt-4.1-mini",
                temperature = 0.2,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"Use the given context to answer.

- Extract correct info
- Map user wording correctly
- Do NOT hallucinate
- If not found → say 'Not available in manual'"
                    },
                    new
                    {
                        role = "user",
                        content = $@"
Context:
{context}

Question:
{question}
"
                    }
                }
            };

            return await SendRequest(requestBody);
        }
    }
}