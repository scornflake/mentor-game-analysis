namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Configuration for a rule evaluation run
/// </summary>
public class EvaluationConfig
{
    public string Provider { get; set; } = string.Empty;
    public string? Evaluator { get; set; } // Optional separate provider for subjective evaluation
    public List<ScreenshotConfig> Screenshots { get; set; } = new();
}

/// <summary>
/// Configuration for a single screenshot analysis
/// </summary>
public class ScreenshotConfig
{
    public string Path { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<EvaluationCriterion> EvaluationCriteria { get; set; } = new();
}

