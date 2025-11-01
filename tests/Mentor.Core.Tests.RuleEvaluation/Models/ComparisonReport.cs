namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Aggregates multiple comparison metrics with summary statistics
/// </summary>
public class ComparisonReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ProviderName { get; set; } = string.Empty;
    public List<ComparisonMetrics> IndividualComparisons { get; set; } = new();
    
    // Aggregate statistics
    public double AverageSpecificityDelta => 
        IndividualComparisons.Count > 0 
            ? IndividualComparisons.Average(c => c.SpecificityDelta) 
            : 0;
    
    public double AverageTerminologyDelta => 
        IndividualComparisons.Count > 0 
            ? IndividualComparisons.Average(c => c.TerminologyDelta) 
            : 0;
    
    public double AverageActionabilityDelta => 
        IndividualComparisons.Count > 0 
            ? IndividualComparisons.Average(c => c.ActionabilityDelta) 
            : 0;
    
    public double AverageOverallImprovement => 
        IndividualComparisons.Count > 0 
            ? IndividualComparisons.Average(c => c.OverallImprovementScore) 
            : 0;
    
    public double MedianSpecificityDelta => 
        CalculateMedian(IndividualComparisons.Select(c => c.SpecificityDelta).ToList());
    
    public double MedianTerminologyDelta => 
        CalculateMedian(IndividualComparisons.Select(c => c.TerminologyDelta).ToList());
    
    public double MedianActionabilityDelta => 
        CalculateMedian(IndividualComparisons.Select(c => c.ActionabilityDelta).ToList());
    
    // Subjective evaluation aggregates (only from comparisons that have subjective scores)
    public double? AverageSubjectiveDelta
    {
        get
        {
            var deltas = IndividualComparisons
                .Where(c => c.SubjectiveScoreDelta.HasValue)
                .Select(c => c.SubjectiveScoreDelta!.Value)
                .ToList();
            return deltas.Count > 0 ? deltas.Average() : null;
        }
    }
    
    public double? MedianSubjectiveDelta
    {
        get
        {
            var deltas = IndividualComparisons
                .Where(c => c.SubjectiveScoreDelta.HasValue)
                .Select(c => c.SubjectiveScoreDelta!.Value)
                .ToList();
            return deltas.Count > 0 ? CalculateMedian(deltas) : null;
        }
    }
    
    public int ComparisonsWithSubjectiveEvaluation => 
        IndividualComparisons.Count(c => c.SubjectiveScoreDelta.HasValue);
    
    public int TotalComparisons => IndividualComparisons.Count;
    
    public int ComparisonsWithImprovement => 
        IndividualComparisons.Count(c => c.OverallImprovementScore > 0);
    
    public int ComparisonsWithDegradation => 
        IndividualComparisons.Count(c => c.OverallImprovementScore < 0);
    
    public int ComparisonsNeutral => 
        IndividualComparisons.Count(c => Math.Abs(c.OverallImprovementScore) < 0.01);
    
    private static double CalculateMedian(List<double> values)
    {
        if (values.Count == 0) return 0;
        
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        
        if (sorted.Count % 2 == 0)
        {
            return (sorted[mid - 1] + sorted[mid]) / 2.0;
        }
        else
        {
            return sorted[mid];
        }
    }
}

