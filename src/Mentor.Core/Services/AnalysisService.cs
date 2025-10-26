using Mentor.Core.Models;

namespace Mentor.Core.Services;

public class AnalysisService : IAnalysisService
{
    public Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation - returns hardcoded recommendation
        var recommendation = new Recommendation
        {
            Analysis = "This is a stub analysis of the provided screenshot.",
            Summary = "Stub recommendation summary",
            Recommendations =
            [
                new()
                {
                    Priority = Priority.High,
                    Action = "Focus on main objective",
                    Reasoning = "This is the most important task",
                    Context = "Based on current game state"
                },

                new()
                {
                    Priority = Priority.Medium,
                    Action = "Collect resources",
                    Reasoning = "Resources enable progression",
                    Context = "Resource nodes visible in screenshot"
                }
            ],
            Confidence = 0.95,
            GeneratedAt = DateTime.UtcNow,
            ProviderUsed = "Stub"
        };

        return Task.FromResult(recommendation);
    }
}

