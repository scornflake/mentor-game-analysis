using System.Text.RegularExpressions;
using Mentor.Core.Models;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Calculates objective metrics for analyzing recommendation quality
/// </summary>
public class MetricsCalculator
{
    // Warframe-specific entity names (weapons, mods, frames, etc.) loaded from data/ folder
    private static readonly HashSet<string> SpecificTerms;

    static MetricsCalculator()
    {
        SpecificTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LoadSpecificTermsFromDataFolder();
    }

    private static void LoadSpecificTermsFromDataFolder()
    {
        var dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
        
        if (!Directory.Exists(dataFolder))
        {
            // Fallback: try relative path from project root
            var projectRoot = Directory.GetCurrentDirectory();
            dataFolder = Path.Combine(projectRoot, "data");
        }

        if (!Directory.Exists(dataFolder))
        {
            Console.WriteLine($"Warning: data folder not found at {dataFolder}");
            return;
        }

        var txtFiles = Directory.GetFiles(dataFolder, "*.txt");
        var totalLoaded = 0;
        
        foreach (var file in txtFiles)
        {
            try
            {
                var lines = File.ReadAllLines(file);
                var countBefore = SpecificTerms.Count;
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        SpecificTerms.Add(trimmedLine);
                    }
                }
                
                var loadedFromFile = SpecificTerms.Count - countBefore;
                totalLoaded += loadedFromFile;
                Console.WriteLine($"Loaded {loadedFromFile} terms from {Path.GetFileName(file)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load {file}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"Total specific terms loaded: {totalLoaded} ({SpecificTerms.Count} unique)");
    }
    
    private static readonly HashSet<string> VagueWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "good", "best", "fun", "try", "use", "play", "strong", "weak", "maybe", "perhaps"
    };

