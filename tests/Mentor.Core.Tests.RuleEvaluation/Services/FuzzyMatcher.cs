using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for fuzzy matching of concepts to Stat definitions.
/// Uses multiple string similarity algorithms to find the best match with confidence scores.
/// </summary>
public class FuzzyMatcher
{
    private readonly ILogger<FuzzyMatcher>? _logger;
    private readonly double _confidenceThreshold;

    public FuzzyMatcher(ILogger<FuzzyMatcher>? logger = null, double confidenceThreshold = 0.8)
    {
        _logger = logger;
        _confidenceThreshold = confidenceThreshold;
    }

    /// <summary>
    /// Represents a match result with confidence score.
    /// </summary>
    public class MatchResult
    {
        public string? MatchedConcept { get; set; }
        public double Confidence { get; set; }
        public MatchType MatchType { get; set; }
        public bool IsMatch => Confidence >= 0.5 && MatchedConcept != null;
        public bool IsExactMatch => MatchType == MatchType.Exact;
    }

    public enum MatchType
    {
        Exact,
        HighConfidence,
        MediumConfidence,
        LowConfidence,
        NoMatch
    }

    /// <summary>
    /// Finds the best match for an extracted concept against a list of Stat definitions.
    /// </summary>
    public MatchResult FindBestMatch(string extractedConcept, HashSet<string> statNames)
    {
        if (string.IsNullOrWhiteSpace(extractedConcept))
        {
            return new MatchResult { Confidence = 0.0, MatchType = MatchType.NoMatch };
        }

        // Normalize the extracted concept
        var normalized = NormalizeText(extractedConcept);

        // Try exact match first (case-insensitive)
        var exactMatch = statNames.FirstOrDefault(k => 
            string.Equals(NormalizeText(k), normalized, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
        {
            return new MatchResult
            {
                MatchedConcept = exactMatch,
                Confidence = 1.0,
                MatchType = MatchType.Exact
            };
        }

        // Try fuzzy matching
        var bestMatch = FindBestFuzzyMatch(normalized, statNames);
        
        return bestMatch;
    }

    /// <summary>
    /// Finds the best fuzzy match using multiple similarity algorithms.
    /// </summary>
    private MatchResult FindBestFuzzyMatch(string normalized, HashSet<string> statNames)
    {
        var bestMatch = new MatchResult { Confidence = 0.0, MatchType = MatchType.NoMatch };
        var bestScore = 0.0;
        string? bestConcept = null;

        foreach (var statName in statNames)
        {
            var normalizedStatName = NormalizeText(statName);
            
            // Calculate multiple similarity scores
            var levenshteinScore = CalculateLevenshteinSimilarity(normalized, normalizedStatName);
            var jaroWinklerScore = CalculateJaroWinklerSimilarity(normalized, normalizedStatName);
            var tokenScore = CalculateTokenSimilarity(normalized, normalizedStatName);
            var substringScore = CalculateSubstringSimilarity(normalized, normalizedStatName);

            // Combine scores (weighted average)
            var combinedScore = (levenshteinScore * 0.3) + 
                               (jaroWinklerScore * 0.3) + 
                               (tokenScore * 0.3) + 
                               (substringScore * 0.1);

            if (combinedScore > bestScore)
            {
                bestScore = combinedScore;
                bestConcept = statName;
            }
        }

        if (bestConcept != null && bestScore > 0)
        {
            var matchType = bestScore >= 0.8 ? MatchType.HighConfidence :
                           bestScore >= 0.5 ? MatchType.MediumConfidence :
                           MatchType.LowConfidence;

            return new MatchResult
            {
                MatchedConcept = bestConcept,
                Confidence = bestScore,
                MatchType = matchType
            };
        }

        return new MatchResult { Confidence = 0.0, MatchType = MatchType.NoMatch };
    }

    /// <summary>
    /// Calculates Levenshtein distance similarity (normalized to 0-1).
    /// </summary>
    private double CalculateLevenshteinSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (s1 == s2)
            return 1.0;

        var maxLength = Math.Max(s1.Length, s2.Length);
        if (maxLength == 0)
            return 1.0;

        var distance = LevenshteinDistance(s1, s2);
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        if (s1.Length == 0) return s2.Length;
        if (s2.Length == 0) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    /// <summary>
    /// Calculates Jaro-Winkler similarity (0-1).
    /// </summary>
    private double CalculateJaroWinklerSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (s1 == s2)
            return 1.0;

        var jaro = JaroSimilarity(s1, s2);
        var prefixLength = CommonPrefixLength(s1, s2, 4);
        
        return jaro + (0.1 * prefixLength * (1 - jaro));
    }

    /// <summary>
    /// Calculates Jaro similarity.
    /// </summary>
    private double JaroSimilarity(string s1, string s2)
    {
        if (s1 == s2)
            return 1.0;

        var matchWindow = Math.Max(s1.Length, s2.Length) / 2 - 1;
        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];

        var matches = 0;
        var transpositions = 0;

        // Find matches
        for (int i = 0; i < s1.Length; i++)
        {
            var start = Math.Max(0, i - matchWindow);
            var end = Math.Min(i + matchWindow + 1, s2.Length);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j])
                    continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        // Count transpositions
        var k = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i])
                continue;

            while (!s2Matches[k])
                k++;

            if (s1[i] != s2[k])
                transpositions++;

            k++;
        }

        return ((double)matches / s1.Length +
                (double)matches / s2.Length +
                (double)(matches - transpositions / 2.0) / matches) / 3.0;
    }

    /// <summary>
    /// Calculates common prefix length (up to maxLength).
    /// </summary>
    private int CommonPrefixLength(string s1, string s2, int maxLength)
    {
        var prefixLength = 0;
        var max = Math.Min(Math.Min(s1.Length, s2.Length), maxLength);

        for (int i = 0; i < max; i++)
        {
            if (s1[i] == s2[i])
                prefixLength++;
            else
                break;
        }

        return prefixLength;
    }

    /// <summary>
    /// Calculates token-based similarity (how many tokens overlap).
    /// </summary>
    private double CalculateTokenSimilarity(string s1, string s2)
    {
        var tokens1 = Tokenize(s1);
        var tokens2 = Tokenize(s2);

        if (tokens1.Count == 0 && tokens2.Count == 0)
            return 1.0;

        if (tokens1.Count == 0 || tokens2.Count == 0)
            return 0.0;

        var intersection = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).Count();
        var union = tokens1.Union(tokens2, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    /// <summary>
    /// Calculates substring similarity (checks if one contains the other).
    /// </summary>
    private double CalculateSubstringSimilarity(string s1, string s2)
    {
        if (s1.Contains(s2, StringComparison.OrdinalIgnoreCase) || 
            s2.Contains(s1, StringComparison.OrdinalIgnoreCase))
        {
            var shorter = Math.Min(s1.Length, s2.Length);
            var longer = Math.Max(s1.Length, s2.Length);
            return (double)shorter / longer;
        }

        return 0.0;
    }

    /// <summary>
    /// Tokenizes text into words.
    /// </summary>
    private HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var words = text.Split(new[] { ' ', '-', '/', '(', ')', ':', ',' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            var trimmed = word.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                tokens.Add(trimmed);
            }
        }

        return tokens;
    }

    /// <summary>
    /// Normalizes text by removing line breaks and normalizing whitespace.
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Use Regex to handle all whitespace variations comprehensively
        var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ", 
            System.Text.RegularExpressions.RegexOptions.Compiled);

        return normalized.Trim();
    }
}
