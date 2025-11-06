using System.Text.Json.Serialization;

namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Model for deserializing import limit JSON files.
/// Used to filter which mods, weapons, and warframes are imported into Neo4j.
/// </summary>
public class ImportLimit
{
    /// <summary>
    /// List of mod names to import. If null or empty, all mods will be imported.
    /// </summary>
    [JsonPropertyName("mods")]
    public List<string>? Mods { get; set; }

    /// <summary>
    /// List of weapon names to import. If null or empty, all weapons will be imported.
    /// </summary>
    [JsonPropertyName("weapons")]
    public List<string>? Weapons { get; set; }

    /// <summary>
    /// List of warframe names to import. If null or empty, all warframes will be imported.
    /// </summary>
    [JsonPropertyName("warframes")]
    public List<string>? Warframes { get; set; }
}

