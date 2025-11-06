namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Report model for data quality analysis results.
/// </summary>
public class DataQualityReport
{
    public int TotalEntities { get; set; }
    public int EntitiesWithConcepts { get; set; }
    public int TotalUniqueConceptsExtracted { get; set; }
    
    public int AccurateMatches { get; set; } // Exact match or high confidence (>= 0.8)
    public int LikelyMatches { get; set; } // Medium confidence (0.5-0.8)
    public int UncertainMatches { get; set; } // Low confidence (<0.5) or no match
    
    public double AccurateMatchPercentage { get; set; }
    public double LikelyMatchPercentage { get; set; }
    public double UncertainMatchPercentage { get; set; }
    
    public List<ConceptMatch> AccurateConceptList { get; set; } = new();
    public List<ConceptMatch> LikelyConceptList { get; set; } = new();
    public List<string> UncertainConceptList { get; set; } = new();
    
    public Dictionary<string, int> UncertainConceptFrequency { get; set; } = new();
}

/// <summary>
/// Represents a concept match with confidence score.
/// </summary>
public class ConceptMatch
{
    public string ExtractedConcept { get; set; } = string.Empty;
    public string MatchedConcept { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool IsExactMatch { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public int Frequency { get; set; }
}

