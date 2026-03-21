using AiManual.API.Models;

namespace AiManual.API.Services
{
    public class ChatService
    {
        private readonly DataService _dataService;
        private readonly EmbeddingService _embeddingService;
        private readonly AIService _aiService;

        public ChatService(
            DataService dataService,
            EmbeddingService embeddingService,
            AIService aiService)
        {
            _dataService = dataService;
            _embeddingService = embeddingService;
            _aiService = aiService;
        }

        public async Task<ChatResponse> GetAnswer(string question)
        {
            var steps = _dataService.GetAllSteps();
            var q = question.ToLower();

            // 🔒 DOMAIN CHECK
            if (!IsVehicleDomain(q))
            {
                return new ChatResponse
                {
                    Heading = "Out of Scope",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse
                        {
                            FormattedText = "❌ This assistant supports only vehicle service queries."
                        }
                    }
                };
            }

            // 🔥 FULL PROCEDURE
            if (q.Contains("procedure") || q.Contains("replacement") || q.Contains("how"))
            {
                return new ChatResponse
                {
                    Heading = "🔧 Parking Brake Replacement Procedure",
                    Steps = steps
                        .OrderBy(s => s.StepNo)
                        .Select(s => MapStep(s))
                        .ToList()
                };
            }

            // 🔧 TOOL QUERY
            if (q.Contains("tool"))
            {
                var tools = steps
                    .SelectMany(s => s.Tools)
                    .GroupBy(t => t.Name)
                    .Select(g => g.First())
                    .ToList();

                return new ChatResponse
                {
                    Heading = "🛠 Tools Required",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse
                        {
                            FormattedText = "🛠 Tools: " +
                                string.Join(", ", tools.Select(t => $"{t.Name} ({t.Size})")),
                            Tools = tools
                        }
                    }
                };
            }

            // 🔍 SEMANTIC SEARCH
            var queryVector = await _embeddingService.GetEmbedding(question);
            var vectors = _dataService.GetVectorData();

            var results = vectors
                .Select(v => new
                {
                    Step = v.Step,
                    Score = SimilarityHelper.CosineSimilarity(queryVector, v.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => x.Step)
                .ToList();

            // 🤖 AI FALLBACK
            if (!results.Any())
            {
                var aiAnswer = await _aiService.GetAnswer(question);

                return new ChatResponse
                {
                    Heading = "🤖 AI Explanation",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse
                        {
                            FormattedText = aiAnswer
                        }
                    }
                };
            }

            return new ChatResponse
            {
                Heading = "🔍 Best Match",
                Steps = results.Select(s => MapStep(s)).ToList()
            };
        }

        // 🔥 STEP MAPPER
        private StepResponse MapStep(ManualStep s)
        {
            return new StepResponse
            {
                StepNo = s.StepNo,
                Tools = s.Tools,
                Images = s.Images,
                Warning = s.Warning,
                Note = s.Note,
                FormattedText = FormatStep(s)
            };
        }

        // 🔥 STEP FORMATTER (UI READY)
        private string FormatStep(ManualStep step)
        {
            var text = $"🔹 Step {step.StepNo}: {step.Description}\n";

            if (step.Tools != null && step.Tools.Any())
            {
                text += "🛠 Tools: " +
                    string.Join(", ", step.Tools.Select(t => $"{t.Name} ({t.Size})")) + "\n";
            }

            if (!string.IsNullOrEmpty(step.Warning))
            {
                text += $"⚠ Warning: {step.Warning}\n";
            }

            if (!string.IsNullOrEmpty(step.Note))
            {
                text += $"ℹ Note: {step.Note}\n";
            }

            return text;
        }

        // 🔒 DOMAIN FILTER
        private bool IsVehicleDomain(string q)
        {
            return q.Contains("brake") ||
                   q.Contains("parking") ||
                   q.Contains("axle") ||
                   q.Contains("vehicle") ||
                   q.Contains("pedal") ||
                   q.Contains("spanner") ||
                   q.Contains("bolt") ||
                   q.Contains("torque");
        }
    }
}