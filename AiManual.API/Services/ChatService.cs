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
            var q = question.ToLower();

            // 🔥 1. PROCEDURE
            if (IsProcedureQuestion(q))
            {
                var steps = _dataService.GetAllSteps();

                return new ChatResponse
                {
                    Heading = "🔧 Parking Brake Replacement Procedure",
                    Steps = steps.OrderBy(s => s.StepNo)
                        .Select(s => new StepResponse
                        {
                            StepNo = s.StepNo,
                            FormattedText = $"Step {s.StepNo}: {s.Description}",
                            Tools = s.Tools,
                            Images = s.Images,
                            Warning = s.Warning,
                            Note = s.Note
                        }).ToList()
                };
            }

            // 🔥 2. ALL TOOLS
            if (IsAllToolsQuestion(q))
            {
                var tools = _dataService.GetAllSteps()
                    .SelectMany(s => s.Tools)
                    .GroupBy(t => t.Name + t.Size)
                    .Select(g => g.First())
                    .ToList();

                return new ChatResponse
                {
                    Heading = "🛠 All Tools Used",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse
                        {
                            FormattedText = string.Join("\n",
                                tools.Select(t => $"{t.Name} - {t.Size}")),
                            Tools = tools,
                            Images = new List<string>() // 🔥 NO images
                        }
                    }
                };
            }

            // 🔍 3. RAG SEARCH
            var enhancedQuery = $"{question} parking brake backhoe loader tools screws";

            var queryVector = await _embeddingService.GetEmbedding(enhancedQuery);
            var vectors = _dataService.GetVectorData();

            var results = vectors
                .Select(v => new
                {
                    v.Step,
                    Score = SimilarityHelper.CosineSimilarity(queryVector, v.Embedding)
                })
                .Where(x => x.Score > 0.35) // 🔥 tuned threshold
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => x.Step)
                .ToList();

            // ❌ 4. NO MATCH → fallback
            if (!results.Any())
            {
                return await HandleFallback(question);
            }

            // 🔥 5. SPECIFIC TOOL QUESTION
            if (IsSpecificToolQuestion(q))
            {
                var tools = results
                    .SelectMany(r => r.Tools)
                    .Where(t => q.Contains(t.Name.ToLower()))
                    .GroupBy(t => t.Name + t.Size)
                    .Select(g => g.First())
                    .ToList();

                // fallback → all tools from matched steps
                if (!tools.Any())
                {
                    tools = results
                        .SelectMany(r => r.Tools)
                        .GroupBy(t => t.Name + t.Size)
                        .Select(g => g.First())
                        .ToList();
                }

                return new ChatResponse
                {
                    Heading = "🛠 Tools & Sizes",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse
                        {
                            FormattedText = string.Join("\n",
                                tools.Select(t => $"{t.Name} - {t.Size}")),
                            Tools = tools,
                            Images = new List<string>() // 🔥 NO images
                        }
                    }
                };
            }

            // 🔥 6. NORMAL STEP RESPONSE
            var bestStep = results.First();

            var answer = await _aiService.GetHybridAnswer(
                question,
                bestStep.Description
            );

            bool isNotAvailable = answer.ToLower().Contains("not available");

            return new ChatResponse
            {
                Heading = "🔧 Answer",
                Steps = new List<StepResponse>
                {
                    new StepResponse
                    {
                        FormattedText = answer,

                        // 🔥 CRITICAL FIX
                        Tools = isNotAvailable ? new List<Tool>() : bestStep.Tools,
                        Images = isNotAvailable ? new List<string>() : bestStep.Images
                    }
                }
            };
        }

        // 🔥 FALLBACK HANDLER
        private async Task<ChatResponse> HandleFallback(string question)
        {
            var q = question.ToLower();

            // ❌ OUT OF SCOPE
            if (!q.Contains("brake"))
            {
                return new ChatResponse
                {
                    Heading = "❌ Out of Scope",
                    Steps = new List<StepResponse>
                    {
                        new StepResponse
                        {
                            FormattedText = "This question is not related to parking brake.",
                            Tools = new List<Tool>(),
                            Images = new List<string>()
                        }
                    }
                };
            }

            // 🤖 AI fallback
            var aiAnswer = await _aiService.GetAnswer(question);

            return new ChatResponse
            {
                Heading = "🤖 Answer",
                Steps = new List<StepResponse>
                {
                    new StepResponse
                    {
                        FormattedText = aiAnswer,
                        Tools = new List<Tool>(),
                        Images = new List<string>()
                    }
                }
            };
        }

        // 🔍 INTENT DETECTION

        private bool IsProcedureQuestion(string q)
        {
            return q.Contains("procedure") ||
                   q.Contains("steps") ||
                   q.Contains("replacement");
        }

        private bool IsAllToolsQuestion(string q)
        {
            return q.Contains("what tools") ||
                   q.Contains("list tools") ||
                   q.Contains("tools used");
        }

        private bool IsSpecificToolQuestion(string q)
        {
            return q.Contains("allen") ||
                   q.Contains("spanner") ||
                   q.Contains("tool") ||
                   q.Contains("size") ||
                   q.Contains("screw");
        }
    }
}