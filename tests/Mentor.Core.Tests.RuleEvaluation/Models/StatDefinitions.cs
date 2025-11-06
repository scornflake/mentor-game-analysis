using System.Collections.Generic;

namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Provides stat definitions to replace KeyInformation constants.
/// Can be loaded from JSON or defined as a collection.
/// </summary>
public static class StatDefinitions
{
    /// <summary>
    /// Gets all stat definitions with their categories.
    /// </summary>
    public static List<Stat> GetAllStats()
    {
        return new List<Stat>
        {
            // DAMAGE TYPES
            new Stat { Name = "Blast Damage", Category = "damage" },
            new Stat { Name = "Corrosive Damage", Category = "damage" },
            new Stat { Name = "Damage", Category = "damage" },
            new Stat { Name = "Gas Damage", Category = "damage" },
            new Stat { Name = "Impact Damage", Category = "damage" },
            new Stat { Name = "Magnetic Damage", Category = "damage" },
            new Stat { Name = "Puncture Damage", Category = "damage" },
            new Stat { Name = "Radiation Damage", Category = "damage" },
            new Stat { Name = "Slash Damage", Category = "damage" },
            new Stat { Name = "Viral Damage", Category = "damage" },

            // STATUS EFFECTS
            new Stat { Name = "Status Chance", Category = "status" },
            new Stat { Name = "Status Duration", Category = "status" },
            new Stat { Name = "Status Duration on Self", Category = "status" },

            // CRITICAL STATS
            new Stat { Name = "Critical Chance", Category = "critical" },
            new Stat { Name = "Critical Damage", Category = "critical" },

            // COMBAT STATS
            new Stat { Name = "Ability Range", Category = "combat" },
            new Stat { Name = "Accuracy", Category = "combat" },
            new Stat { Name = "Fire Rate", Category = "combat" },
            new Stat { Name = "Magazine Capacity", Category = "combat" },
            new Stat { Name = "Multishot", Category = "combat" },
            new Stat { Name = "Projectile Speed", Category = "combat" },
            new Stat { Name = "Punch Through", Category = "combat" },
            new Stat { Name = "Range", Category = "combat" },
            new Stat { Name = "Recoil", Category = "combat" },
            new Stat { Name = "Reload Speed", Category = "combat" },
            new Stat { Name = "Weapon Recoil", Category = "combat" },

            // DEFENSIVE STATS
            new Stat { Name = "Armor", Category = "defensive" },
            new Stat { Name = "Damage Reduction", Category = "defensive" },
            new Stat { Name = "Damage Resistance", Category = "defensive" },
            new Stat { Name = "Health", Category = "defensive" },
            new Stat { Name = "Shield", Category = "defensive" },
            new Stat { Name = "Shield Capacity", Category = "defensive" },
            new Stat { Name = "Shield Recharge", Category = "defensive" },
            new Stat { Name = "Shield Recharge Delay", Category = "defensive" },

            // MOBILITY STATS
            new Stat { Name = "Aim Glide/Wall Latch Duration", Category = "mobility" },
            new Stat { Name = "Casting Speed", Category = "mobility" },
            new Stat { Name = "Friction", Category = "mobility" },
            new Stat { Name = "Parkour Velocity", Category = "mobility" },
            new Stat { Name = "Slide", Category = "mobility" },
            new Stat { Name = "Sprint Speed", Category = "mobility" },

            // ENERGY/ABILITY STATS
            new Stat { Name = "Ability Duration", Category = "energy" },
            new Stat { Name = "Ability Efficiency", Category = "energy" },
            new Stat { Name = "Ability Strength", Category = "energy" },
            new Stat { Name = "Energy", Category = "energy" },
            new Stat { Name = "Energy Max", Category = "energy" },
            new Stat { Name = "Maximum Energy", Category = "energy" },

            // FACTION DAMAGE
            new Stat { Name = "Damage to Corpus", Category = "faction" },
            new Stat { Name = "Damage to Grineer", Category = "faction" },
            new Stat { Name = "Damage to Infested", Category = "faction" },
            new Stat { Name = "Damage to Orokin", Category = "faction" },

            // UTILITY
            new Stat { Name = "Enemy Radar", Category = "utility" },
            new Stat { Name = "Hacking Time", Category = "utility" },
            new Stat { Name = "Loot Radar", Category = "utility" },

            // SPECIAL EFFECTS
            new Stat { Name = "Chance to Resist Knockdown", Category = "special" },
            new Stat { Name = "Finisher Attacks", Category = "special" },
            new Stat { Name = "Headshot Multiplier", Category = "special" },
            new Stat { Name = "Knockdown", Category = "special" },
            new Stat { Name = "Self Stagger", Category = "special" },
            new Stat { Name = "Stagger on Block", Category = "special" },
            new Stat { Name = "Stun on Block", Category = "special" },

            // WEAPON PROPERTIES
            new Stat { Name = "Ammo Pickup", Category = "weapon" },
            new Stat { Name = "Explosion Radius", Category = "weapon" },

            // OTHER
            new Stat { Name = "Cold", Category = "other" },
            new Stat { Name = "Cold on Bullet Jump", Category = "other" },
            new Stat { Name = "Electricity", Category = "other" },
            new Stat { Name = "Heat", Category = "other" },
            new Stat { Name = "Heat on Bullet Jump", Category = "other" },
            new Stat { Name = "Toxin", Category = "other" },
            new Stat { Name = "Toxin on Bullet Jump", Category = "other" }
        };
    }

    /// <summary>
    /// Gets all stat names as a HashSet for matching.
    /// </summary>
    public static HashSet<string> GetAllStatNames()
    {
        return new HashSet<string>(GetAllStats().Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a stat by name.
    /// </summary>
    public static Stat? GetStatByName(string name)
    {
        return GetAllStats().FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

