using System.Text.RegularExpressions;
using Mentor.Core.Tests.RuleEvaluation.Models;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for parsing mod effect strings into structured Effect objects.
/// Handles all identified patterns including conditional effects, stacking, duration, per-unit modifiers, etc.
/// </summary>
public class EffectParserService
{
    private readonly HashSet<string> _statNames;

    public EffectParserService()
    {
        _statNames = StatDefinitions.GetAllStatNames();
    }

    /// <summary>
    /// Parses a mod effect string into a structured Effect object.
    /// </summary>
    public Effect? ParseEffect(string effectText)
    {
        if (string.IsNullOrWhiteSpace(effectText))
        {
            return null;
        }

        var effect = new Effect
        {
            OriginalText = effectText.Trim()
        };

        // Normalize text for parsing
        var normalized = NormalizeText(effectText);

        // Extract condition (must be done first as it affects other parsing)
        ExtractCondition(normalized, effect);

        // Extract stacking information
        ExtractStacking(normalized, effect);

        // Extract duration
        ExtractDuration(normalized, effect);

        // Extract per-unit modifiers
        ExtractPerUnit(normalized, effect);

        // Extract until condition
        ExtractUntilCondition(normalized, effect);

        // Extract will effects
        ExtractWillEffect(normalized, effect);

        // Extract consume condition
        ExtractConsumeCondition(normalized, effect);

        // Extract value and unit (percentage, multiplier, flat)
        ExtractValueAndUnit(normalized, effect);

        // Extract operation type (additive vs multiplicative)
        ExtractOperationType(normalized, effect);

        // Extract effect type
        ExtractEffectType(normalized, effect);

        // Extract trigger
        ExtractTrigger(normalized, effect);

        // Match to stat name
        MatchStatName(normalized, effect);

        return effect;
    }

    /// <summary>
    /// Parses multiple effect strings from a mod rank.
    /// </summary>
    public List<Effect> ParseEffects(IEnumerable<string> effectStrings)
    {
        var effects = new List<Effect>();
        foreach (var effectText in effectStrings)
        {
            var effect = ParseEffect(effectText);
            if (effect != null)
            {
                effects.Add(effect);
            }
        }
        return effects;
    }

    private void ExtractCondition(string text, Effect effect)
    {
        // Patterns: "On Kill:", "On Status Effect:", "When Damaged:", "On Weak Point Kill:", etc.
        var conditionPatterns = new Dictionary<string, string>
        {
            { @"On\s+Kill\s*:", "on_kill" },
            { @"On\s+Status\s+Effect\s*:", "on_status" },
            { @"When\s+Damaged\s*:", "when_damaged" },
            { @"On\s+Weak\s+Point\s+Kill\s*:", "on_weak_point_kill" },
            { @"On\s+Jump\s+Kick\s*:", "on_jump_kick" },
            { @"On\s+Bullet\s+Jump\s*:", "on_bullet_jump" },
            { @"Restoring\s+health\s+with\s+abilities", "restoring_health_with_abilities" },
            { @"Wall\s+Dashing", "wall_dashing" },
            { @"Killing\s+a\s+Marked\s+Enemy", "killing_marked_enemy" },
            { @"Secondary\s+Fire", "secondary_fire" },
            { @"Melee\s+attacks", "melee_attacks" }
        };

        foreach (var pattern in conditionPatterns)
        {
            if (Regex.IsMatch(text, pattern.Key, RegexOptions.IgnoreCase))
            {
                effect.Condition = pattern.Value;
                break;
            }
        }

        // If no condition found, set to "none"
        if (string.IsNullOrEmpty(effect.Condition))
        {
            effect.Condition = "none";
        }
    }

