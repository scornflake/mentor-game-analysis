using System.Text.Json.Serialization;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

public class PublicExportMod
{
    public string UniqueName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Polarity { get; set; }
    public string? Rarity { get; set; }
    public int? BaseDrain { get; set; }
    public int? FusionLimit { get; set; }
    public bool? IsUtility { get; set; }
    public List<string>? Tags { get; set; }

    /// <summary>
    /// All effects at each rank level. Each entry represents one rank level.
    /// </summary>
    public List<List<string>>? Effects { get; set; }

    /// <summary>
    /// Gets the effects at max rank (last entry in Effects list).
    /// </summary>
    [JsonIgnore] public List<string>? FullyRankedEffects => Effects?.LastOrDefault();

    [JsonIgnore] public List<string>? UnrankedEffects => Effects?.FirstOrDefault();
}


