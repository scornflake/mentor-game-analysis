using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for parsing Warframe wiki pages and generating GameRule entries
/// </summary>
public class WikiRuleParserService
{
    private readonly ILogger<WikiRuleParserService> _logger;
    private readonly GameRuleRepository _gameRuleRepository;
    private readonly WikiContentExtractorService _contentExtractor;
    private readonly IUserDataPathService _userDataPathService;

    public WikiRuleParserService(
        ILogger<WikiRuleParserService> logger,
        GameRuleRepository gameRuleRepository,
        WikiContentExtractorService contentExtractor,
        IUserDataPathService userDataPathService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameRuleRepository = gameRuleRepository ?? throw new ArgumentNullException(nameof(gameRuleRepository));
        _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));
        _userDataPathService = userDataPathService ?? throw new ArgumentNullException(nameof(userDataPathService));
    }

    /// <summary>
    /// Parse a wiki page and generate GameRule entries from weapon characteristics
    /// </summary>
    public async Task<List<ParsedGameRule>> ParseCharacteristicsWikiPageAsync(
        string wikiUrl,
        ILLMClient llmClient,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        // Extract page name from URL
        var pageName = _contentExtractor.ExtractPageNameFromUrl(wikiUrl);
        _logger.LogInformation("Parsing wiki page: {PageName}", pageName);

        // Check cache if enabled
        if (useCache)
        {
            var cachedResult = await TryLoadFromCacheAsync(pageName, "weapon", cancellationToken);
            if (cachedResult != null)
            {
                _logger.LogInformation("Loaded {Count} rules from cache for {PageName}", cachedResult.Count, pageName);
                return cachedResult;
            }
        }

        // Extract characteristics from wiki page
        var characteristics = await _contentExtractor.ExtractCharacteristicsFromUrlAsync(wikiUrl, cancellationToken);
        _logger.LogInformation("Extracted {Count} characteristics", characteristics.Count);

        if (characteristics.Count == 0)
        {
            _logger.LogWarning("No characteristics found on page");
            return new List<ParsedGameRule>();
        }

        // Batch categorize using LLM
        var categorizations = await CategorizeCharacteristicsAsync(characteristics, llmClient, cancellationToken);
        
        // Load existing rules from repository to generate unique IDs
        // Note: Pass empty list to get no existing rules, starting IDs from 001
        List<GameRule> existingRules = new List<GameRule>();
        _logger.LogInformation("Starting rule ID generation from 001 (no existing rules loaded)");
        
        // Generate GameRules with unique IDs
        var parsedRules = GenerateGameRules(pageName, characteristics, categorizations, existingRules);
        
        _logger.LogInformation("Generated {Count} game rules", parsedRules.Count);

        // Save to cache
        if (useCache && parsedRules.Count > 0)
        {
            await SaveToCacheAsync(pageName, "weapon", parsedRules, cancellationToken);
        }

        return parsedRules;
    }

    /// <summary>
    /// Parse a wiki page and generate GameRule entries from enemy information
    /// </summary>
    public async Task<List<ParsedGameRule>> ParseEnemyWikiPageAsync(
        string wikiUrl,
        ILLMClient llmClient,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        // Extract page name from URL
        var pageName = _contentExtractor.ExtractPageNameFromUrl(wikiUrl);
        _logger.LogInformation("Parsing enemy wiki page: {PageName}", pageName);

        // Check cache if enabled
        if (useCache)
        {
            var cachedResult = await TryLoadFromCacheAsync(pageName, "enemy", cancellationToken);
            if (cachedResult != null)
            {
                _logger.LogInformation("Loaded {Count} rules from cache for {PageName}", cachedResult.Count, pageName);
                return cachedResult;
            }
        }

        // Extract enemy information from wiki page
        var enemyInfo = await _contentExtractor.ExtractEnemyInfoFromUrlAsync(wikiUrl, llmClient, cancellationToken);
        _logger.LogInformation("Extracted {Count} enemy info items", enemyInfo.Count);

        if (enemyInfo.Count == 0)
        {
            _logger.LogWarning("No enemy information found on page");
            return new List<ParsedGameRule>();
        }

        // Batch categorize using LLM
        var categorizations = await CategorizeEnemyInfoAsync(enemyInfo, llmClient, cancellationToken);
        
        // Load existing rules from repository to generate unique IDs
        // Note: Pass empty list to get no existing rules, starting IDs from 001
        List<GameRule> existingRules = new List<GameRule>();
        _logger.LogInformation("Starting rule ID generation from 001 (no existing rules loaded)");
        
        // Generate GameRules with unique IDs
        var parsedRules = GenerateGameRules(pageName, enemyInfo, categorizations, existingRules);
        
        _logger.LogInformation("Generated {Count} game rules from enemy info", parsedRules.Count);

        // Save to cache
        if (useCache && parsedRules.Count > 0)
        {
            await SaveToCacheAsync(pageName, "enemy", parsedRules, cancellationToken);
        }

        return parsedRules;
    }

    private async Task<RuleCategorization> CategorizeCharacteristicsAsync(
        List<WikiContent> characteristics,
        ILLMClient llmClient,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Categorizing {Count} characteristics using LLM", characteristics.Count);

        // Build the categorization prompt
        var prompt = BuildCategorizationPrompt(characteristics);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"You are a Warframe game mechanics expert. Your task is to categorize game characteristics into appropriate rule categories.

Available categories:
- StatusMechanics: Rules about status effects, procs, and how they work
- DamageTypes: Rules about damage types and their effectiveness
- WeaponSpecific: Rules specific to a particular weapon's mechanics
- Synergies: Rules about how mods, abilities, or weapons synergize
- ModPriority: Rules about which mods to prioritize
- EnemyWeakness: Rules about enemy vulnerabilities
- BuildStrategy: High-level build strategy guidance

For each characteristic, provide:
1. The category that best fits
2. Brief reasoning for your choice

Respond in JSON format only."),
            new(ChatRole.User, prompt)
        };

        try
        {
            var jsonOptions = RuleEvaluationJsonSerializerContext.CreateOptions();
            var chatOptions = new ChatOptions
            {
                Temperature = 0.1f,
                ResponseFormat = ChatResponseFormat.Json
            };

            var chatResponse = await llmClient.ChatClient.GetResponseAsync<RuleCategorization>(
                messages,
                jsonOptions,
                chatOptions,
                true, // useNativeJsonSchema
                cancellationToken
            );

            var response = chatResponse.Result;

            if (response?.Categorizations == null || response.Categorizations.Count == 0)
            {
                throw new InvalidOperationException("LLM returned empty or null categorization");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to categorize characteristics with LLM");
            throw;
        }
    }

    private async Task<RuleCategorization> CategorizeEnemyInfoAsync(
        List<WikiContent> enemyInfo,
        ILLMClient llmClient,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Categorizing {Count} enemy info items using LLM", enemyInfo.Count);

        // Build the categorization prompt
        var prompt = BuildEnemyCategorizationPrompt(enemyInfo);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"You are a Warframe game mechanics expert. Your task is to categorize enemy information into appropriate rule categories.

Available categories:
- EnemyWeakness: Rules about enemy vulnerabilities and what damages them effectively
- EnemyStrength: Rules about enemy resistances and what they are strong against
- DamageTypes: Rules about specific damage types and their effectiveness
- StatusEffects: Rules about status effects and how they interact with enemies
- BuildStrategy: High-level strategy guidance for fighting these enemies

For each piece of enemy information, provide:
1. The category that best fits
2. Brief reasoning for your choice

Respond in JSON format only."),
            new(ChatRole.User, prompt)
        };

        try
        {
            var jsonOptions = RuleEvaluationJsonSerializerContext.CreateOptions();
            var chatOptions = new ChatOptions
            {
                Temperature = 0.1f,
                ResponseFormat = ChatResponseFormat.Json
            };

            var chatResponse = await llmClient.ChatClient.GetResponseAsync<RuleCategorization>(
                messages,
                jsonOptions,
                chatOptions,
                true, // useNativeJsonSchema
                cancellationToken
            );

            var response = chatResponse.Result;

            if (response?.Categorizations == null || response.Categorizations.Count == 0)
            {
                throw new InvalidOperationException("LLM returned empty or null categorization");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to categorize enemy info with LLM");
            _logger.LogError("The system prompt was: {SystemPrompt}", messages[0].Text);
            _logger.LogError("The prompt was: {Prompt}", prompt);
            throw;
        }
    }

    private string BuildCategorizationPrompt(List<WikiContent> characteristics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Categorize the following Warframe weapon characteristics:");
        sb.AppendLine();

        // Flatten the hierarchy for LLM processing
        var flatCharacteristics = FlattenCharacteristics(characteristics);
        foreach (var characteristic in flatCharacteristics)
        {
            sb.AppendLine($"[{characteristic.OriginalIndex}] {characteristic.Text}");
        }

//         sb.AppendLine();
//         sb.AppendLine("Provide your response as a JSON object with this structure:");
//         sb.AppendLine(@"{
//   ""categorizations"": [
//     {
//       ""index"": 0,
//       ""category"": ""WeaponSpecific"",
//       ""reasoning"": ""This describes the weapon's primary damage type""
//     }
//   ]
// }");

        return sb.ToString();
    }

    private string BuildEnemyCategorizationPrompt(List<WikiContent> enemyInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Categorize the following Warframe enemy information:");
        sb.AppendLine();

        // Flatten the hierarchy for LLM processing
        var flatEnemyInfo = FlattenCharacteristics(enemyInfo);
        foreach (var info in flatEnemyInfo)
        {
            sb.AppendLine($"[{info.OriginalIndex}] {info.Text}");
        }

        sb.AppendLine();
        sb.AppendLine("Provide your response as a JSON object with this structure:");
        sb.AppendLine(@"{
  ""categorizations"": [
    {
      ""index"": 0,
      ""category"": ""EnemyWeakness"",
      ""reasoning"": ""This describes what the enemy is vulnerable to""
    }
  ]
}");

        return sb.ToString();
    }

    /// <summary>
    /// Flatten hierarchical characteristics for LLM processing
    /// </summary>
    private List<WikiContent> FlattenCharacteristics(List<WikiContent> characteristics)
    {
        var flat = new List<WikiContent>();
        
        void Flatten(WikiContent characteristic)
        {
            flat.Add(characteristic);
            foreach (var child in characteristic.Children)
            {
                Flatten(child);
            }
        }

        foreach (var characteristic in characteristics)
        {
            Flatten(characteristic);
        }

        return flat;
    }


    private List<ParsedGameRule> GenerateGameRules(
        string pageName,
        List<WikiContent> characteristics,
        RuleCategorization categorizations,
        List<GameRule> existingRules)
    {
        // Build a map of category to next available ID
        var categoryCounters = BuildCategoryCounters(existingRules);

        // Create a map of index to categorization
        var categorizationMap = categorizations.Categorizations.ToDictionary(c => c.Index);

        // Build hierarchical rules
        var parsedRules = new List<ParsedGameRule>();
        foreach (var characteristic in characteristics)
        {
            var rule = BuildRuleHierarchy(characteristic, categorizationMap, categoryCounters, null);
            if (rule != null)
            {
                parsedRules.Add(rule);
            }
        }

        return parsedRules;
    }

    /// <summary>
    /// Recursively build hierarchical game rules from characteristics
    /// </summary>
    private ParsedGameRule? BuildRuleHierarchy(
        WikiContent characteristic,
        Dictionary<int, CategoryAssignment> categorizationMap,
        Dictionary<string, int> categoryCounters,
        string? parentCategory)
    {
        // Find categorization for this characteristic
        if (!categorizationMap.TryGetValue(characteristic.OriginalIndex, out var categorization))
        {
            _logger.LogWarning("Could not find categorization for index {Index}", characteristic.OriginalIndex);
            return null;
        }

        // Use parent's category if not explicitly categorized, otherwise use LLM's category
        var category = categorization.Category;
        if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(parentCategory))
        {
            category = parentCategory;
        }

        // Generate unique rule ID
        var ruleId = GenerateRuleId(category, categoryCounters);

        var rule = new ParsedGameRule
        {
            RuleId = ruleId,
            RuleText = characteristic.Text,
            Category = category
        };

        // Process children
        foreach (var child in characteristic.Children)
        {
            var childRule = BuildRuleHierarchy(child, categorizationMap, categoryCounters, category);
            if (childRule != null)
            {
                rule.Children.Add(childRule);
            }
        }

        return rule;
    }

    private Dictionary<string, int> BuildCategoryCounters(List<GameRule> existingRules)
    {
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in existingRules)
        {
            // Extract category prefix and number from RuleId
            // Example: "wf-status-001" -> category prefix is "status", number is 1
            var match = Regex.Match(rule.RuleId, @"wf-([a-z-]+)-(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var categoryPrefix = match.Groups[1].Value.ToLowerInvariant();
                var number = int.Parse(match.Groups[2].Value);

                if (!counters.ContainsKey(categoryPrefix) || counters[categoryPrefix] < number)
                {
                    counters[categoryPrefix] = number;
                }
            }
        }

        return counters;
    }

    private string GenerateRuleId(string category, Dictionary<string, int> categoryCounters)
    {
        // Convert category to kebab-case prefix
        // Examples:
        // "StatusMechanics" -> "status-mechanics"
        // "WeaponSpecific" -> "weapon-specific"
        var prefix = ConvertToKebabCase(category);

        // Get next number for this category
        if (!categoryCounters.ContainsKey(prefix))
        {
            categoryCounters[prefix] = 0;
        }

        categoryCounters[prefix]++;
        var number = categoryCounters[prefix];

        return $"wf-{prefix}-{number:D3}";
    }

    private string ConvertToKebabCase(string input)
    {
        // Insert hyphens before uppercase letters (except the first one)
        var kebabCase = Regex.Replace(input, @"(?<!^)(?=[A-Z])", "-");
        return kebabCase.ToLowerInvariant();
    }

    /// <summary>
    /// Generate a cache file name from page name and type
    /// </summary>
    private string GetCacheFileName(string pageName, string parseType)
    {
        // Sanitize pageName to be filesystem-safe
        var sanitized = Regex.Replace(pageName, @"[^a-zA-Z0-9_-]", "_");
        return $"{sanitized}_{parseType}.json";
    }

    /// <summary>
    /// Try to load parsed rules from cache
    /// </summary>
    private async Task<List<ParsedGameRule>?> TryLoadFromCacheAsync(
        string pageName,
        string parseType,
        CancellationToken cancellationToken)
    {
        try
        {
            var cachePath = Path.Combine(_userDataPathService.GetCachePath(), "WikiParser");
            var cacheFilePath = Path.Combine(cachePath, GetCacheFileName(pageName, parseType));

            if (!File.Exists(cacheFilePath))
            {
                _logger.LogDebug("Cache miss for {PageName} ({ParseType})", pageName, parseType);
                return null;
            }

            var json = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
            var jsonOptions = RuleEvaluationJsonSerializerContext.CreateOptions();
            var cachedRules = JsonSerializer.Deserialize<List<ParsedGameRule>>(json, jsonOptions);

            if (cachedRules != null && cachedRules.Count > 0)
            {
                _logger.LogInformation("Cache hit for {PageName} ({ParseType})", pageName, parseType);
                return cachedRules;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load from cache for {PageName} ({ParseType})", pageName, parseType);
            return null;
        }
    }

    /// <summary>
    /// Save parsed rules to cache
    /// </summary>
    private async Task SaveToCacheAsync(
        string pageName,
        string parseType,
        List<ParsedGameRule> rules,
        CancellationToken cancellationToken)
    {
        try
        {
            var cachePath = Path.Combine(_userDataPathService.GetCachePath(), "WikiParser");
            _userDataPathService.EnsureDirectoryExists(cachePath);

            var cacheFilePath = Path.Combine(cachePath, GetCacheFileName(pageName, parseType));
            var jsonOptions = RuleEvaluationJsonSerializerContext.CreateOptions();
            var json = JsonSerializer.Serialize(rules, jsonOptions);

            await File.WriteAllTextAsync(cacheFilePath, json, cancellationToken);
            _logger.LogInformation("Saved {Count} rules to cache for {PageName} ({ParseType})", rules.Count, pageName, parseType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save to cache for {PageName} ({ParseType})", pageName, parseType);
            // Don't throw - caching is not critical
        }
    }
}