    // Game-specific mechanic terms
    private static readonly HashSet<string> MechanicTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "status chance", "crit", "critical", "corrosive", "viral", "slash",
        "proc", "status proc", "armor", "strip", "damage type", "elemental",
        "multishot", "fire rate", "reload speed", "magazine", "toxin", "cold",
        "heat", "electric", "radiation", "magnetic", "gas", "blast", "impact",
        "puncture", "faction", "grineer", "corpus", "infested", "corrupted",
        "steel path", "sortie", "eidolon", "build", "mod", "galvanized",
        "arcane", "riven", "kuva", "forma", "polarity", "umbral", "primed"
    };
    
    private static readonly HashSet<string> ActionVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "equip", "use", "replace", "add", "remove", "upgrade", "forma", "max", "slot", "build",
        // Suggested additions: Warframe-specific
        "install", "polarize", "subsume", "infuse", "mod", "rank", "catalyze"
    };

    // New array for better reasoning detection
    private static readonly HashSet<string> ReasoningIndicators = new(StringComparer.OrdinalIgnoreCase)
    {
        "because", "to", "for", "in order to", "which", "allowing", "enabling", "as it", "since"
    };

    /// <summary>
    /// Calculates specificity score: average of per-recommendation specifics (entities + mechanics + stats - vague) / words
    /// Higher score = more specific, concrete recommendations
    /// </summary>
    public double CalculateSpecificityScore(Recommendation recommendation)
    {
        if (recommendation.Recommendations.Count == 0)
            return 0;

        double totalScore = 0;

        foreach (var rec in recommendation.Recommendations)
        {
            var recText = $"{rec.Action} {rec.Reasoning} {rec.Context}".ToLowerInvariant();

            // Word count for normalization
            var words = Regex.Matches(recText, @"\b\w+\b").Count;
            if (words == 0) continue;

            // Count specific terms (with word boundaries)
            int specificCount = SpecificTerms.Count(term => Regex.IsMatch(recText, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase));

            // Count stats/numbers (e.g., "165%", "30")
            int statsCount = Regex.Matches(recText, @"\d+%?|\b\d+\b").Count;

            // Count vague words
            int vagueCount = VagueWords.Count(word => Regex.IsMatch(recText, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase));

            // Raw score per rec
            double rawScore = specificCount + (0.5 * statsCount) - vagueCount;

            // Normalize and clamp to 0-1
            double recScore = Math.Max(0, Math.Min(1, rawScore / words));
            totalScore += recScore;
        }

        // Average across recommendations
        return totalScore / recommendation.Recommendations.Count;
    }
    
    /// <summary>
    /// Calculates terminology score: average of per-rec mechanic terms + stats - vague / words
    /// Higher score = more technical and game-knowledgeable
    /// </summary>
    public double CalculateTerminologyScore(Recommendation recommendation)
    {
        if (recommendation.Recommendations.Count == 0)
            return 0;

        double totalScore = 0;

        foreach (var rec in recommendation.Recommendations)
        {
            var recText = $"{rec.Action} {rec.Reasoning} {rec.Context}".ToLowerInvariant();

            // Word count for normalization
            var words = Regex.Matches(recText, @"\b\w+\b").Count;
            if (words == 0) continue;

            // Count mechanic terms (with word boundaries)
            int termCount = MechanicTerms.Count(term => Regex.IsMatch(recText, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase));

            // Count stats/numbers
            int statsCount = Regex.Matches(recText, @"\d+%?|\b\d+\b").Count;

            // Count vague words
            int vagueCount = VagueWords.Count(word => Regex.IsMatch(recText, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase));

            // Raw score per rec
            double rawScore = termCount + (0.5 * statsCount) - vagueCount;

            // Normalize and clamp to 0-1 (adjust divisor for "excellent" threshold, e.g., expect ~0.2 terms/word)
            double recScore = Math.Max(0, Math.Min(1, rawScore / (words * 5)));  // *5 makes it harder to reach 1; tune based on data
            totalScore += recScore;
        }

        // Average across recommendations
        return totalScore / recommendation.Recommendations.Count;
    }
    
    /// <summary>
    /// Calculates actionability score: average of per-rec criteria met (verb + entity + reasoning)
    /// Higher score = more actionable and concrete
    /// </summary>
    public double CalculateActionabilityScore(Recommendation recommendation)
    {
        if (recommendation.Recommendations.Count == 0)
            return 0;

        double totalScore = 0;

        foreach (var rec in recommendation.Recommendations)
        {
            var actionText = rec.Action.ToLowerInvariant();
            var reasoningText = rec.Reasoning.ToLowerInvariant();
            var contextText = rec.Context.ToLowerInvariant();
            var fullText = $"{actionText} {reasoningText} {contextText}";

            double recScore = 0;

            // Check for action verbs (must have at least one)
            bool hasVerb = ActionVerbs.Any(verb => Regex.IsMatch(actionText, $@"\b{Regex.Escape(verb)}\b", RegexOptions.IgnoreCase));
            if (hasVerb) recScore += 0.33;

            // Check for specific entities/items (bonus for multiple)
            int entityCount = SpecificTerms.Count(entity => Regex.IsMatch(fullText, $@"\b{Regex.Escape(entity)}\b", RegexOptions.IgnoreCase));
            if (entityCount > 0) recScore += Math.Min(0.33, 0.33 * (entityCount / 2.0));  // Cap at 0.33, reward depth

            // Check for reasoning indicators
            bool hasReasoning = ReasoningIndicators.Any(ind => Regex.IsMatch(reasoningText, $@"\b{Regex.Escape(ind)}\b", RegexOptions.IgnoreCase));
            if (hasReasoning) recScore += 0.33;

            // Bonus for stats in action/reasoning
            int statsCount = Regex.Matches(fullText, @"\d+%?|\b\d+\b").Count;
            if (statsCount > 0) recScore += 0.1;  // Small bonus, clamped below

            // Clamp to 0-1
            recScore = Math.Min(1, recScore);
            totalScore += recScore;
        }

        // Average across recommendations
        return totalScore / recommendation.Recommendations.Count;
    }
    
    /// <summary>
    /// Calculates all metrics for a single recommendation
    /// </summary>
    public (double specificity, double terminology, double actionability) CalculateAllMetrics(Recommendation recommendation)
    {
        return (
            CalculateSpecificityScore(recommendation),
            CalculateTerminologyScore(recommendation),
            CalculateActionabilityScore(recommendation)
        );
    }
}

