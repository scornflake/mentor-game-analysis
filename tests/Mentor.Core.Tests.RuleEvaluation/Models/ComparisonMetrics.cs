using Mentor.Core.Models;

namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Quantifiable metrics comparing analysis with and without rules
/// </summary>
public class ComparisonMetrics
{
    public string ScreenshotPath { get; set; } = string.Empty;
    
    // Full recommendation objects for detailed review
    public Recommendation? BaselineRecommendation { get; set; }
    public Recommendation? RuleAugmentedRecommendation { get; set; }
    
    // Subjective evaluation results
    public SubjectiveEvaluationResult? BaselineSubjectiveEvaluation { get; set; }
    public SubjectiveEvaluationResult? RuleAugmentedSubjectiveEvaluation { get; set; }
    
    // Baseline (no rules) metrics
    public double BaselineSpecificityScore { get; set; }
    public double BaselineTerminologyScore { get; set; }
    public double BaselineActionabilityScore { get; set; }
    public double BaselineConfidence { get; set; }
    public int BaselineRecommendationCount { get; set; }
    
    // Rule-augmented metrics
    public double RuleAugmentedSpecificityScore { get; set; }
    public double RuleAugmentedTerminologyScore { get; set; }
    public double RuleAugmentedActionabilityScore { get; set; }
    public double RuleAugmentedConfidence { get; set; }
    public int RuleAugmentedRecommendationCount { get; set; }
    
    // Deltas (improvement/degradation)
    public double SpecificityDelta => RuleAugmentedSpecificityScore - BaselineSpecificityScore;
    public double TerminologyDelta => RuleAugmentedTerminologyScore - BaselineTerminologyScore;
    public double ActionabilityDelta => RuleAugmentedActionabilityScore - BaselineActionabilityScore;
    public double ConfidenceDelta => RuleAugmentedConfidence - BaselineConfidence;
    public int RecommendationCountDelta => RuleAugmentedRecommendationCount - BaselineRecommendationCount;
    
    // Subjective evaluation scores
    public double? BaselineSubjectiveScore => BaselineSubjectiveEvaluation?.OverallScore;
    public double? RuleAugmentedSubjectiveScore => RuleAugmentedSubjectiveEvaluation?.OverallScore;
    public double? SubjectiveScoreDelta => 
        (RuleAugmentedSubjectiveScore.HasValue && BaselineSubjectiveScore.HasValue)
            ? RuleAugmentedSubjectiveScore.Value - BaselineSubjectiveScore.Value
            : null;
    
    // Overall improvement score (positive = rules helped, negative = rules hurt)
    public double OverallImprovementScore => 
        (SpecificityDelta + TerminologyDelta + ActionabilityDelta) / 3.0;
    
    // Timing
    public TimeSpan BaselineDuration { get; set; }
    public TimeSpan RuleAugmentedDuration { get; set; }
    public TimeSpan DurationDelta => RuleAugmentedDuration - BaselineDuration;
}

