namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Result of evaluating a single criterion
/// </summary>
public class CriterionEvaluation
{
    public int CriterionIndex { get; set; }
    public string Criterion { get; set; } = string.Empty;
    public double Score { get; set; } // 0-10
    public string Reasoning { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
}

