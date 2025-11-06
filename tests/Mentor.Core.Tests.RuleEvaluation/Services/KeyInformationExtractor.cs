using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.Logging;
using Mentor.Core.Tests.RuleEvaluation.Services;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for extracting key information concepts from Public Export JSON files.
/// Processes mods, weapons, and warframes to identify normalized key information concepts.
/// Maintains a "known things" database to detect significant semantic changes.
/// </summary>
public class KeyInformationExtractor
{
    private readonly ILogger<KeyInformationExtractor>? _logger;
    private readonly string _knownThingsPath;
    private HashSet<string>? _knownThings;

    public KeyInformationExtractor(ILogger<KeyInformationExtractor>? logger = null, string? knownThingsPath = null)
    {
        _logger = logger;
        // Store in Models directory so it can be checked in with the code
        _knownThingsPath = knownThingsPath ?? Path.Combine("Models", "key_information_known.json");
    }

    /// <summary>
    /// Loads the "known things" database from disk.
    /// </summary>
    private async Task<HashSet<string>> LoadKnownThingsAsync(CancellationToken cancellationToken = default)
    {
        if (_knownThings != null)
        {
            return _knownThings;
        }

        if (!File.Exists(_knownThingsPath))
        {
            _logger?.LogInformation("No known things database found. First run - will create from discovered concepts.");
            _knownThings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return _knownThings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_knownThingsPath, cancellationToken);
            var knownList = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            // Normalize all loaded known concepts to ensure consistency
            var normalizedKnown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in knownList)
            {
                var normalized = NormalizeText(item);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    normalizedKnown.Add(normalized);
                }
            }
            _knownThings = normalizedKnown;
            _logger?.LogInformation($"Loaded {_knownThings.Count} known concepts from {_knownThingsPath}");
            return _knownThings;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load known things database. Will create from scratch.");
            _knownThings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return _knownThings;
        }
    }

    /// <summary>
    /// Saves the "known things" database to disk.
    /// </summary>
    private async Task SaveKnownThingsAsync(HashSet<string> knownThings, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_knownThingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sorted = knownThings.OrderBy(k => k).ToList();
            var json = JsonSerializer.Serialize(sorted, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            await File.WriteAllTextAsync(_knownThingsPath, json, cancellationToken);
            _knownThings = knownThings;
            _logger?.LogInformation($"Saved {knownThings.Count} known concepts to {_knownThingsPath}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save known things database to {Path}", _knownThingsPath);
            throw;
        }
    }

    /// <summary>
    /// Compares discovered concepts to known concepts and detects significant changes.
    /// </summary>
    public class ConceptChanges
    {
        public HashSet<string> Added { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Removed { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool HasChanges => Added.Count > 0 || Removed.Count > 0;
        public bool IsSignificant => Added.Count > 0 || Removed.Count > 0; // Can be enhanced with thresholds
    }

    /// <summary>
    /// Normalizes a concept string for consistent comparison.
    /// </summary>
    private string NormalizeConcept(string concept)
    {
        return NormalizeText(concept);
    }

    /// <summary>
    /// Normalizes a set of concepts.
    /// </summary>
    private HashSet<string> NormalizeConcepts(HashSet<string> concepts)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var concept in concepts)
        {
            var normalizedConcept = NormalizeConcept(concept);
            if (!string.IsNullOrWhiteSpace(normalizedConcept))
            {
                normalized.Add(normalizedConcept);
            }
        }
        return normalized;
    }

    /// <summary>
    /// Compares discovered concepts to known concepts.
    /// </summary>
    public async Task<ConceptChanges> CompareToKnownThingsAsync(HashSet<string> discoveredConcepts, CancellationToken cancellationToken = default)
    {
        var known = await LoadKnownThingsAsync(cancellationToken);
        
        // Normalize both sets for comparison
        var normalizedDiscovered = NormalizeConcepts(discoveredConcepts);
        var normalizedKnown = NormalizeConcepts(known);
        
        var changes = new ConceptChanges
        {
            Added = new HashSet<string>(normalizedDiscovered.Except(normalizedKnown, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase),
            Removed = new HashSet<string>(normalizedKnown.Except(normalizedDiscovered, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
        };

        return changes;
    }

    /// <summary>
    /// Extracts all unique key information concepts from the three public export files.
    /// Compares to known things and halts if significant changes are detected.
    /// </summary>
    public async Task<(HashSet<string> Concepts, bool ShouldContinue, ConceptChanges? Changes)> ExtractAllKeyInformationAsync(
        string modsFilePath,
        string weaponsFilePath,
        string warframesFilePath,
        bool forceUpdate = false,
        CancellationToken cancellationToken = default)
    {
        var allConcepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract from mods
        if (File.Exists(modsFilePath))
        {
            _logger?.LogInformation("Extracting key information from mods file...");
            var modsConcepts = await ExtractFromModsAsync(modsFilePath, cancellationToken);
            foreach (var concept in modsConcepts)
            {
                allConcepts.Add(concept);
            }
            _logger?.LogInformation($"Found {modsConcepts.Count} unique concepts from mods");
        }

        // Extract from weapons
        if (File.Exists(weaponsFilePath))
        {
            _logger?.LogInformation("Extracting key information from weapons file...");
            var weaponsConcepts = await ExtractFromWeaponsAsync(weaponsFilePath, cancellationToken);
            foreach (var concept in weaponsConcepts)
            {
                allConcepts.Add(concept);
            }
            _logger?.LogInformation($"Found {weaponsConcepts.Count} unique concepts from weapons");
        }

        // Extract from warframes
        if (File.Exists(warframesFilePath))
        {
            _logger?.LogInformation("Extracting key information from warframes file...");
            var warframesConcepts = await ExtractFromWarframesAsync(warframesFilePath, cancellationToken);
            foreach (var concept in warframesConcepts)
            {
                allConcepts.Add(concept);
            }
            _logger?.LogInformation($"Found {warframesConcepts.Count} unique concepts from warframes");
        }

        _logger?.LogInformation($"Total unique key information concepts: {allConcepts.Count}");

        // Check if this is first run (no known things DB exists)
        var isFirstRun = !File.Exists(_knownThingsPath);

        // Compare to known things (unless force update or first run)
        if (!forceUpdate && !isFirstRun)
        {
            var changes = await CompareToKnownThingsAsync(allConcepts, cancellationToken);
            if (changes.HasChanges)
            {
                return (allConcepts, false, changes);
            }
        }

        // Normalize concepts before saving to known things
        var normalizedConcepts = NormalizeConcepts(allConcepts);
        
        // Update known things if no changes, force update, or first run
        await SaveKnownThingsAsync(normalizedConcepts, cancellationToken);
        
        if (isFirstRun)
        {
            _logger?.LogInformation("First run - created known things database from discovered concepts.");
        }

        return (allConcepts, true, null);
    }

    /// <summary>
    /// Extracts key information from mods JSON file.
    /// </summary>
    public async Task<HashSet<string>> ExtractFromModsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var concepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var mods = JsonSerializer.Deserialize<List<PublicExportMod>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (mods == null)
        {
            return concepts;
        }

        foreach (var mod in mods)
        {
            if (mod.Effects != null)
            {
                foreach (var rankEffects in mod.Effects)
                {
                    foreach (var effect in rankEffects)
                    {
                        // Normalize the effect string before extracting concepts
                        // This ensures multi-line effects like "On Weak Point Kill:\n+125%..." are normalized
                        var normalizedEffect = NormalizeText(effect);
                        var extracted = ExtractConceptsFromText(normalizedEffect);
                        foreach (var concept in extracted)
                        {
                            // Normalize each extracted concept before adding
                            var normalizedConcept = NormalizeText(concept);
                            if (!string.IsNullOrWhiteSpace(normalizedConcept))
                            {
                                concepts.Add(normalizedConcept);
                            }
                        }
                    }
                }
            }
        }

        return concepts;
    }

    /// <summary>
    /// Extracts key information from weapons JSON file.
    /// </summary>
    public async Task<HashSet<string>> ExtractFromWeaponsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var concepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            return concepts;
        }

        foreach (var weapon in root.EnumerateArray())
        {
            // Extract from description
            if (weapon.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
            {
                var description = desc.GetString();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    // Normalize description before extracting concepts
                    var normalizedDescription = NormalizeText(description);
                    var extracted = ExtractConceptsFromText(normalizedDescription);
                    foreach (var concept in extracted)
                    {
                        // Normalize each extracted concept before adding
                        var normalizedConcept = NormalizeText(concept);
                        if (!string.IsNullOrWhiteSpace(normalizedConcept))
                        {
                            concepts.Add(normalizedConcept);
                        }
                    }
                }
            }

            // Extract concepts from property names (stats that exist in the data)
            ExtractConceptsFromProperties(weapon, concepts);
        }

        return concepts;
    }

    /// <summary>
    /// Extracts key information from warframes JSON file.
    /// </summary>
    public async Task<HashSet<string>> ExtractFromWarframesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var concepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            return concepts;
        }

        foreach (var warframe in root.EnumerateArray())
        {
            // Extract from description
            if (warframe.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
            {
                var description = desc.GetString();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    // Normalize description before extracting concepts
                    var normalizedDescription = NormalizeText(description);
                    var extracted = ExtractConceptsFromText(normalizedDescription);
                    foreach (var concept in extracted)
                    {
                        // Normalize each extracted concept before adding
                        var normalizedConcept = NormalizeText(concept);
                        if (!string.IsNullOrWhiteSpace(normalizedConcept))
                        {
                            concepts.Add(normalizedConcept);
                        }
                    }
                }
            }

            // Extract from abilities
            if (warframe.TryGetProperty("abilities", out var abilities) && abilities.ValueKind == JsonValueKind.Array)
            {
                foreach (var ability in abilities.EnumerateArray())
                {
                    if (ability.TryGetProperty("description", out var abilityDesc) && 
                        abilityDesc.ValueKind == JsonValueKind.String)
                    {
                        var abilityDescription = abilityDesc.GetString();
                        if (!string.IsNullOrWhiteSpace(abilityDescription))
                        {
                            // Normalize ability description before extracting concepts
                            var normalizedAbilityDesc = NormalizeText(abilityDescription);
                            var extracted = ExtractConceptsFromText(normalizedAbilityDesc);
                            foreach (var concept in extracted)
                            {
                                // Normalize each extracted concept before adding
                                var normalizedConcept = NormalizeText(concept);
                                if (!string.IsNullOrWhiteSpace(normalizedConcept))
                                {
                                    concepts.Add(normalizedConcept);
                                }
                            }
                        }
                    }
                }
            }

            // Extract concepts from property names (stats that exist in the data)
            ExtractConceptsFromProperties(warframe, concepts);
        }

        return concepts;
    }

    /// <summary>
    /// Normalizes text by removing line breaks and normalizing whitespace.
    /// Handles all variations of line breaks and whitespace characters.
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Use Regex to handle all whitespace variations comprehensively
        // This replaces ALL whitespace characters (including \r, \n, \t, \v, \f, etc.) with a single space
        var normalized = Regex.Replace(text, @"\s+", " ", RegexOptions.Compiled);

        return normalized.Trim();
    }

    /// <summary>
    /// Extracts key information concepts from text by parsing common patterns.
    /// Public method for use by other services (e.g., Neo4jImportService).
    /// </summary>
    public HashSet<string> ExtractConceptsFromText(string text)
    {
        var concepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
        {
            return concepts;
        }

        // Normalize text for processing (remove line breaks, normalize whitespace)
        var normalized = NormalizeText(text);

        // Extract damage types (with "Damage" suffix to distinguish from chances)
        ExtractDamageTypes(normalized, concepts);

        // Extract status effects
        ExtractStatusEffects(normalized, concepts);

        // Extract critical stats
        ExtractCriticalStats(normalized, concepts);

        // Extract combat stats
        ExtractCombatStats(normalized, concepts);

        // Extract defensive stats
        ExtractDefensiveStats(normalized, concepts);

        // Extract mobility stats
        ExtractMobilityStats(normalized, concepts);

        // Extract energy/ability stats
        ExtractEnergyAbilityStats(normalized, concepts);

        // Extract faction damage
        ExtractFactionDamage(normalized, concepts);

        // Extract utility
        ExtractUtility(normalized, concepts);

        // Extract special effects
        ExtractSpecialEffects(normalized, concepts);

        // Extract weapon properties
        ExtractWeaponProperties(normalized, concepts);

        // Extract conditional effects and stacking mechanics
        ExtractConditionalEffects(normalized, concepts);

        return concepts;
    }

    private void ExtractDamageTypes(string text, HashSet<string> concepts)
    {
        // Physical damage types
        if (ContainsPattern(text, @"\bimpact\b", @"damage", @"on\s+bullet\s+jump"))
            concepts.Add("Impact on Bullet Jump");
        else if (ContainsPattern(text, @"\bimpact\b", @"damage"))
            concepts.Add("Impact Damage");

        if (ContainsPattern(text, @"\bpuncture\b", @"damage", @"on\s+bullet\s+jump"))
            concepts.Add("Puncture on Bullet Jump");
        else if (ContainsPattern(text, @"\bpuncture\b", @"damage"))
            concepts.Add("Puncture Damage");

        if (ContainsPattern(text, @"\bslash\b", @"damage", @"on\s+bullet\s+jump"))
            concepts.Add("Slash on Bullet Jump");
        else if (ContainsPattern(text, @"\bslash\b", @"damage"))
            concepts.Add("Slash Damage");

        // Elemental damage types
        if (ContainsPattern(text, @"\bheat\b", @"on\s+bullet\s+jump"))
            concepts.Add("Heat on Bullet Jump");
        else if (ContainsPattern(text, @"\bheat\b"))
            concepts.Add("Heat");

        if (ContainsPattern(text, @"\bcold\b", @"on\s+bullet\s+jump"))
            concepts.Add("Cold on Bullet Jump");
        else if (ContainsPattern(text, @"\bcold\b"))
            concepts.Add("Cold");

        if (ContainsPattern(text, @"\belectricity\b", @"damage", @"on\s+bullet\s+jump"))
            concepts.Add("Electricity on Bullet Jump");
        else if (ContainsPattern(text, @"\belectricity\b", @"damage"))
            concepts.Add("Electricity");

        if (ContainsPattern(text, @"\btoxin\b", @"on\s+bullet\s+jump"))
            concepts.Add("Toxin on Bullet Jump");
        else if (ContainsPattern(text, @"\btoxin\b"))
            concepts.Add("Toxin");

        // Combined elemental damage types
        if (ContainsPattern(text, @"\bcorrosive\b", @"damage"))
            concepts.Add("Corrosive Damage");
        if (ContainsPattern(text, @"\bviral\b", @"damage"))
            concepts.Add("Viral Damage");
        if (ContainsPattern(text, @"\bradiation\b", @"damage"))
            concepts.Add("Radiation Damage");
        if (ContainsPattern(text, @"\bblast\b", @"damage"))
            concepts.Add("Blast Damage");
        if (ContainsPattern(text, @"\bmagnetic\b", @"damage"))
            concepts.Add("Magnetic Damage");
        if (ContainsPattern(text, @"\bgas\b", @"damage"))
            concepts.Add("Gas Damage");
    }

    private void ExtractStatusEffects(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"status", @"chance"))
            concepts.Add("Status Chance");
        if (ContainsPattern(text, @"status", @"duration"))
            concepts.Add("Status Duration");
        if (ContainsPattern(text, @"status", @"duration", @"on\s+self"))
            concepts.Add("Status Duration on Self");
        if (ContainsPattern(text, @"guaranteed", @"proc"))
            concepts.Add("Guaranteed Procs");
    }

    private void ExtractCriticalStats(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"critical", @"chance"))
            concepts.Add("Critical Chance");
        if (ContainsPattern(text, @"critical", @"damage"))
            concepts.Add("Critical Damage");
        if (ContainsPattern(text, @"crit", @"chance"))
            concepts.Add("Critical Chance");
        if (ContainsPattern(text, @"crit", @"damage"))
            concepts.Add("Critical Damage");
    }

    private void ExtractCombatStats(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"fire", @"rate"))
            concepts.Add("Fire Rate");
        if (ContainsPattern(text, @"reload", @"speed"))
            concepts.Add("Reload Speed");
        if (ContainsPattern(text, @"magazine", @"capacity"))
            concepts.Add("Magazine Capacity");
        if (ContainsPattern(text, @"multishot"))
            concepts.Add("Multishot");
        if (ContainsPattern(text, @"damage", @"falloff"))
            concepts.Add("Damage Falloff");
        if (ContainsPattern(text, @"punch", @"through"))
            concepts.Add("Punch Through");
        if (ContainsPattern(text, @"\brange\b"))
            concepts.Add("Range");
        if (ContainsPattern(text, @"\baccuracy\b"))
            concepts.Add("Accuracy");
        if (ContainsPattern(text, @"\brecoil\b"))
            concepts.Add("Recoil");
        if (ContainsPattern(text, @"weapon", @"recoil"))
            concepts.Add("Weapon Recoil");
        if (ContainsPattern(text, @"projectile", @"speed"))
            concepts.Add("Projectile Speed");
    }

    private void ExtractDefensiveStats(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"\bhealth\b"))
            concepts.Add("Health");
        if (ContainsPattern(text, @"\bshield\b"))
            concepts.Add("Shield");
        if (ContainsPattern(text, @"shield", @"capacity"))
            concepts.Add("Shield Capacity");
        if (ContainsPattern(text, @"shield", @"recharge"))
            concepts.Add("Shield Recharge");
        if (ContainsPattern(text, @"shield", @"recharge", @"delay"))
            concepts.Add("Shield Recharge Delay");
        if (ContainsPattern(text, @"\barmor\b"))
            concepts.Add("Armor");
        if (ContainsPattern(text, @"damage", @"reduction"))
            concepts.Add("Damage Reduction");
        if (ContainsPattern(text, @"\bresistance\b"))
            concepts.Add("Damage Resistance");
    }

    private void ExtractMobilityStats(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"sprint", @"speed"))
            concepts.Add("Sprint Speed");
        if (ContainsPattern(text, @"parkour", @"velocity"))
            concepts.Add("Parkour Velocity");
        if (ContainsPattern(text, @"aim", @"glide", @"wall", @"latch", @"duration"))
            concepts.Add("Aim Glide/Wall Latch Duration");
        if (ContainsPattern(text, @"\bslide\b"))
            concepts.Add("Slide");
        if (ContainsPattern(text, @"\bfriction\b"))
            concepts.Add("Friction");
        if (ContainsPattern(text, @"casting", @"speed"))
            concepts.Add("Casting Speed");
    }

    private void ExtractEnergyAbilityStats(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"\benergy\b"))
            concepts.Add("Energy");
        if (ContainsPattern(text, @"energy", @"max"))
            concepts.Add("Energy Max");
        if (ContainsPattern(text, @"maximum", @"energy"))
            concepts.Add("Maximum Energy");
        if (ContainsPattern(text, @"ability", @"strength"))
            concepts.Add("Ability Strength");
        if (ContainsPattern(text, @"ability", @"duration"))
            concepts.Add("Ability Duration");
        if (ContainsPattern(text, @"ability", @"range"))
            concepts.Add("Ability Range");
        if (ContainsPattern(text, @"ability", @"efficiency"))
            concepts.Add("Ability Efficiency");
    }

    private void ExtractFactionDamage(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"damage", @"to", @"grineer"))
            concepts.Add("Damage to Grineer");
        if (ContainsPattern(text, @"damage", @"to", @"corpus"))
            concepts.Add("Damage to Corpus");
        if (ContainsPattern(text, @"damage", @"to", @"infested"))
            concepts.Add("Damage to Infested");
        if (ContainsPattern(text, @"damage", @"to", @"orokin"))
            concepts.Add("Damage to Orokin");
    }

    private void ExtractUtility(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"loot", @"radar"))
            concepts.Add("Loot Radar");
        if (ContainsPattern(text, @"enemy", @"radar"))
            concepts.Add("Enemy Radar");
        if (ContainsPattern(text, @"\bhacking\b"))
            concepts.Add("Hacking Time");
    }

    private void ExtractSpecialEffects(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"finisher", @"attack"))
            concepts.Add("Finisher Attacks");
        if (ContainsPattern(text, @"stagger", @"on", @"block"))
            concepts.Add("Stagger on Block");
        if (ContainsPattern(text, @"stun", @"on", @"block"))
            concepts.Add("Stun on Block");
        if (ContainsPattern(text, @"self", @"stagger"))
            concepts.Add("Self Stagger");
        if (ContainsPattern(text, @"headshot", @"multiplier"))
            concepts.Add("Headshot Multiplier");
        if (ContainsPattern(text, @"chance", @"to", @"resist", @"knockdown"))
            concepts.Add("Chance to Resist Knockdown");
        if (ContainsPattern(text, @"\bknockdown\b"))
            concepts.Add("Knockdown");
    }

    private void ExtractWeaponProperties(string text, HashSet<string> concepts)
    {
        if (ContainsPattern(text, @"arming", @"distance"))
            concepts.Add("Arming Distance");
        if (ContainsPattern(text, @"explosion", @"radius"))
            concepts.Add("Explosion Radius");
        if (ContainsPattern(text, @"bounce", @"count"))
            concepts.Add("Bounce Count");
        if (ContainsPattern(text, @"travel", @"time"))
            concepts.Add("Travel Time");
        if (ContainsPattern(text, @"ammo", @"consumption"))
            concepts.Add("Ammo Consumption");
        if (ContainsPattern(text, @"ammo", @"mutation"))
            concepts.Add("Ammo Mutation");
        if (ContainsPattern(text, @"ammo", @"pickup"))
            concepts.Add("Ammo Pickup");
    }

    /// <summary>
    /// Extracts conditional effects and stacking mechanics from text.
    /// These are important gameplay mechanics that trigger under specific conditions.
    /// </summary>
    private void ExtractConditionalEffects(string text, HashSet<string> concepts)
    {
        // Conditional triggers
        if (ContainsPattern(text, @"on", @"kill"))
            concepts.Add("On Kill");
        if (ContainsPattern(text, @"on", @"hit"))
            concepts.Add("On Hit");
        if (ContainsPattern(text, @"on", @"headshot"))
            concepts.Add("On Headshot");
        if (ContainsPattern(text, @"on", @"status", @"effect"))
            concepts.Add("On Status Effect");
        if (ContainsPattern(text, @"on", @"crit"))
            concepts.Add("On Critical Hit");
        if (ContainsPattern(text, @"on", @"critical", @"hit"))
            concepts.Add("On Critical Hit");
        if (ContainsPattern(text, @"on", @"headshot", @"kill"))
            concepts.Add("On Headshot Kill");
        if (ContainsPattern(text, @"on", @"weak", @"point", @"kill"))
            concepts.Add("On Weak Point Kill");
        if (ContainsPattern(text, @"on", @"block"))
            concepts.Add("On Block");
        if (ContainsPattern(text, @"on", @"parry"))
            concepts.Add("On Parry");
        if (ContainsPattern(text, @"on", @"slide"))
            concepts.Add("On Slide");
        if (ContainsPattern(text, @"on", @"jump", @"kick"))
            concepts.Add("On Jump Kick");
        if (ContainsPattern(text, @"on", @"bullet", @"jump"))
            concepts.Add("On Bullet Jump");

        // Stacking mechanics
        if (ContainsPattern(text, @"stack", @"up", @"to"))
            concepts.Add("Stacking");
        if (ContainsPattern(text, @"stacks", @"up", @"to"))
            concepts.Add("Stacking");
        if (ContainsPattern(text, @"stacks"))
            concepts.Add("Stacking");
        if (ContainsPattern(text, @"per", @"stack"))
            concepts.Add("Per Stack");
        if (ContainsPattern(text, @"per", @"status", @"type"))
            concepts.Add("Per Status Type");
        if (ContainsPattern(text, @"per", @"status", @"effect"))
            concepts.Add("Per Status Effect");
        if (ContainsPattern(text, @"per", @"kill"))
            concepts.Add("Per Kill");
        if (ContainsPattern(text, @"per", @"hit"))
            concepts.Add("Per Hit");
        if (ContainsPattern(text, @"per", @"headshot"))
            concepts.Add("Per Headshot");
        if (ContainsPattern(text, @"per", @"crit"))
            concepts.Add("Per Critical Hit");
        if (ContainsPattern(text, @"per", @"critical", @"hit"))
            concepts.Add("Per Critical Hit");

        // Duration-based effects (check for "for Xs" or "for X seconds" pattern)
        if (Regex.IsMatch(text, @"for\s+\d+\s*s", RegexOptions.IgnoreCase))
            concepts.Add("Duration Based");
        if (Regex.IsMatch(text, @"for\s+\d+\s*seconds", RegexOptions.IgnoreCase))
            concepts.Add("Duration Based");
    }

    /// <summary>
    /// Extracts key information concepts from JSON property names.
    /// Maps property names to concept strings.
    /// Public method for use by other services (e.g., Neo4jImportService).
    /// </summary>
    public void ExtractConceptsFromProperties(JsonElement element, HashSet<string> concepts)
    {
        // Map of property names to concept strings
        var propertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Weapon properties
            { "criticalChance", "Critical Chance" },
            { "criticalMultiplier", "Critical Damage" },
            { "procChance", "Status Chance" },
            { "fireRate", "Fire Rate" },
            { "reloadTime", "Reload Speed" },
            { "magazineSize", "Magazine Capacity" },
            { "multishot", "Multishot" },
            { "accuracy", "Accuracy" },
            { "range", "Range" },
            
            // Warframe properties
            { "health", "Health" },
            { "shield", "Shield" },
            { "armor", "Armor" },
            { "power", "Energy" },
            { "energy", "Energy" },
            { "sprintSpeed", "Sprint Speed" },
            
            // Damage per shot indicates damage types exist
            { "damagePerShot", "Damage" },
            { "totalDamage", "Damage" },
        };

        foreach (var property in element.EnumerateObject())
        {
            // Check if the property name maps to a concept
            if (propertyMap.TryGetValue(property.Name, out var concept))
            {
                concepts.Add(concept);
            }

            // Special handling for damagePerShot array
            if (property.Name.Equals("damagePerShot", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Array)
            {
                // If damagePerShot array exists, it implies various damage types are possible
                // The specific types are extracted from descriptions, but we can indicate damage exists
                concepts.Add("Damage");
            }
        }
    }

    /// <summary>
    /// Generates StatDefinitions.cs file from discovered concepts.
    /// Note: This method is kept for backward compatibility but StatDefinitions is now a static class.
    /// </summary>
    public async Task GenerateKeyInformationClassAsync(HashSet<string> concepts, string outputPath, CancellationToken cancellationToken = default)
    {
        var sortedConcepts = concepts.OrderBy(c => c).ToList();
        
        var sb = new StringBuilder();
        sb.AppendLine("namespace Mentor.Core.Tests.RuleEvaluation.Models;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Static class containing all key information constants used to bind entities (mods, warframes, weapons).");
        sb.AppendLine("/// These constants represent normalized, canonical forms of game mechanics concepts.");
        sb.AppendLine("/// This file is auto-generated from Public Export data. Do not edit manually.");
        sb.AppendLine($"/// Written: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (Local Time)");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class KeyInformation");
        sb.AppendLine("{");
        
        // Group concepts by category for better organization
        var damageTypes = new List<string>();
        var statusEffects = new List<string>();
        var criticalStats = new List<string>();
        var combatStats = new List<string>();
        var defensiveStats = new List<string>();
        var mobilityStats = new List<string>();
        var energyAbilityStats = new List<string>();
        var factionDamage = new List<string>();
        var utility = new List<string>();
        var specialEffects = new List<string>();
        var weaponProperties = new List<string>();
        var conditionalEffects = new List<string>();
        var other = new List<string>();

        foreach (var concept in sortedConcepts)
        {
            var lower = concept.ToLowerInvariant();
            if (lower.Contains("damage") && (lower.Contains("impact") || lower.Contains("puncture") || lower.Contains("slash") || 
                lower.Contains("heat") || lower.Contains("cold") || lower.Contains("electricity") || lower.Contains("toxin") ||
                lower.Contains("corrosive") || lower.Contains("viral") || lower.Contains("radiation") || lower.Contains("blast") ||
                lower.Contains("magnetic") || lower.Contains("gas") || lower == "damage"))
            {
                damageTypes.Add(concept);
            }
            else if (lower.Contains("status") || lower.Contains("proc"))
            {
                statusEffects.Add(concept);
            }
            else if (lower.Contains("critical") || lower.Contains("crit"))
            {
                criticalStats.Add(concept);
            }
            else if (lower.Contains("fire") || lower.Contains("reload") || lower.Contains("magazine") || lower.Contains("multishot") ||
                     lower.Contains("falloff") || lower.Contains("punch") || lower.Contains("range") || lower.Contains("accuracy") ||
                     lower.Contains("recoil") || lower.Contains("projectile"))
            {
                combatStats.Add(concept);
            }
            else if (lower.Contains("health") || lower.Contains("shield") || lower.Contains("armor") || lower.Contains("resistance") ||
                     lower.Contains("reduction"))
            {
                defensiveStats.Add(concept);
            }
            else if (lower.Contains("sprint") || lower.Contains("parkour") || lower.Contains("glide") || lower.Contains("slide") ||
                     lower.Contains("friction") || lower.Contains("casting"))
            {
                mobilityStats.Add(concept);
            }
            else if (lower.Contains("energy") || lower.Contains("ability") || lower.Contains("maximum"))
            {
                energyAbilityStats.Add(concept);
            }
            else if (lower.Contains("grineer") || lower.Contains("corpus") || lower.Contains("infested") || lower.Contains("orokin"))
            {
                factionDamage.Add(concept);
            }
            else if (lower.Contains("radar") || lower.Contains("hacking"))
            {
                utility.Add(concept);
            }
            else if (lower.Contains("finisher") || lower.Contains("stagger") || lower.Contains("stun") || lower.Contains("headshot") ||
                     lower.Contains("knockdown"))
            {
                specialEffects.Add(concept);
            }
            else if (lower.Contains("arming") || lower.Contains("explosion") || lower.Contains("bounce") || lower.Contains("travel") ||
                     lower.Contains("ammo"))
            {
                weaponProperties.Add(concept);
            }
            else if (lower.StartsWith("on ") || lower.StartsWith("per ") || lower.Contains("stacking") || 
                     lower.Contains("duration based"))
            {
                conditionalEffects.Add(concept);
            }
            else
            {
                other.Add(concept);
            }
        }

        // Generate constants grouped by category
        void WriteCategory(StringBuilder sb, string categoryName, List<string> items, string comment)
        {
            if (items.Count == 0) return;
            
            sb.AppendLine($"    // ============================================================================");
            sb.AppendLine($"    // {categoryName}");
            sb.AppendLine($"    // ============================================================================");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(comment))
            {
                sb.AppendLine($"    // {comment}");
            }
            sb.AppendLine();
            
            foreach (var item in items.OrderBy(i => i))
            {
                var constantName = ToConstantName(item);
                sb.AppendLine($"    public const string {constantName} = \"{item}\";");
            }
            sb.AppendLine();
        }

        WriteCategory(sb, "DAMAGE TYPES", damageTypes, "");
        WriteCategory(sb, "STATUS EFFECTS", statusEffects, "");
        WriteCategory(sb, "CRITICAL STATS", criticalStats, "");
        WriteCategory(sb, "COMBAT STATS", combatStats, "");
        WriteCategory(sb, "DEFENSIVE STATS", defensiveStats, "");
        WriteCategory(sb, "MOBILITY STATS", mobilityStats, "");
        WriteCategory(sb, "ENERGY/ABILITY STATS", energyAbilityStats, "");
        WriteCategory(sb, "FACTION DAMAGE", factionDamage, "");
        WriteCategory(sb, "UTILITY", utility, "");
        WriteCategory(sb, "SPECIAL EFFECTS", specialEffects, "");
        WriteCategory(sb, "WEAPON PROPERTIES", weaponProperties, "");
        WriteCategory(sb, "CONDITIONAL EFFECTS", conditionalEffects, "");
        
        if (other.Count > 0)
        {
            WriteCategory(sb, "OTHER", other, "");
        }

        sb.AppendLine("}");

        await File.WriteAllTextAsync(outputPath, sb.ToString(), cancellationToken);
        _logger?.LogInformation($"Generated KeyInformation.cs with {sortedConcepts.Count} constants at: {outputPath}");
    }

    /// <summary>
    /// Converts a concept string to a valid C# constant name.
    /// </summary>
    private string ToConstantName(string concept)
    {
        // Remove special characters and convert to PascalCase
        var parts = concept.Split(new[] { ' ', '/', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
        var name = string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
        
        // Ensure it starts with a letter
        if (char.IsDigit(name[0]))
        {
            name = "_" + name;
        }
        
        return name;
    }

    /// <summary>
    /// Checks if text contains all the specified patterns (case-insensitive).
    /// </summary>
    private bool ContainsPattern(string text, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (!Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }
        }
        return true;
    }
}

