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

    public WikiRuleParserService(
        ILogger<WikiRuleParserService> logger,
        GameRuleRepository gameRuleRepository,
        WikiContentExtractorService contentExtractor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameRuleRepository = gameRuleRepository ?? throw new ArgumentNullException(nameof(gameRuleRepository));
        _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));
    }

    /// <summary>
    /// Parse a wiki page and generate GameRule entries
    /// </summary>
    public async Task<List<ParsedGameRule>> ParseWikiPageAsync(
        string wikiUrl,
        ILLMClient llmClient,
        CancellationToken cancellationToken = default)
    {
        // Extract page name from URL
        var pageName = _contentExtractor.ExtractPageNameFromUrl(wikiUrl);
        _logger.LogInformation("Parsing wiki page: {PageName}", pageName);

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
        List<GameRule> existingRules;
        try
        {
            existingRules = await _gameRuleRepository.LoadRulesAsync();
            _logger.LogInformation("Loaded {Count} existing rules for ID generation", existingRules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load existing rules, starting from 001");
            existingRules = new List<GameRule>();
        }
        
        // Generate GameRules with unique IDs
        var parsedRules = GenerateGameRules(pageName, characteristics, categorizations, existingRules);
        
        _logger.LogInformation("Generated {Count} game rules", parsedRules.Count);
        return parsedRules;
    }

    private async Task<RuleCategorization> CategorizeCharacteristicsAsync(
        List<WikiCharacteristic> characteristics,
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

    private string BuildCategorizationPrompt(List<WikiCharacteristic> characteristics)
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

        sb.AppendLine();
        sb.AppendLine("Provide your response as a JSON object with this structure:");
        sb.AppendLine(@"{
  ""categorizations"": [
    {
      ""index"": 0,
      ""category"": ""WeaponSpecific"",
      ""reasoning"": ""This describes the weapon's primary damage type""
    }
  ]
}");

        return sb.ToString();
    }

    /// <summary>
    /// Flatten hierarchical characteristics for LLM processing
    /// </summary>
    private List<WikiCharacteristic> FlattenCharacteristics(List<WikiCharacteristic> characteristics)
    {
        var flat = new List<WikiCharacteristic>();
        
        void Flatten(WikiCharacteristic characteristic)
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
        List<WikiCharacteristic> characteristics,
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
        WikiCharacteristic characteristic,
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
}

