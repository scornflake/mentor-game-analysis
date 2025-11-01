using System.Text.RegularExpressions;
using Mentor.Core.Models;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Calculates objective metrics for analyzing recommendation quality
/// </summary>
public class MetricsCalculator
{
    // Warframe-specific entity names (weapons, mods, frames, etc.)
    private static readonly HashSet<string> GameEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Weapons
        "cedo", "phantasma", "acceltra", "kuva nukor", "tigris", "soma", "boltor",
        "braton", "grakata", "arca plasmor", "ignis", "amprex", "synapse",
        
        // Mods
        "serration", "split chamber", "point strike", "vital sense", "hunter munitions",
        "condition overload", "primed continuity", "primed flow", "adaptation",
        "galvanized", "multishot", "hornet strike", "barrel diffusion",
        "hell's chamber", "blaze", "shotgun savvy", "vigilante armaments",
        
        // Warframes
        "mesa", "saryn", "volt", "rhino", "excalibur", "mag", "trinity", "frost",
        "ember", "loki", "nova", "nekros", "vauban", "ash",
        
        // Primes
        "prime", "cedo prime", "phantasma prime", "acceltra prime"
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
    
    /// <summary>
    /// Calculates specificity score: unique game entity mentions / total recommendations
    /// Higher score = more specific, concrete recommendations
    /// </summary>
    public double CalculateSpecificityScore(Recommendation recommendation)
    {
        if (recommendation.Recommendations.Count == 0)
            return 0;
        
        var allText = string.Join(" ", new[]
        {
            recommendation.Analysis,
            recommendation.Summary,
            string.Join(" ", recommendation.Recommendations.Select(r => $"{r.Action} {r.Reasoning} {r.Context}"))
        });
        
        var mentionedEntities = GameEntities
            .Where(entity => allText.Contains(entity, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        
        // Normalize by recommendation count
        return (double)mentionedEntities / recommendation.Recommendations.Count;
    }
    
    /// <summary>
    /// Calculates terminology score: game-specific mechanic terms used
    /// Higher score = more technical and game-knowledgeable
    /// </summary>
    public double CalculateTerminologyScore(Recommendation recommendation)
    {
        var allText = string.Join(" ", new[]
        {
            recommendation.Analysis,
            recommendation.Summary,
            string.Join(" ", recommendation.Recommendations.Select(r => $"{r.Action} {r.Reasoning} {r.Context}"))
        }).ToLowerInvariant();
        
        var termsUsed = MechanicTerms
            .Where(term => allText.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        
        // Normalize to 0-1 scale (assuming 20 unique terms would be excellent)
        return Math.Min(1.0, (double)termsUsed / 20.0);
    }
    
    /// <summary>
    /// Calculates actionability score: recommendations with clear, specific actions
    /// Higher score = more actionable and concrete
    /// </summary>
    public double CalculateActionabilityScore(Recommendation recommendation)
    {
        if (recommendation.Recommendations.Count == 0)
            return 0;
        
        int actionableCount = 0;
        
        foreach (var rec in recommendation.Recommendations)
        {
            bool isActionable = false;
            
            // Has specific action verbs
            var actionVerbs = new[] { "equip", "use", "replace", "add", "remove", "upgrade", "forma", "max", "slot", "build" };
            if (actionVerbs.Any(verb => rec.Action.Contains(verb, StringComparison.OrdinalIgnoreCase)))
            {
                isActionable = true;
            }
            
            // Mentions specific items/mods
            if (GameEntities.Any(entity => rec.Action.Contains(entity, StringComparison.OrdinalIgnoreCase)))
            {
                isActionable = true;
            }
            
            // Has reasoning with "because", "to", "for"
            if (rec.Reasoning.Contains("because", StringComparison.OrdinalIgnoreCase) ||
                rec.Reasoning.Contains(" to ", StringComparison.OrdinalIgnoreCase) ||
                rec.Reasoning.Contains(" for ", StringComparison.OrdinalIgnoreCase))
            {
                isActionable = true;
            }
            
            if (isActionable)
                actionableCount++;
        }
        
        return (double)actionableCount / recommendation.Recommendations.Count;
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

