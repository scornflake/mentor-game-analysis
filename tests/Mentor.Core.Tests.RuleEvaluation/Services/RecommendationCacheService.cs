using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for caching recommendation results to avoid regenerating LLM analysis
/// </summary>
public class RecommendationCacheService
{
    private readonly IUserDataPathService _userDataPathService;
    private readonly ILogger<RecommendationCacheService>? _logger;
    private readonly string _cacheDirectory;

    public RecommendationCacheService(
        IUserDataPathService userDataPathService,
        ILogger<RecommendationCacheService>? logger = null)
    {
        _userDataPathService = userDataPathService ?? throw new ArgumentNullException(nameof(userDataPathService));
        _logger = logger;
        
        // Use 'evaluation' subfolder under user data path
        _cacheDirectory = Path.Combine(_userDataPathService.GetBasePath(), "evaluation");
        _userDataPathService.EnsureDirectoryExists(_cacheDirectory);
    }

    /// <summary>
    /// Attempts to retrieve a cached recommendation
    /// </summary>
    /// <returns>Cached recommendation if found, null otherwise</returns>
    public async Task<Recommendation?> TryGetCachedAsync(
        string screenshotPath,
        string prompt,
        string providerName,
        bool rulesEnabled,
        List<string>? ruleFiles)
    {
        var cacheKey = GetCacheKey(screenshotPath, prompt, providerName, rulesEnabled, ruleFiles);
        var cacheFile = GetCacheFilePath(cacheKey, rulesEnabled);

        if (!File.Exists(cacheFile))
        {
            _logger?.LogDebug("Cache miss: {CacheFile}", cacheFile);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(cacheFile);
            var recommendation = JsonSerializer.Deserialize<Recommendation>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger?.LogInformation("Cache hit: {CacheFile}", Path.GetFileName(cacheFile));
            return recommendation;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to deserialize cached recommendation: {CacheFile}", cacheFile);
            return null;
        }
    }

    /// <summary>
    /// Saves a recommendation to cache
    /// </summary>
    public async Task SaveToCacheAsync(
        Recommendation recommendation,
        string screenshotPath,
        string prompt,
        string providerName,
        bool rulesEnabled,
        List<string>? ruleFiles)
    {
        var cacheKey = GetCacheKey(screenshotPath, prompt, providerName, rulesEnabled, ruleFiles);
        var cacheFile = GetCacheFilePath(cacheKey, rulesEnabled);

        try
        {
            var json = JsonSerializer.Serialize(recommendation, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(cacheFile, json);
            _logger?.LogDebug("Saved to cache: {CacheFile}", Path.GetFileName(cacheFile));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save recommendation to cache: {CacheFile}", cacheFile);
        }
    }

    /// <summary>
    /// Generates a cache key from the analysis parameters
    /// </summary>
    private string GetCacheKey(
        string screenshotPath,
        string prompt,
        string providerName,
        bool rulesEnabled,
        List<string>? ruleFiles)
    {
        // Normalize screenshot path to absolute path for consistency
        var absolutePath = Path.GetFullPath(screenshotPath);
        
        // Build a composite key from all parameters that affect the recommendation
        var keyComponents = new StringBuilder();
        keyComponents.Append(absolutePath);
        keyComponents.Append('|');
        keyComponents.Append(prompt);
        keyComponents.Append('|');
        keyComponents.Append(providerName);
        keyComponents.Append('|');
        keyComponents.Append(rulesEnabled);
        
        if (ruleFiles != null && ruleFiles.Count > 0)
        {
            keyComponents.Append('|');
            keyComponents.Append(string.Join(",", ruleFiles.OrderBy(r => r)));
        }

        // Hash the composite key to create a fixed-length cache key
        var keyString = keyComponents.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the full path to a cache file
    /// </summary>
    private string GetCacheFilePath(string cacheKey, bool rulesEnabled)
    {
        var suffix = rulesEnabled ? "ruleaugmented" : "baseline";
        return Path.Combine(_cacheDirectory, $"{cacheKey}_{suffix}.json");
    }
}

