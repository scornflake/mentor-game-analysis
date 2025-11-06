namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Represents a structured effect parsed from mod effect strings.
/// Models the various properties of effects including conditions, stacking, duration, etc.
/// </summary>
public class Effect
{
    /// <summary>
    /// Type of effect: increase, boost, reduce, conversion, grant, etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Numeric value of the effect (e.g., 58.2 for "+58.2% Status Chance").
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    /// Unit of the value: "%", "flat", "multiplier", etc.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Condition that triggers the effect: "on_kill", "on_status", "when_damaged", "none", etc.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Duration of the effect in seconds (e.g., 20 for "for 20s").
    /// </summary>
    public double? Duration { get; set; }

    /// <summary>
    /// Whether the effect stacks.
    /// </summary>
    public bool Stacking { get; set; }

    /// <summary>
    /// Maximum number of stacks (e.g., 3 for "Stacks up to 3x").
    /// </summary>
    public int? MaxStacks { get; set; }

    /// <summary>
    /// Type of stacking: "count" (e.g., "3x") or "percentage" (e.g., "90%").
    /// </summary>
    public string? StackType { get; set; }

    /// <summary>
    /// Trigger for the effect (e.g., "per_status_type", "on_bullet_jump").
    /// </summary>
    public string? Trigger { get; set; }

    /// <summary>
    /// Operation type: "additive" or "multiplicative".
    /// </summary>
    public string Operation { get; set; } = "additive";

    /// <summary>
    /// Per-unit modifier: "per_status_type", "per_stack_of_slash", "for_each_enemy", etc.
    /// </summary>
    public string? PerUnit { get; set; }

    /// <summary>
    /// Condition that consumes stacks (e.g., "taking_damage" for "Taking damage will consume a stack").
    /// </summary>
    public string? ConsumeCondition { get; set; }

    /// <summary>
    /// Until condition (e.g., "until_magazine_empty" for "until the magazine is empty").
    /// </summary>
    public string? UntilCondition { get; set; }

    /// <summary>
    /// Will effect: "applied_twice", "also_apply", "refund", etc.
    /// </summary>
    public string? WillEffect { get; set; }

    /// <summary>
    /// Original effect string for reference.
    /// </summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// The stat that this effect modifies (set during parsing/matching).
    /// </summary>
    public string? StatName { get; set; }
}

