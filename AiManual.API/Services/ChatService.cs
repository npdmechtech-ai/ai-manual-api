using AiManual.API.Models;

namespace AiManual.API.Services
{
    public class ChatService
    {
        private readonly DataService _dataService;
        private readonly AIService _aiService;

        public ChatService(DataService dataService, AIService aiService)
        {
            _dataService = dataService;
            _aiService = aiService;
        }

        public async Task<ChatResponse> GetAnswer(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Empty("Please enter a valid question.");

            var q = question.ToLower().Trim();

            // ── 1. OUT OF SCOPE ─────────────────────────────
            if (q.ContainsAny("engine oil", "fuel", "battery", "coolant"))
                return OutOfScope();

            // ── 2. TOOL LIST (FORCE) ───────────────────────
            if (q.Contains("tools"))
                return BuildAllToolsResponse();

            // ── 3. TOOL QUESTION (STRICT FILTER) ───────────
            if (IsToolQuestion(q))
            {
                var tool = GetExactToolAnswer(q);
                if (tool != null)
                    return tool;
            }

            // ── 4. CONCEPT QUESTION ────────────────────────
            if (q.ContainsAny("function", "what is", "purpose"))
            {
                var answer = await _aiService.GetGeneralAnswer(question);

                return new ChatResponse
                {
                    Heading = "🤖 General Knowledge",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse { FormattedText = answer }
                    }
                };
            }

            // ── 5. STEP RETRIEVAL (FIXED) ──────────────────
            var steps = _dataService.GetAllSteps();

            var keywords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var matchedSteps = steps
                .Select(s => new
                {
                    Step = s,
                    Score = keywords.Count(k =>
                        s.Description.ToLower().Contains(k) ||
                        s.Heading.ToLower().Contains(k))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Step)
                .ToList();

            if (!matchedSteps.Any())
            {
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

            var context = string.Join("\n\n", matchedSteps.Select(s =>
                $"Step {s.StepNo}: {s.Description}"
            ));

            var answerText = await _aiService.GetManualAnswer(question, context);
            var best = matchedSteps.First();

            return new ChatResponse
            {
                Heading = "📖 From Manual",
                Steps = new List<StepResponse>
                {
                    new StepResponse
                    {
                        FormattedText = answerText,
                        Tools = best.Tools ?? new List<Tool>(),
                        Images = best.Images ?? new List<string>()
                    }
                }
            };
        }

        // ───────────── TOOL FILTER (STRICT) ─────────────

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
                        Tools = tools,
                        Images = new List<string>() // NO extra images
                    }
                }
            };
        }

        private bool IsToolQuestion(string q)
        {
            return q.ContainsAny("allen", "hex", "tool", "size",
                                 "wrench", "spanner", "plier", "screwdriver");
        }

        // ───────────── TOOL LIST ─────────────

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

        // ───────────── HELPERS ─────────────

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