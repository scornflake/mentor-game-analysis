namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// LLM response for subjective evaluation
/// </summary>
public class SubjectiveEvaluationLLMResponse
{
    public List<SubjectiveCriterionScore> Evaluations { get; set; } = new();
}

/// <summary>
/// Individual criterion score from LLM
/// </summary>
public class SubjectiveCriterionScore
{
    public int CriterionIndex { get; set; }
    public double Score { get; set; }
    public string? Reasoning { get; set; }
}

