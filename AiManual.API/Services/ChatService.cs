using AiManual.API.Models;

namespace AiManual.API.Services
{
    public class ChatService
    {
        private readonly DataService _dataService;
        private readonly AIService _aiService;
        private readonly EmbeddingService _embeddingService;

        private string? _cachedFullContext = null;

        public ChatService(
            DataService dataService,
            AIService aiService,
            EmbeddingService embeddingService)
        {
            _dataService = dataService;
            _aiService = aiService;
            _embeddingService = embeddingService;
        }

        public async Task<ChatResponse> GetAnswer(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Empty("Please enter a valid question.");

            var q = question.ToLower().Trim();

            // ── 1. OUT OF SCOPE ────────────────────────────────────────────────
            if (q.ContainsAny("engine oil", "fuel", "battery", "coolant"))
                return OutOfScope();

            // ── 2. TOOL LIST ───────────────────────────────────────────────────
            var isToolListQuestion =
                (q.ContainsAny("list", "all", "show", "what tools", "which tools") && q.Contains("tool")) ||
                q.Trim() == "tools";

            if (isToolListQuestion && !q.ContainsAny("needed", "required", "used for", "remove", "install", "fix", "replace"))
                return BuildAllToolsResponse();

            // ── 3. TOOL QUESTION ───────────────────────────────────────────────
            if (IsToolQuestion(q))
            {
                var tool = GetExactToolAnswer(q);
                if (tool != null)
                    return tool;
            }

            // ── 4. SCOPE CHECK ─────────────────────────────────────────────────
            var scope = await _aiService.ClassifyScope(question);
            if (scope == "OUT_OF_SCOPE")
                return OutOfScope();

            // ── 5. FULL DOCUMENT CONTEXT → AI ─────────────────────────────────
            var fullContext = GetFullManualContext();

            if (string.IsNullOrEmpty(fullContext))
                return Empty("Manual not loaded. Please restart the server.");

            // Ask AI to identify which step numbers are relevant
            // AI returns step numbers → we pull full structured data from DataService
            var relevantStepNumbers = await _aiService.GetRelevantStepNumbers(question, fullContext);

            var allSteps = _dataService.GetAllSteps();

            List<Step> matchedSteps;

            if (relevantStepNumbers.Count == 0)
            {
                // AI found no specific steps → general knowledge answer
                var fallback = await _aiService.GetGeneralAnswer(question);
                return new ChatResponse
                {
                    Heading = "🤖 General Knowledge",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse { FormattedText = fallback }
                    }
                };
            }
            else if (relevantStepNumbers.Contains(-1))
            {
                // -1 is the signal for "ALL steps" (full procedure request)
                matchedSteps = allSteps;
            }
            else
            {
                // Pick only the matched step numbers
                matchedSteps = allSteps
                    .Where(s => relevantStepNumbers.Contains(s.StepNo))
                    .OrderBy(s => s.StepNo)
                    .ToList();
            }

            // Build fully structured step responses — exactly like your JSON
            var stepResponses = matchedSteps.Select(s => new StepResponse
            {
                StepNo = s.StepNo,
                FormattedText = $"{s.Heading}: {s.Description}",
                Tools = s.Tools ?? new List<Tool>(),
                Images = s.Images ?? new List<string>(),
                Warning = s.Warning ?? "",
                Note = s.Note ?? ""
            }).ToList();

            return new ChatResponse
            {
                Heading = "📖 From Manual",
                Steps = stepResponses
            };
        }

        // ── Builds the full manual context string (cached) ─────────────────────
        private string GetFullManualContext()
        {
            if (_cachedFullContext != null)
                return _cachedFullContext;

            var steps = _dataService.GetAllSteps();
            if (!steps.Any()) return "";

            var sb = new System.Text.StringBuilder();
            foreach (var s in steps)
            {
                sb.AppendLine($"--- Step {s.StepNo}: {s.Heading} ---");
                sb.AppendLine($"Description: {s.Description}");

                if (s.Components != null && s.Components.Any())
                    sb.AppendLine($"Components: {string.Join(", ", s.Components)}");

                if (s.Tools != null && s.Tools.Any())
                    sb.AppendLine($"Tools: {string.Join(", ", s.Tools.Select(t => $"{t.Name} ({t.Size})"))}");

                if (!string.IsNullOrWhiteSpace(s.Warning))
                    sb.AppendLine($"Warning: {s.Warning}");

                if (!string.IsNullOrWhiteSpace(s.Note))
                    sb.AppendLine($"Note: {s.Note}");

                sb.AppendLine();
            }

            _cachedFullContext = sb.ToString();
            return _cachedFullContext;
        }

        // ── All methods below are YOUR ORIGINAL CODE — unchanged ──────────────

        private ChatResponse? GetExactToolAnswer(string q)
        {
            var steps = _dataService.GetAllSteps();
            string? tool = null;

            if (q.Contains("allen") || q.Contains("hex"))
                tool = "allen";
            else if (q.Contains("screwdriver"))
                tool = "screwdriver";
            else if (q.Contains("plier"))
                tool = "plier";
            else if (q.Contains("spanner") || q.Contains("wrench"))
                tool = "spanner";

            if (tool == null)
                return null;

            var tools = steps
                .SelectMany(s => s.Tools)
                .Where(t =>
                    !string.IsNullOrEmpty(t.Name) &&
                    t.Name.ToLower().Contains(tool))
                .GroupBy(t => $"{t.Name}|{t.Size}")
                .Select(g => g.First())
                .ToList();

            if (!tools.Any())
                return null;

            return new ChatResponse
            {
                Heading = "🛠 Tool Info",
                Steps = new List<StepResponse>
                {
                    new StepResponse
                    {
                        FormattedText = string.Join("\n",
                            tools.Select(t => $"• {t.Name} — {t.Size}")),
                        Tools  = tools,
                        Images = new List<string>()
                    }
                }
            };
        }

        private bool IsToolQuestion(string q)
        {
            return q.ContainsAny("allen", "hex", "tool", "size",
                                 "wrench", "spanner", "plier", "screwdriver");
        }

        private ChatResponse BuildAllToolsResponse()
        {
            var tools = _dataService.GetAllSteps()
                .SelectMany(s => s.Tools)
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .GroupBy(t => $"{t.Name}|{t.Size}")
                .Select(g => g.First())
                .ToList();

            return new ChatResponse
            {
                Heading = "🛠 All Tools",
                Steps = new List<StepResponse>
                {
                    new StepResponse
                    {
                        FormattedText = string.Join("\n",
                            tools.Select(t => $"• {t.Name} — {t.Size}"))
                    }
                }
            };
        }

        private ChatResponse OutOfScope()
        {
            return new ChatResponse
            {
                Heading = "❌ Out of Scope",
                Steps = new List<StepResponse>
                {
                    new StepResponse
                    {
                        FormattedText = "This question is not related to parking brake system."
                    }
                }
            };
        }

        private ChatResponse Empty(string msg)
        {
            return new ChatResponse
            {
                Heading = "ℹ️",
                Steps = new List<StepResponse>
                {
                    new StepResponse { FormattedText = msg }
                }
            };
        }
    }

    internal static class StringExtensions
    {
        public static bool ContainsAny(this string source, params string[] values)
        {
            return values.Any(v => source.Contains(v, StringComparison.OrdinalIgnoreCase));
        }
    }
}