    private void ExtractStacking(string text, Effect effect)
    {
        // Patterns: "Stacks up to 3x", "Stacks up to 90%", "Stacks up to 2 times"
        var stackPattern = new Regex(@"Stacks?\s+up\s+to\s+(\d+(?:\.\d+)?)(x|%|times?)", RegexOptions.IgnoreCase);
        var match = stackPattern.Match(text);
        
        if (match.Success)
        {
            effect.Stacking = true;
            if (double.TryParse(match.Groups[1].Value, out var maxStacks))
            {
                effect.MaxStacks = (int)maxStacks;
            }

            var stackTypeIndicator = match.Groups[2].Value.ToLower();
            if (stackTypeIndicator == "%")
            {
                effect.StackType = "percentage";
            }
            else
            {
                effect.StackType = "count";
            }
        }
    }

    private void ExtractDuration(string text, Effect effect)
    {
        // Patterns: "for 10s", "for 20s", "after 3s"
        var durationPattern = new Regex(@"(?:for|after)\s+(\d+(?:\.\d+)?)s", RegexOptions.IgnoreCase);
        var match = durationPattern.Match(text);
        
        if (match.Success)
        {
            if (double.TryParse(match.Groups[1].Value, out var duration))
            {
                effect.Duration = duration;
            }
        }
    }

    private void ExtractPerUnit(string text, Effect effect)
    {
        // Patterns: "per Status Type", "per stack of Slash", "for each enemy within 7.5m"
        var perUnitPatterns = new Dictionary<Regex, string>
        {
            { new Regex(@"per\s+Status\s+Type", RegexOptions.IgnoreCase), "per_status_type" },
            { new Regex(@"per\s+stack\s+of\s+(\w+)", RegexOptions.IgnoreCase), "per_stack" },
            { new Regex(@"for\s+each\s+enemy\s+within\s+[\d.]+m", RegexOptions.IgnoreCase), "for_each_enemy" },
            { new Regex(@"per\s+Status\s+Type\s+affecting\s+the\s+target", RegexOptions.IgnoreCase), "per_status_type" }
        };

        foreach (var pattern in perUnitPatterns)
        {
            if (pattern.Key.IsMatch(text))
            {
                effect.PerUnit = pattern.Value;
                break;
            }
        }
    }

    private void ExtractUntilCondition(string text, Effect effect)
    {
        // Patterns: "until the magazine is empty"
        if (Regex.IsMatch(text, @"until\s+the\s+magazine\s+is\s+empty", RegexOptions.IgnoreCase))
        {
            effect.UntilCondition = "until_magazine_empty";
        }
    }

    private void ExtractWillEffect(string text, Effect effect)
    {
        // Patterns: "will be applied twice", "will also apply", "will refund 25% of the ammo"
        if (Regex.IsMatch(text, @"will\s+be\s+applied\s+twice", RegexOptions.IgnoreCase))
        {
            effect.WillEffect = "applied_twice";
        }
        else if (Regex.IsMatch(text, @"will\s+also\s+apply", RegexOptions.IgnoreCase))
        {
            effect.WillEffect = "also_apply";
        }
        else if (Regex.IsMatch(text, @"will\s+refund", RegexOptions.IgnoreCase))
        {
            effect.WillEffect = "refund";
        }
    }

    private void ExtractConsumeCondition(string text, Effect effect)
    {
        // Patterns: "Taking damage will consume a stack after 3s"
        if (Regex.IsMatch(text, @"Taking\s+damage\s+will\s+consume\s+a\s+stack", RegexOptions.IgnoreCase))
        {
            effect.ConsumeCondition = "taking_damage";
        }
    }

