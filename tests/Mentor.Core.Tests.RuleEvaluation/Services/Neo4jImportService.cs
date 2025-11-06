using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Neo4j.Driver;
using Microsoft.Extensions.Logging;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Mentor.Core.Tests.RuleEvaluation.Services;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

public class Neo4jImportService : IDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jImportService>? _logger;
    private readonly KeyInformationExtractor _extractor;
    private readonly FuzzyMatcher _fuzzyMatcher;
    private readonly EffectParserService _effectParser;

    public Neo4jImportService(
        string uri, 
        string username, 
        string password, 
        KeyInformationExtractor extractor,
        FuzzyMatcher fuzzyMatcher,
        EffectParserService effectParser,
        ILogger<Neo4jImportService>? logger = null)
    {
        _logger = logger;
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        _extractor = extractor;
        _fuzzyMatcher = fuzzyMatcher;
        _effectParser = effectParser;
    }

    public async Task ImportModsAsync(string jsonFilePath, bool clearExisting = false, HashSet<string>? nameFilter = null)
    {
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");
        }

        var json = await File.ReadAllTextAsync(jsonFilePath);
        var mods = JsonSerializer.Deserialize<List<PublicExportMod>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (mods == null || mods.Count == 0)
        {
            _logger?.LogWarning("No mods found in JSON file");
            return;
        }

        // Filter by name if filter is provided
        if (nameFilter != null && nameFilter.Count > 0)
        {
            var originalCount = mods.Count;
            mods = mods.Where(m => nameFilter.Contains(m.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            
            // Log warnings for names in filter that don't match any items
            var matchedNames = new HashSet<string>(mods.Select(m => m.Name), StringComparer.OrdinalIgnoreCase);
            var unmatchedNames = nameFilter.Where(n => !matchedNames.Contains(n)).ToList();
            
            if (unmatchedNames.Count > 0)
            {
                _logger?.LogWarning($"The following mod names in the filter did not match any mods: {string.Join(", ", unmatchedNames)}");
            }
            
            _logger?.LogInformation($"Filtered from {originalCount} to {mods.Count} mods");
        }

        if (mods.Count == 0)
        {
            _logger?.LogWarning("No mods match the filter criteria");
            return;
        }

        _logger?.LogInformation($"Importing {mods.Count} mods into Neo4j...");

        await using var session = _driver.AsyncSession();

        // Clear existing data if requested
        if (clearExisting)
        {
            _logger?.LogInformation("Clearing existing mod data...");
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync("MATCH (m:Mod) DETACH DELETE m");
            });
        }

        // Create indexes for better query performance
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE INDEX mod_unique_name IF NOT EXISTS FOR (m:Mod) ON (m.uniqueName)");
            await tx.RunAsync("CREATE INDEX mod_name IF NOT EXISTS FOR (m:Mod) ON (m.name)");
            await tx.RunAsync("CREATE INDEX mod_type IF NOT EXISTS FOR (m:Mod) ON (m.type)");
        });

        // Import mods in batches
        const int batchSize = 100;
        var imported = 0;

        for (int i = 0; i < mods.Count; i += batchSize)
        {
            var batch = mods.Skip(i).Take(batchSize).ToList();
            await session.ExecuteWriteAsync(async tx =>
            {
                foreach (var mod in batch)
                {
                    var parameters = new
                    {
                        uniqueName = mod.UniqueName,
                        name = mod.Name,
                        type = mod.Type,
                        polarity = mod.Polarity,
                        rarity = mod.Rarity,
                        baseDrain = mod.BaseDrain,
                        fusionLimit = mod.FusionLimit,
                        isUtility = mod.IsUtility,
                        tags = mod.Tags?.ToArray() ?? Array.Empty<string>(),
                        effects = mod.Effects?.SelectMany(e => e).ToArray() ?? Array.Empty<string>(),
                        maxRankEffects = mod.FullyRankedEffects?.ToArray() ?? Array.Empty<string>()
                    };

                    var query = @"
                        MERGE (m:Mod {uniqueName: $uniqueName})
                        SET m.name = $name,
                            m.type = $type,
                            m.polarity = $polarity,
                            m.rarity = $rarity,
                            m.baseDrain = $baseDrain,
                            m.fusionLimit = $fusionLimit,
                            m.isUtility = $isUtility,
                            m.tags = $tags,
                            m.effects = $effects,
                            m.maxRankEffects = $maxRankEffects
                        
                        WITH m
                        MERGE (t:ModType {name: m.type})
                        MERGE (m)-[:HAS_TYPE]->(t)
                        
                        WITH m
                        WHERE m.polarity IS NOT NULL
                        MERGE (p:Polarity {name: m.polarity})
                        MERGE (m)-[:HAS_POLARITY]->(p)
                        
                        WITH m
                        WHERE m.rarity IS NOT NULL
                        MERGE (r:Rarity {name: m.rarity})
                        MERGE (m)-[:HAS_RARITY]->(r)";

                    await tx.RunAsync(query, parameters);
                    imported++;

                    if (imported % 50 == 0)
                    {
                        _logger?.LogInformation($"Imported {imported}/{mods.Count} mods...");
                    }
                }
            });

            _logger?.LogInformation($"Batch imported: {imported}/{mods.Count} mods");
        }

        _logger?.LogInformation($"✓ Successfully imported {imported} mods into Neo4j");
    }

    /// <summary>
    /// Imports warframes into Neo4j.
    /// </summary>
    public async Task ImportWarframesAsync(string jsonFilePath, bool clearExisting = false, HashSet<string>? nameFilter = null)
    {
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");
        }

        var json = await File.ReadAllTextAsync(jsonFilePath);
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            _logger?.LogWarning("Warframes JSON is not an array");
            return;
        }

        var warframes = new List<JsonElement>();
        foreach (var warframe in root.EnumerateArray())
        {
            warframes.Add(warframe);
        }

        if (warframes.Count == 0)
        {
            _logger?.LogWarning("No warframes found in JSON file");
            return;
        }

        // Filter by name if filter is provided
        if (nameFilter != null && nameFilter.Count > 0)
        {
            var originalCount = warframes.Count;
            var matchedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var filteredWarframes = new List<JsonElement>();
            
            foreach (var warframe in warframes)
            {
                if (warframe.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                {
                    var name = nameProp.GetString();
                    if (!string.IsNullOrEmpty(name) && nameFilter.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        filteredWarframes.Add(warframe);
                        matchedNames.Add(name);
                    }
                }
            }
            
            warframes = filteredWarframes;
            
            // Log warnings for names in filter that don't match any items
            var unmatchedNames = nameFilter.Where(n => !matchedNames.Contains(n)).ToList();
            
            if (unmatchedNames.Count > 0)
            {
                _logger?.LogWarning($"The following warframe names in the filter did not match any warframes: {string.Join(", ", unmatchedNames)}");
            }
            
            _logger?.LogInformation($"Filtered from {originalCount} to {warframes.Count} warframes");
        }

        if (warframes.Count == 0)
        {
            _logger?.LogWarning("No warframes match the filter criteria");
            return;
        }

        _logger?.LogInformation($"Importing {warframes.Count} warframes into Neo4j...");

        await using var session = _driver.AsyncSession();

        // Clear existing data if requested
        if (clearExisting)
        {
            _logger?.LogInformation("Clearing existing warframe data...");
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync("MATCH (w:Warframe) DETACH DELETE w");
            });
        }

        // Create indexes for better query performance
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE INDEX warframe_unique_name IF NOT EXISTS FOR (w:Warframe) ON (w.uniqueName)");
            await tx.RunAsync("CREATE INDEX warframe_name IF NOT EXISTS FOR (w:Warframe) ON (w.name)");
        });

        // Import warframes in batches
        const int batchSize = 100;
        var imported = 0;

        for (int i = 0; i < warframes.Count; i += batchSize)
        {
            var batch = warframes.Skip(i).Take(batchSize).ToList();
            await session.ExecuteWriteAsync(async tx =>
            {
                foreach (var warframe in batch)
                {
                    var parameters = new
                    {
                        uniqueName = GetString(warframe, "uniqueName"),
                        name = GetString(warframe, "name"),
                        description = GetOptionalString(warframe, "description"),
                        health = GetOptionalDouble(warframe, "health"),
                        shield = GetOptionalDouble(warframe, "shield"),
                        armor = GetOptionalDouble(warframe, "armor"),
                        stamina = GetOptionalDouble(warframe, "stamina"),
                        power = GetOptionalDouble(warframe, "power"),
                        sprintSpeed = GetOptionalDouble(warframe, "sprintSpeed"),
                        masteryReq = GetOptionalInt(warframe, "masteryReq"),
                        productCategory = GetOptionalString(warframe, "productCategory")
                    };

                    var query = @"
                        MERGE (w:Warframe {uniqueName: $uniqueName})
                        SET w.name = $name,
                            w.description = $description,
                            w.health = $health,
                            w.shield = $shield,
                            w.armor = $armor,
                            w.stamina = $stamina,
                            w.power = $power,
                            w.sprintSpeed = $sprintSpeed,
                            w.masteryReq = $masteryReq,
                            w.productCategory = $productCategory";

                    await tx.RunAsync(query, parameters);
                    imported++;

                    if (imported % 50 == 0)
                    {
                        _logger?.LogInformation($"Imported {imported}/{warframes.Count} warframes...");
                    }
                }
            });

            _logger?.LogInformation($"Batch imported: {imported}/{warframes.Count} warframes");
        }

        _logger?.LogInformation($"✓ Successfully imported {imported} warframes into Neo4j");
    }

    /// <summary>
    /// Imports weapons into Neo4j.
    /// </summary>
    public async Task ImportWeaponsAsync(string jsonFilePath, bool clearExisting = false, HashSet<string>? nameFilter = null)
    {
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");
        }

        var json = await File.ReadAllTextAsync(jsonFilePath);
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            _logger?.LogWarning("Weapons JSON is not an array");
            return;
        }

        var weapons = new List<JsonElement>();
        foreach (var weapon in root.EnumerateArray())
        {
            weapons.Add(weapon);
        }

        if (weapons.Count == 0)
        {
            _logger?.LogWarning("No weapons found in JSON file");
            return;
        }

        // Filter by name if filter is provided
        if (nameFilter != null && nameFilter.Count > 0)
        {
            var originalCount = weapons.Count;
            var matchedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var filteredWeapons = new List<JsonElement>();
            
            foreach (var weapon in weapons)
            {
                if (weapon.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                {
                    var name = nameProp.GetString();
                    if (!string.IsNullOrEmpty(name) && nameFilter.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        filteredWeapons.Add(weapon);
                        matchedNames.Add(name);
                    }
                }
            }
            
            weapons = filteredWeapons;
            
            // Log warnings for names in filter that don't match any items
            var unmatchedNames = nameFilter.Where(n => !matchedNames.Contains(n)).ToList();
            
            if (unmatchedNames.Count > 0)
            {
                _logger?.LogWarning($"The following weapon names in the filter did not match any weapons: {string.Join(", ", unmatchedNames)}");
            }
            
            _logger?.LogInformation($"Filtered from {originalCount} to {weapons.Count} weapons");
        }

        if (weapons.Count == 0)
        {
            _logger?.LogWarning("No weapons match the filter criteria");
            return;
        }

        _logger?.LogInformation($"Importing {weapons.Count} weapons into Neo4j...");

        await using var session = _driver.AsyncSession();

        // Clear existing data if requested
        if (clearExisting)
        {
            _logger?.LogInformation("Clearing existing weapon data...");
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync("MATCH (w:Weapon) DETACH DELETE w");
            });
        }

        // Create indexes for better query performance
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE INDEX weapon_unique_name IF NOT EXISTS FOR (w:Weapon) ON (w.uniqueName)");
            await tx.RunAsync("CREATE INDEX weapon_name IF NOT EXISTS FOR (w:Weapon) ON (w.name)");
        });

        // Import weapons in batches
        const int batchSize = 100;
        var imported = 0;

        for (int i = 0; i < weapons.Count; i += batchSize)
        {
            var batch = weapons.Skip(i).Take(batchSize).ToList();
            await session.ExecuteWriteAsync(async tx =>
            {
                foreach (var weapon in batch)
                {
                    var parameters = new
                    {
                        uniqueName = GetString(weapon, "uniqueName"),
                        name = GetString(weapon, "name"),
                        description = GetOptionalString(weapon, "description"),
                        totalDamage = GetOptionalDouble(weapon, "totalDamage"),
                        criticalChance = GetOptionalDouble(weapon, "criticalChance"),
                        criticalMultiplier = GetOptionalDouble(weapon, "criticalMultiplier"),
                        procChance = GetOptionalDouble(weapon, "procChance"),
                        fireRate = GetOptionalDouble(weapon, "fireRate"),
                        accuracy = GetOptionalDouble(weapon, "accuracy"),
                        magazineSize = GetOptionalInt(weapon, "magazineSize"),
                        reloadTime = GetOptionalDouble(weapon, "reloadTime"),
                        multishot = GetOptionalDouble(weapon, "multishot"),
                        masteryReq = GetOptionalInt(weapon, "masteryReq"),
                        productCategory = GetOptionalString(weapon, "productCategory")
                    };

                    var query = @"
                        MERGE (w:Weapon {uniqueName: $uniqueName})
                        SET w.name = $name,
                            w.description = $description,
                            w.totalDamage = $totalDamage,
                            w.criticalChance = $criticalChance,
                            w.criticalMultiplier = $criticalMultiplier,
                            w.procChance = $procChance,
                            w.fireRate = $fireRate,
                            w.accuracy = $accuracy,
                            w.magazineSize = $magazineSize,
                            w.reloadTime = $reloadTime,
                            w.multishot = $multishot,
                            w.masteryReq = $masteryReq,
                            w.productCategory = $productCategory";

                    await tx.RunAsync(query, parameters);
                    imported++;

                    if (imported % 50 == 0)
                    {
                        _logger?.LogInformation($"Imported {imported}/{weapons.Count} weapons...");
                    }
                }
            });

            _logger?.LogInformation($"Batch imported: {imported}/{weapons.Count} weapons");
        }

        _logger?.LogInformation($"✓ Successfully imported {imported} weapons into Neo4j");
    }

    /// <summary>
    /// Creates Stat nodes from StatDefinitions.
    /// </summary>
    public async Task CreateStatNodesAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Creating Stat nodes...");

        var stats = StatDefinitions.GetAllStats();
        _logger?.LogInformation($"Loaded {stats.Count} stat definitions");

        await using var session = _driver.AsyncSession();

        // Create all Stat nodes
        await session.ExecuteWriteAsync(async tx =>
        {
            foreach (var stat in stats)
            {
                await tx.RunAsync(
                    "MERGE (s:Stat {name: $name}) SET s.description = $description, s.category = $category",
                    new { name = stat.Name, description = stat.Description, category = stat.Category });
            }
        });

        _logger?.LogInformation($"Created {stats.Count} Stat nodes");

        // Create indexes on Stat nodes
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE INDEX stat_name IF NOT EXISTS FOR (s:Stat) ON (s.name)");
            await tx.RunAsync("CREATE INDEX stat_category IF NOT EXISTS FOR (s:Stat) ON (s.category)");
        });
    }

    /// <summary>
    /// Creates Effect nodes and relationships for mods.
    /// Parses mod effects and creates Effect nodes with proper relationships to Stats.
    /// </summary>
    public async Task CreateEffectNodesAsync(
        string modsFilePath,
        HashSet<string>? modNameFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modsFilePath))
        {
            _logger?.LogWarning($"Mods file not found: {modsFilePath}");
            return;
        }

        _logger?.LogInformation("Creating Effect nodes for mods...");

        var json = await File.ReadAllTextAsync(modsFilePath, cancellationToken);
        var mods = JsonSerializer.Deserialize<List<PublicExportMod>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (mods == null || mods.Count == 0)
        {
            _logger?.LogWarning("No mods found in JSON file");
            return;
        }

        // Filter by name if filter is provided
        if (modNameFilter != null && modNameFilter.Count > 0)
        {
            var originalCount = mods.Count;
            mods = mods.Where(m => modNameFilter.Contains(m.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            _logger?.LogInformation($"Filtered mods from {originalCount} to {mods.Count} for effect creation");
        }

        if (mods.Count == 0)
        {
            _logger?.LogWarning("No mods match the filter criteria for effect creation");
            return;
        }

        await using var session = _driver.AsyncSession();

        // Create indexes for Effect nodes
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE INDEX effect_type IF NOT EXISTS FOR (e:Effect) ON (e.type)");
            await tx.RunAsync("CREATE INDEX effect_condition IF NOT EXISTS FOR (e:Effect) ON (e.condition)");
        });

        var effectsCreated = 0;
        var relationshipsCreated = 0;
        const int batchSize = 50;

        for (int i = 0; i < mods.Count; i += batchSize)
        {
            var batch = mods.Skip(i).Take(batchSize).ToList();
            await session.ExecuteWriteAsync(async tx =>
            {
                foreach (var mod in batch)
                {
                    if (mod.Effects == null || mod.Effects.Count == 0)
                    {
                        continue;
                    }

                    // Process each rank level
                    for (int rank = 0; rank < mod.Effects.Count; rank++)
                    {
                        var rankEffects = mod.Effects[rank];
                        var parsedEffects = _effectParser.ParseEffects(rankEffects);

                        // Process each effect in the rank
                        for (int effectIndex = 0; effectIndex < parsedEffects.Count; effectIndex++)
                        {
                            var effect = parsedEffects[effectIndex];
                            
                            // Create Effect node
                            var effectId = $"{mod.UniqueName}_rank{rank}_effect{effectIndex}";
                            
                            await tx.RunAsync(@"
                                MERGE (e:Effect {id: $effectId})
                                SET e.type = $type,
                                    e.value = $value,
                                    e.unit = $unit,
                                    e.condition = $condition,
                                    e.duration = $duration,
                                    e.stacking = $stacking,
                                    e.maxStacks = $maxStacks,
                                    e.stackType = $stackType,
                                    e.trigger = $trigger,
                                    e.operation = $operation,
                                    e.perUnit = $perUnit,
                                    e.consumeCondition = $consumeCondition,
                                    e.untilCondition = $untilCondition,
                                    e.willEffect = $willEffect,
                                    e.originalText = $originalText",
                                new
                                {
                                    effectId = effectId,
                                    type = effect.Type,
                                    value = effect.Value,
                                    unit = effect.Unit,
                                    condition = effect.Condition ?? "none",
                                    duration = effect.Duration,
                                    stacking = effect.Stacking,
                                    maxStacks = effect.MaxStacks,
                                    stackType = effect.StackType,
                                    trigger = effect.Trigger,
                                    operation = effect.Operation,
                                    perUnit = effect.PerUnit,
                                    consumeCondition = effect.ConsumeCondition,
                                    untilCondition = effect.UntilCondition,
                                    willEffect = effect.WillEffect,
                                    originalText = effect.OriginalText
                                });

                            effectsCreated++;

                            // Create Mod -> Effect relationship
                            await tx.RunAsync(@"
                                MATCH (m:Mod {uniqueName: $uniqueName})
                                MATCH (e:Effect {id: $effectId})
                                MERGE (m)-[r:HAS_EFFECT]->(e)
                                SET r.rank = $rank,
                                    r.effectIndex = $effectIndex",
                                new
                                {
                                    uniqueName = mod.UniqueName,
                                    effectId = effectId,
                                    rank = rank,
                                    effectIndex = effectIndex
                                });

                            // Create Effect -> Stat relationship if stat name is matched
                            if (!string.IsNullOrEmpty(effect.StatName))
                            {
                                await tx.RunAsync(@"
                                    MATCH (e:Effect {id: $effectId})
                                    MATCH (s:Stat {name: $statName})
                                    MERGE (e)-[r:MODIFIES]->(s)
                                    SET r.operation = $operation,
                                        r.condition = $condition,
                                        r.value = $value,
                                        r.unit = $unit,
                                        r.perUnit = $perUnit,
                                        r.duration = $duration,
                                        r.stacking = $stacking,
                                        r.maxStacks = $maxStacks,
                                        r.stackType = $stackType,
                                        r.consumeCondition = $consumeCondition,
                                        r.untilCondition = $untilCondition,
                                        r.willEffect = $willEffect",
                                    new
                                    {
                                        effectId = effectId,
                                        statName = effect.StatName,
                                        operation = effect.Operation,
                                        condition = effect.Condition ?? "none",
                                        value = effect.Value,
                                        unit = effect.Unit,
                                        perUnit = effect.PerUnit,
                                        duration = effect.Duration,
                                        stacking = effect.Stacking,
                                        maxStacks = effect.MaxStacks,
                                        stackType = effect.StackType,
                                        consumeCondition = effect.ConsumeCondition,
                                        untilCondition = effect.UntilCondition,
                                        willEffect = effect.WillEffect
                                    });

                                relationshipsCreated++;
                            }
                        }
                    }
                }
            });

            if (i % 500 == 0)
            {
                _logger?.LogInformation($"Created effects for {i}/{mods.Count} mods...");
            }
        }

        _logger?.LogInformation($"✓ Created {effectsCreated} Effect nodes and {relationshipsCreated} Effect->Stat relationships");
    }


    // Helper methods for JSON parsing
    private string GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private int? GetOptionalInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt32();
        }
        return null;
    }

    private double? GetOptionalDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return null;
    }

    public async Task<long> GetModCountAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (m:Mod) RETURN count(m) as count");
            var record = await cursor.SingleAsync();
            return record["count"].As<long>();
        });
        return result;
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}

