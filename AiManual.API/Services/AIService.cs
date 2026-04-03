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
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API Key is missing.");
        }

        // ── Called when RAG found matching steps ────────────────────────────────
        // AI explains the manual content — does NOT add outside knowledge
        public async Task<string> GetManualAnswer(string question, string manualContext)
        {
            return await Send(
                system:
                    "You are a technical assistant for a Backhoe Loader parking brake replacement manual.\n" +
                    "You are given context directly from the service manual (step number, heading, description, tools, warnings).\n\n" +
                    "Rules:\n" +
                    "- Answer ONLY using the provided context. Do not add information not present.\n" +
                    "- If the context mentions a specific tool (e.g. 5mm Allen Key), always include it.\n" +
                    "- If the context has a warning, always include it in your answer.\n" +
                    "- Be concise and practical. Use simple language.\n" +
                    "- Do NOT start your answer with a label like '📖 From Manual:' — the UI adds that.",

                user:
                    $"Manual context:\n{manualContext}\n\n" +
                    $"Question: {question}"
            );
        }

        // ── Called when RAG found no match but topic is parking brake ───────────
        // AI uses general knowledge — but stays strictly on topic
        public async Task<string> GetGeneralAnswer(string question)
        {
            return await Send(
                system:
                    "You are a technical assistant for Backhoe Loader parking brake systems.\n" +
                    "The service manual does not contain a direct answer to this question.\n" +
                    "Answer from your general knowledge about parking brake systems on backhoe loaders.\n\n" +
                    "Rules:\n" +
                    "- Stay on topic: parking brake function, components, or general maintenance only.\n" +
                    "- Do NOT invent specific torque values or part numbers.\n" +
                    "- Do NOT mention cars or trucks — this is construction equipment.\n" +
                    "- Be concise. Do NOT start with a label prefix.",

                user: question
            );
        }

        // ── Called when RAG score is too low to determine topic ──────────────────
        // Returns a simple token: "IN_SCOPE" or "OUT_OF_SCOPE"
        // This replaces the fragile answer.Contains("out of scope") string check
        public async Task<string> ClassifyScope(string question)
        {
            var result = await Send(
                system:
                    "You are a topic classifier. Reply with ONLY one of these two words — nothing else:\n" +
                    "IN_SCOPE — if the question is about:\n" +
                    "  parking brake replacement, parking brake components, parking brake function,\n" +
                    "  brake cables, clevis pins, cotter pins, brake levers, brake assembly,\n" +
                    "  tools for brake replacement, or general parking brake knowledge\n\n" +
                    "OUT_OF_SCOPE — if the question is about:\n" +
                    "  engine, oil, transmission, hydraulics, tyres, electrical wiring (unrelated to brake),\n" +
                    "  fuel system, cooling system, or any unrelated topic",

                user: question
            );

            // Normalise — trim whitespace and check prefix only
            var normalised = result.Trim().ToUpper();
            return normalised.StartsWith("IN_SCOPE") ? "IN_SCOPE" : "OUT_OF_SCOPE";
        }

        // ── Single private sender ────────────────────────────────────────────────
        private async Task<string> Send(string system, string user)
        {
            var body = new
            {
                model = "gpt-4o-mini",
                temperature = 0.2,
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user",   content = user   }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"Error communicating with AI: {response.StatusCode}";

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }
    }
}