namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Represents a normalized stat in the game system.
/// Stats are shared across all entities (weapons, mods, warframes).
/// </summary>
public class Stat
{
    /// <summary>
    /// Normalized stat name (e.g., "Status Chance", "Damage", "Critical Chance").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this stat represents.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category of the stat: "damage", "status", "critical", "combat", "defensive", "mobility", "energy", "faction", "utility", "special", "weapon", "other".
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

