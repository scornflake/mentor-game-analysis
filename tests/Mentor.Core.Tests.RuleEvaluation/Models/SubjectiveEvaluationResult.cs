namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Complete result of subjective evaluation
/// </summary>
public class SubjectiveEvaluationResult
{
    public List<CriterionEvaluation> Evaluations { get; set; } = new();
    public double OverallScore { get; set; } // 0-10, weighted average
}

