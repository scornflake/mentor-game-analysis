using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for downloading and parsing Warframe Public Export data
/// Focus: ExportUpgrades_en.json (mods), excluding Rivens and Set mods.
/// </summary>
public class PublicExportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicExportService> _logger;

    public PublicExportService(IHttpClientFactory httpClientFactory, ILogger<PublicExportService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generic method to fetch any Public Export file and save raw JSON to disk
    /// </summary>
    public async Task<string> FetchRawExportAsync(string exportType, string languageCode = "en", string? outputDir = null, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MentorRuleEval", "1.0"));

        // 1) Download the language-specific index from origin (LZMA)
        var indexUrl = $"https://origin.warframe.com/PublicExport/index_{languageCode}.txt.lzma";
        _logger.LogInformation("Downloading Public Export index: {IndexUrl}", indexUrl);
        var indexBytes = await client.GetByteArrayAsync(indexUrl, cancellationToken);

        // 2) Decompress LZMA -> text
        var indexText = LzmaHelper.DecompressToString(indexBytes);
        if (string.IsNullOrWhiteSpace(indexText))
        {
            throw new InvalidOperationException("Failed to decompress Public Export index (empty result)");
        }

        // 3) Find hashed entry for the export type
        var lines = indexText.Split('\n');
        var prefix = $"{exportType}_{languageCode}.json!";
        var hashed = lines.FirstOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(hashed))
        {
            throw new InvalidOperationException($"Could not find {prefix} in Public Export index");
        }

        var manifestUrl = $"http://content.warframe.com/PublicExport/Manifest/{hashed.Trim()}";
        _logger.LogInformation("Resolved {ExportType} manifest: {ManifestUrl}", exportType, manifestUrl);

        // 4) Download the JSON manifest
        var json = await client.GetStringAsync(manifestUrl, cancellationToken);

        // Save raw manifest to disk if output directory is specified
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                var outputPath = Path.Combine(outputDir, $"{exportType.ToLowerInvariant()}_public_export.json");
                await File.WriteAllTextAsync(outputPath, json, cancellationToken);
                _logger.LogInformation("Saved raw {ExportType} export to: {OutputPath}", exportType, outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write raw {ExportType} export to {Dir}", exportType, outputDir);
            }
        }

        return json;
    }

    public async Task<List<PublicExportMod>> FetchModsAsync(string languageCode = "en", string? rawOutputDir = null, CancellationToken cancellationToken = default)
    {
        // Fetch raw JSON using the generic method
        var json = await FetchRawExportAsync("ExportUpgrades", languageCode, outputDir: null, cancellationToken);

        // Optionally save raw manifest to disk with temp_ prefix (for backward compatibility)
        if (!string.IsNullOrWhiteSpace(rawOutputDir))
        {
            try
            {
                Directory.CreateDirectory(rawOutputDir);
                var tempPath = Path.Combine(rawOutputDir, $"temp_ExportUpgrades_{languageCode}.json");
                await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write raw Public Export manifest to {Dir}", rawOutputDir);
            }
        }

        // Parse JSON and map mods
        List<PublicExportMod> mods = new List<PublicExportMod>();
        try
        {
            mods = ParseMods(json);
        }
        catch
        {
            // Fallback for NDJSON or string-wrapped lines
            // mods = FallbackParseNdjson(json);
        }
        _logger.LogInformation("Parsed {Count} mods from Public Export (excluding Rivens/Sets)", mods.Count);
        return mods;
    }

    private List<PublicExportMod> ParseMods(string json)
    {
        var result = new List<PublicExportMod>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // The structure is: { "ExportUpgrades": [ ... ] }
        if (!root.TryGetProperty("ExportUpgrades", out var upgradesArray) || 
            upgradesArray.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("ExportUpgrades property not found or not an array");
            return result;
        }

        foreach (var el in upgradesArray.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // Only include items from /Lotus/Upgrades/Mods/*
            if (!TryGetString(el, "uniqueName", out var uniqueName) || 
                !uniqueName.StartsWith("/Lotus/Upgrades/Mods/", StringComparison.Ordinal))
            {
                continue;
            }

            // Filter out Rivens and Set mods
            if (IsRiven(el) || IsSetMod(el))
            {
                continue;
            }

            result.Add(MapToPublicExportMod(el));
        }

        return result;
    }

    private bool IsRiven(JsonElement el)
    {
        if (TryGetString(el, "name", out var name) && 
            name.Contains("Riven Mod", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    private bool IsSetMod(JsonElement el)
    {
        // Set mods have upgradeEntries property
        return el.TryGetProperty("upgradeEntries", out _);
    }

    private PublicExportMod MapToPublicExportMod(JsonElement el)
    {
        var mod = new PublicExportMod
        {
            UniqueName = GetString(el, "uniqueName"),
            Name = GetString(el, "name"),
            Type = GetOptionalString(el, "type"),
            Polarity = GetOptionalString(el, "polarity"),
            Rarity = GetOptionalString(el, "rarity"),
            BaseDrain = GetOptionalInt(el, "baseDrain"),
            FusionLimit = GetOptionalInt(el, "fusionLimit"),
            IsUtility = GetOptionalBool(el, "isUtility"),
            Tags = GetOptionalStringArray(el, "tags"),
            Effects = ExtractEffects(el)
        };

        return mod;
    }

    private List<List<string>>? ExtractEffects(JsonElement el)
    {
        if (!el.TryGetProperty("levelStats", out var levelStats) || 
            levelStats.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var allLevels = new List<List<string>>();

        // Process each rank level
        foreach (var level in levelStats.EnumerateArray())
        {
            if (!level.TryGetProperty("stats", out var stats) || 
                stats.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            // Collect all stat strings for this rank level
            var statStrings = new List<string>();
            foreach (var stat in stats.EnumerateArray())
            {
                if (stat.ValueKind == JsonValueKind.String)
                {
                    var s = stat.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        // Strip Unicode escape sequences and HTML-like tags
                        var cleaned = CleanStatString(s);
                        if (!string.IsNullOrWhiteSpace(cleaned))
                        {
                            statStrings.Add(cleaned);
                        }
                    }
                }
            }

            if (statStrings.Count > 0)
            {
                allLevels.Add(statStrings);
            }
        }

        return allLevels.Count > 0 ? allLevels : null;
    }

    private static string CleanStatString(string input)
    {
        // Remove HTML-like tags (e.g., <DT_IMPACT_COLOR>, <DT_POISON_COLOR>)
        var cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"<[^>]+>", string.Empty);
        
        // The Unicode escapes like \u002B are already decoded by JSON parser, but clean up any remaining control chars
        cleaned = cleaned.Trim();
        
        return cleaned;
    }

    private static bool TryGetString(JsonElement el, string name, out string value)
    {
        value = string.Empty;
        if (el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            value = prop.GetString() ?? string.Empty;
            return true;
        }
        return false;
    }

    private static string GetString(JsonElement el, string name)
    {
        return el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? (prop.GetString() ?? string.Empty)
            : string.Empty;
    }

    private static string? GetOptionalString(JsonElement el, string name)
    {
        return el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static int? GetOptionalInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var i)) return i;
        return null;
    }

    private static double? GetOptionalDouble(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var d)) return d;
        return null;
    }

    private static bool? GetOptionalBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.True) return true;
        if (prop.ValueKind == JsonValueKind.False) return false;
        return null;
    }

    private static List<string>? GetOptionalStringArray(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
        {
            return null;
        }
        var list = new List<string>();
        foreach (var item in prop.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
        }
        return list.Count > 0 ? list : null;
    }

    /// <summary>
    /// Fetches ExportWeapons JSON and saves it to disk
    /// </summary>
    public async Task<string> FetchWeaponsAsync(string languageCode = "en", string? outputDir = null, CancellationToken cancellationToken = default)
    {
        return await FetchRawExportAsync("ExportWeapons", languageCode, outputDir, cancellationToken);
    }

    /// <summary>
    /// Fetches ExportWarframes JSON and saves it to disk
    /// </summary>
    public async Task<string> FetchWarframesAsync(string languageCode = "en", string? outputDir = null, CancellationToken cancellationToken = default)
    {
        return await FetchRawExportAsync("ExportWarframes", languageCode, outputDir, cancellationToken);
    }
}


