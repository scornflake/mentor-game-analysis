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

    public GameRuleRepository(ILogger<GameRuleRepository> logger, IUserDataPathService userDataPathService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userDataPathService = userDataPathService ?? throw new ArgumentNullException(nameof(userDataPathService));
    }

    public async Task<List<GameRule>> LoadRulesAsync(string gameName, List<string> ruleFiles)
    {
        if (ruleFiles == null || ruleFiles.Count == 0)
        {
            _logger.LogInformation("No rule files specified, returning empty list");
            return new List<GameRule>();
        }

        _logger.LogInformation("Loading game rules from {Count} file(s): {Files}", 
            ruleFiles.Count, string.Join(", ", ruleFiles));

        var allRules = new List<GameRule>();
        var rulesDirectory = _userDataPathService.GetRulesPath(gameName);

        foreach (var ruleFile in ruleFiles)
        {
            // Search recursively for the rule file
            var searchPattern = $"{ruleFile}.json";
            var matchingFiles = Directory.GetFiles(rulesDirectory, searchPattern, SearchOption.AllDirectories);

            if (matchingFiles.Length == 0)
            {
                _logger.LogError("Rules file not found: {FileName} in {Directory}", searchPattern, rulesDirectory);
                throw new InvalidOperationException(
                    $"Could not find rules file: {searchPattern}. " +
                    $"Please ensure {ruleFile}.json exists in the user data folder or its subfolders. " +
                    $"Searched location: {rulesDirectory}");
            }

            if (matchingFiles.Length > 1)
            {
                _logger.LogWarning("Multiple files found for {FileName}: {Files}. Using first match.", 
                    searchPattern, string.Join(", ", matchingFiles));
            }

            var rulesFilePath = matchingFiles[0];
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

            _logger.LogInformation("Loaded {Count} game rules from {FilePath}", rules.Count, rulesFilePath);
            allRules.AddRange(rules);
        }

        _logger.LogInformation("Total rules loaded: {Count}", allRules.Count);
        return allRules;
    }

    public async Task<List<GameRule>> GetRulesForGameAsync(string gameName, List<string> ruleFiles)
    {
        // Since GameName field has been removed, just return all rules
        // The gameName parameter is kept for API compatibility
        var allRules = await LoadRulesAsync(gameName, ruleFiles);
        return allRules;
    }

    public async Task<string> GetFormattedRulesAsync(string gameName, List<string> ruleFiles)
    {
        var rules = await GetRulesForGameAsync(gameName, ruleFiles);
        
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

    public async Task SaveRulesAsync(string gameName, string type, string thingName, List<GameRule> rules)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            throw new ArgumentException("Game name cannot be null or empty", nameof(gameName));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or empty", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(thingName))
        {
            throw new ArgumentException("Thing name cannot be null or empty", nameof(thingName));
        }

        if (rules == null || rules.Count == 0)
        {
            throw new ArgumentException("Rules list cannot be null or empty", nameof(rules));
        }

        // Get the rules directory from the service and add type subfolder
        var rulesDirectory = _userDataPathService.GetRulesPath(gameName);
        var typeDirectory = Path.Combine(rulesDirectory, type);
        _userDataPathService.EnsureDirectoryExists(typeDirectory);

        var filePath = Path.Combine(typeDirectory, $"{thingName}.json");
        
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

