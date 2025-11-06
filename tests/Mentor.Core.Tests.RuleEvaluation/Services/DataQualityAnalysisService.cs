using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Mentor.Core.Tests.RuleEvaluation.Services;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for analyzing data quality by comparing extracted concepts to Stat definitions.
/// Uses fuzzy matching to categorize concepts as Accurate, Likely, or Uncertain.
/// </summary>
public class DataQualityAnalysisService
{
    private readonly KeyInformationExtractor _extractor;
    private readonly FuzzyMatcher _fuzzyMatcher;
    private readonly ILogger<DataQualityAnalysisService>? _logger;

    public DataQualityAnalysisService(
        KeyInformationExtractor extractor,
        FuzzyMatcher fuzzyMatcher,
        ILogger<DataQualityAnalysisService>? logger = null)
    {
        _extractor = extractor;
        _fuzzyMatcher = fuzzyMatcher;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes data quality for mods, warframes, and weapons.
    /// </summary>
    public async Task<DataQualityReport> AnalyzeDataQualityAsync(
        string modsFilePath,
        string weaponsFilePath,
        string warframesFilePath,
        double confidenceThreshold = 0.8,
        CancellationToken cancellationToken = default)
    {
        var report = new DataQualityReport();

        // Get all Stat definitions
        var statNames = StatDefinitions.GetAllStatNames();
        _logger?.LogInformation($"Loaded {statNames.Count} Stat definitions");

        // Extract concepts from all entities
        var allExtractedConcepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conceptFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Analyze mods
        if (File.Exists(modsFilePath))
        {
            _logger?.LogInformation("Analyzing mods...");
            var modsConcepts = await _extractor.ExtractFromModsAsync(modsFilePath, cancellationToken);
            foreach (var concept in modsConcepts)
            {
                allExtractedConcepts.Add(concept);
                conceptFrequency[concept] = conceptFrequency.GetValueOrDefault(concept, 0) + 1;
            }
            report.EntitiesWithConcepts += modsConcepts.Count > 0 ? 1 : 0;
        }

        // Analyze weapons
        if (File.Exists(weaponsFilePath))
        {
            _logger?.LogInformation("Analyzing weapons...");
            var weaponsConcepts = await _extractor.ExtractFromWeaponsAsync(weaponsFilePath, cancellationToken);
            foreach (var concept in weaponsConcepts)
            {
                allExtractedConcepts.Add(concept);
                conceptFrequency[concept] = conceptFrequency.GetValueOrDefault(concept, 0) + 1;
            }
            report.EntitiesWithConcepts += weaponsConcepts.Count > 0 ? 1 : 0;
        }

        // Analyze warframes
        if (File.Exists(warframesFilePath))
        {
            _logger?.LogInformation("Analyzing warframes...");
            var warframesConcepts = await _extractor.ExtractFromWarframesAsync(warframesFilePath, cancellationToken);
            foreach (var concept in warframesConcepts)
            {
                allExtractedConcepts.Add(concept);
                conceptFrequency[concept] = conceptFrequency.GetValueOrDefault(concept, 0) + 1;
            }
            report.EntitiesWithConcepts += warframesConcepts.Count > 0 ? 1 : 0;
        }

        report.TotalUniqueConceptsExtracted = allExtractedConcepts.Count;

        // Match each extracted concept to Stat definitions
        var accurateMatches = new List<ConceptMatch>();
        var likelyMatches = new List<ConceptMatch>();
        var uncertainConcepts = new List<string>();

        foreach (var concept in allExtractedConcepts.OrderBy(c => c))
        {
            var matchResult = _fuzzyMatcher.FindBestMatch(concept, statNames);

            if (matchResult.MatchedConcept != null && matchResult.Confidence >= 0.8)
            {
                // Accurate match (exact or high confidence)
                accurateMatches.Add(new ConceptMatch
                {
                    ExtractedConcept = concept,
                    MatchedConcept = matchResult.MatchedConcept,
                    Confidence = matchResult.Confidence,
                    IsExactMatch = matchResult.IsExactMatch,
                    MatchType = matchResult.MatchType.ToString(),
                    Frequency = conceptFrequency[concept]
                });
            }
            else if (matchResult.MatchedConcept != null && matchResult.Confidence >= 0.5)
            {
                // Likely match (medium confidence)
                likelyMatches.Add(new ConceptMatch
                {
                    ExtractedConcept = concept,
                    MatchedConcept = matchResult.MatchedConcept,
                    Confidence = matchResult.Confidence,
                    IsExactMatch = matchResult.IsExactMatch,
                    MatchType = matchResult.MatchType.ToString(),
                    Frequency = conceptFrequency[concept]
                });
            }
            else
            {
                // Uncertain (no match or low confidence)
                uncertainConcepts.Add(concept);
                report.UncertainConceptFrequency[concept] = conceptFrequency[concept];
            }
        }

        report.AccurateMatches = accurateMatches.Count;
        report.LikelyMatches = likelyMatches.Count;
        report.UncertainMatches = uncertainConcepts.Count;

        report.AccurateConceptList = accurateMatches.OrderByDescending(m => m.Confidence).ThenByDescending(m => m.Frequency).ToList();
        report.LikelyConceptList = likelyMatches.OrderByDescending(m => m.Confidence).ThenByDescending(m => m.Frequency).ToList();
        report.UncertainConceptList = uncertainConcepts.OrderByDescending(c => report.UncertainConceptFrequency[c]).ToList();

        // Calculate percentages
        if (report.TotalUniqueConceptsExtracted > 0)
        {
            report.AccurateMatchPercentage = (report.AccurateMatches / (double)report.TotalUniqueConceptsExtracted) * 100;
            report.LikelyMatchPercentage = (report.LikelyMatches / (double)report.TotalUniqueConceptsExtracted) * 100;
            report.UncertainMatchPercentage = (report.UncertainMatches / (double)report.TotalUniqueConceptsExtracted) * 100;
        }

        _logger?.LogInformation($"Data quality analysis complete:");
        _logger?.LogInformation($"  Accurate matches: {report.AccurateMatches} ({report.AccurateMatchPercentage:F1}%)");
        _logger?.LogInformation($"  Likely matches: {report.LikelyMatches} ({report.LikelyMatchPercentage:F1}%)");
        _logger?.LogInformation($"  Uncertain matches: {report.UncertainMatches} ({report.UncertainMatchPercentage:F1}%)");

        return report;
    }

}

