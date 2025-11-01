namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// A criterion for subjective evaluation of recommendations
/// </summary>
public class EvaluationCriterion
{
    public string Criterion { get; set; } = string.Empty;
    public string Expectation { get; set; } = "positive"; // "positive" or "negative"
    public double Weight { get; set; } = 1.0;
}