    private void ExtractValueAndUnit(string text, Effect effect)
    {
        // Pattern 1: Percentage format: "+58.2%", "-10%", "+125%"
        var percentagePattern = new Regex(@"([+-]?)(\d+(?:\.\d+)?)%");
        var percentageMatch = percentagePattern.Match(text);
        
        if (percentageMatch.Success)
        {
            var sign = percentageMatch.Groups[1].Value == "-" ? -1.0 : 1.0;
            if (double.TryParse(percentageMatch.Groups[2].Value, out var value))
            {
                effect.Value = sign * value;
                effect.Unit = "%";
            }
            return;
        }

        // Pattern 2: Multiplier format: "x1.5", "x1.25"
        var multiplierPattern = new Regex(@"x(\d+(?:\.\d+)?)");
        var multiplierMatch = multiplierPattern.Match(text);
        
        if (multiplierMatch.Success)
        {
            if (double.TryParse(multiplierMatch.Groups[1].Value, out var value))
            {
                effect.Value = value;
                effect.Unit = "multiplier";
            }
            return;
        }

        // Pattern 3: Flat value: "75 Armor", "25 health stolen"
        var flatPattern = new Regex(@"(\d+(?:\.\d+)?)\s+(?:Armor|health|Energy|Ability\s+Strength|Range|Projectile\s+Speed|Beam\s+Range)", RegexOptions.IgnoreCase);
        var flatMatch = flatPattern.Match(text);
        
        if (flatMatch.Success)
        {
            if (double.TryParse(flatMatch.Groups[1].Value, out var value))
            {
                effect.Value = value;
                effect.Unit = "flat";
            }
        }
    }

    private void ExtractOperationType(string text, Effect effect)
    {
        // Multiplier format indicates multiplicative operation
        if (effect.Unit == "multiplier" || text.Contains("x", StringComparison.OrdinalIgnoreCase))
        {
            effect.Operation = "multiplicative";
        }
        else
        {
            effect.Operation = "additive";
        }
    }

    private void ExtractEffectType(string text, Effect effect)
    {
        // Determine effect type based on keywords
        if (Regex.IsMatch(text, @"Convert|converted", RegexOptions.IgnoreCase))
        {
            effect.Type = "conversion";
        }
        else if (Regex.IsMatch(text, @"grant|grants", RegexOptions.IgnoreCase))
        {
            effect.Type = "grant";
        }
        else if (text.StartsWith("-", StringComparison.Ordinal) || Regex.IsMatch(text, @"less\s+likely|reduce|reduction", RegexOptions.IgnoreCase))
        {
            effect.Type = "reduce";
        }
        else if (Regex.IsMatch(text, @"more\s+likely|increase|boost", RegexOptions.IgnoreCase) || text.StartsWith("+", StringComparison.Ordinal))
        {
            effect.Type = "increase";
        }
        else
        {
            effect.Type = "increase"; // Default
        }
    }

    private void ExtractTrigger(string text, Effect effect)
    {
        // Extract trigger from condition or text
        if (effect.Condition == "on_bullet_jump")
        {
            effect.Trigger = "on_bullet_jump";
        }
        else if (effect.Condition == "on_status")
        {
            effect.Trigger = "on_status_effect";
        }
        else if (effect.Condition == "on_kill")
        {
            effect.Trigger = "on_kill";
        }
    }

    private void MatchStatName(string text, Effect effect)
    {
        // Try to match against known stat names
        foreach (var statName in _statNames)
        {
            // Check if the stat name appears in the text
            if (text.Contains(statName, StringComparison.OrdinalIgnoreCase))
            {
                effect.StatName = statName;
                break;
            }
        }

        // If no direct match, try fuzzy matching for common patterns
        if (string.IsNullOrEmpty(effect.StatName))
        {
            // Try to match damage types
            var damageTypes = new[] { "Blast Damage", "Corrosive Damage", "Gas Damage", "Impact Damage", 
                "Magnetic Damage", "Puncture Damage", "Radiation Damage", "Slash Damage", "Viral Damage" };
            
            foreach (var damageType in damageTypes)
            {
                if (text.Contains(damageType, StringComparison.OrdinalIgnoreCase))
                {
                    effect.StatName = damageType;
                    break;
                }
            }
        }
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Replace newlines with spaces
        var normalized = Regex.Replace(text, @"\s+", " ", RegexOptions.Compiled);
        return normalized.Trim();
    }
}

