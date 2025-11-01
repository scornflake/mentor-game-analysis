using System.Text;
using System.Text.Json;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Services;

public class GameRuleRepository
{
    private readonly ILogger<GameRuleRepository> _logger;
    private readonly IUserDataPathService _userDataPathService;
    private List<GameRule>? _cachedRules;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public GameRuleRepository(ILogger<GameRuleRepository> logger, IUserDataPathService userDataPathService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userDataPathService = userDataPathService ?? throw new ArgumentNullException(nameof(userDataPathService));
    }

    public async Task<List<GameRule>> LoadRulesAsync()
    {
        // Return cached rules if available
        if (_cachedRules != null)
        {
            return _cachedRules;
        }

        // Use lock to prevent multiple simultaneous loads
        await _loadLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedRules != null)
            {
                return _cachedRules;
            }

            _logger.LogInformation("Loading game rules from user data folder");

            // Get the rules path from the service
            var rulesDirectory = _userDataPathService.GetRulesPath("warframe");
            var rulesFilePath = Path.Combine(rulesDirectory, "WarframeRules.json");

            if (!File.Exists(rulesFilePath))
            {
                _logger.LogError("Rules file not found at: {FilePath}", rulesFilePath);
                throw new InvalidOperationException(
                    $"Could not find rules file at: {rulesFilePath}. " +
                    $"Please ensure WarframeRules.json exists in the user data folder. " +
                    $"Expected location: {rulesDirectory}");
            }

            var json = await File.ReadAllTextAsync(rulesFilePath);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var rules = JsonSerializer.Deserialize<List<GameRule>>(json, jsonOptions);
            if (rules == null)
            {
                _logger.LogError("Failed to deserialize game rules from: {FilePath}", rulesFilePath);
                throw new InvalidOperationException($"Failed to deserialize game rules from JSON at: {rulesFilePath}");
            }

            _cachedRules = rules;
            _logger.LogInformation("Loaded {Count} game rules from {FilePath}", rules.Count, rulesFilePath);

            return _cachedRules;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<List<GameRule>> GetRulesForGameAsync(string gameName)
    {
        // Since GameName field has been removed, just return all rules
        // The gameName parameter is kept for API compatibility
        var allRules = await LoadRulesAsync();
        return allRules;
    }

    public async Task<string> GetFormattedRulesAsync(string gameName)
    {
        var rules = await GetRulesForGameAsync(gameName);
        
        if (rules.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("=== GAME KNOWLEDGE RULES ===");
        sb.AppendLine($"These rules provide specific guidance for {gameName}. Apply them when relevant to the user's query.");
        sb.AppendLine();

        // Group rules by category
        var groupedRules = rules.GroupBy(r => r.Category).OrderBy(g => g.Key);

        foreach (var group in groupedRules)
        {
            sb.AppendLine($"## {group.Key}");
            foreach (var rule in group)
            {
                FormatRuleHierarchy(sb, rule, 0);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Recursively format a rule and its children with proper indentation
    /// </summary>
    private void FormatRuleHierarchy(StringBuilder sb, GameRule rule, int indentLevel)
    {
        // Create indentation (2 spaces per level)
        var indent = new string(' ', indentLevel * 2);
        sb.AppendLine($"{indent}- {rule.RuleText}");

        // Format children with increased indentation
        foreach (var child in rule.Children)
        {
            FormatRuleHierarchy(sb, child, indentLevel + 1);
        }
    }

    public async Task SaveRulesAsync(string gameName, string weaponName, List<GameRule> rules)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            throw new ArgumentException("Game name cannot be null or empty", nameof(gameName));
        }

        if (string.IsNullOrWhiteSpace(weaponName))
        {
            throw new ArgumentException("Weapon name cannot be null or empty", nameof(weaponName));
        }

        if (rules == null || rules.Count == 0)
        {
            throw new ArgumentException("Rules list cannot be null or empty", nameof(rules));
        }

        // Get the rules directory from the service
        var rulesDirectory = _userDataPathService.GetRulesPath(gameName);
        _userDataPathService.EnsureDirectoryExists(rulesDirectory);

        var filePath = Path.Combine(rulesDirectory, $"{weaponName}.json");
        
        _logger.LogInformation("Saving {Count} rules to {FilePath}", rules.Count, filePath);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(rules, jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        _logger.LogInformation("Successfully saved rules to {FilePath}", filePath);
    }
}

