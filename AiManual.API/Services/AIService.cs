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

        // ── ADDED: Returns step numbers relevant to the question ───────────────
        // Returns [-1] if ALL steps needed (full procedure)
        // Returns []   if no steps found (general knowledge needed)
        // Returns [9, 10, 13 ...] for specific step questions
        public async Task<List<int>> GetRelevantStepNumbers(string question, string fullManualContext)
        {
            var result = await Send(
                system:
                    "You are a step-number extractor for a service manual.\n" +
                    "Read the full manual and the user question.\n\n" +
                    "Reply with ONLY one of these formats — nothing else:\n" +
                    "- If the user wants ALL steps or full procedure: reply exactly → ALL\n" +
                    "- If specific steps are relevant: reply with comma-separated step numbers → 9,10,13\n" +
                    "- If no steps are relevant (general knowledge question): reply exactly → NONE\n\n" +
                    "Examples:\n" +
                    "Q: 'full procedure' → ALL\n" +
                    "Q: 'how to remove cotter pin' → 9,10\n" +
                    "Q: 'how to install parking brake assembly' → 27\n" +
                    "Q: 'why does parking brake fail' → NONE",

                user:
                    $"MANUAL:\n{fullManualContext}\n\n" +
                    $"Question: {question}"
            );

            var trimmed = result.Trim().ToUpper();

            if (trimmed == "ALL")
                return new List<int> { -1 };  // -1 = signal for all steps

            if (trimmed == "NONE" || string.IsNullOrWhiteSpace(trimmed))
                return new List<int>();

            // Parse comma-separated step numbers
            var numbers = new List<int>();
            foreach (var part in trimmed.Split(','))
            {
                if (int.TryParse(part.Trim(), out int num))
                    numbers.Add(num);
            }
            return numbers;
        }

        // ── ADDED: Full document context answer (your original from last update) 
        public async Task<string> GetAnswerFromFullContext(string question, string fullManualContext)
        {
            return await Send(
                system:
                    "You are a technical assistant for a Backhoe Loader parking brake replacement manual.\n" +
                    "You have been given the COMPLETE service manual — all steps, tools, components, warnings.\n\n" +
                    "Rules:\n" +
                    "- Answer ONLY using the manual content provided.\n" +
                    "- Always include step number and heading in your answer.\n" +
                    "- Always mention tools required and any warnings.\n" +
                    "- Be clear, structured, and practical.\n" +
                    "- Do NOT start your answer with a label like '📖 From Manual:' — the UI adds that.",

                user:
                    $"COMPLETE MANUAL:\n{fullManualContext}\n\n" +
                    $"Question: {question}"
            );
        }

        // ── Your original methods — all unchanged ─────────────────────────────

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

            var normalised = result.Trim().ToUpper();
            return normalised.StartsWith("IN_SCOPE") ? "IN_SCOPE" : "OUT_OF_SCOPE";
        }

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