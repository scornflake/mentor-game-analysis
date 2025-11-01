using Mentor.Core.Models;

namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Wraps a Recommendation with metadata about how it was generated
/// </summary>
public class AnalysisResult
{
    public Recommendation Recommendation { get; set; } = new();
    public bool RulesEnabled { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ScreenshotPath { get; set; } = string.Empty;
}

