using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace AiManual.API.Services
{
    public class InspectService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public InspectService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["OpenAI:ApiKey"];
        }

        public async Task<string> AnalyzeImage(IFormFile image)
        {
            // Convert image → base64
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            var prompt = @"
You are an expert technician.

Analyze this image and identify:
1. What object/device is shown
2. What problem or missing part exists
3. What steps are required to fix it

Respond ONLY in JSON format:

{
  ""heading"": """",
  ""problem"": """",
  ""steps"": [
    { ""stepNo"": 1, ""text"": """" }
  ],
  ""warning"": """",
  ""note"": """"
}
";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/jpeg;base64,{base64}"
                                }
                            }
                        }
                    }
                }
            };

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody),
                Encoding.UTF8, "application/json"));

            var result = await response.Content.ReadAsStringAsync();

            // 🔥 PARSE CLEAN RESPONSE
            using var doc = JsonDocument.Parse(result);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Remove markdown/json wrappers
            content = content.Replace("```json", "")
                             .Replace("```", "")
                             .Replace("json", "")
                             .Trim();

            return content;
        }
    }
}