using System.Diagnostics;
using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Harness for running dual analysis (with/without rules) to compare results
/// </summary>
public class RuleComparisonHarness
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger _logger;
    private readonly MetricsCalculator _metricsCalculator;

    public RuleComparisonHarness(
        ILLMProviderFactory providerFactory,
        ILogger logger)
    {
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCalculator = new MetricsCalculator();
    }

    /// <summary>
    /// Runs comparison analysis on a single screenshot
    /// </summary>
    public async Task<ComparisonMetrics> CompareAnalysisAsync(
        string screenshotPath,
        ProviderConfigurationEntity providerConfig,
        string prompt = "Analyze this Warframe build and suggest improvements",
        List<string>? ruleFiles = null,
        List<EvaluationCriterion>? evaluationCriteria = null,
        ILLMClient? evaluatorClient = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(screenshotPath))
        {
            throw new FileNotFoundException($"Screenshot not found: {screenshotPath}");
        }

        _logger.LogInformation("Starting comparison for screenshot: {Path}", screenshotPath);

        // Load image once
        var imageBytes = await File.ReadAllBytesAsync(screenshotPath, cancellationToken);
        var mimeType = DetermineMimeType(screenshotPath);
        var imageData = new RawImage(imageBytes, mimeType);

        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = prompt,
            GameName = "Warframe",
            RuleFiles = ruleFiles ?? new List<string>()
        };

        // Run baseline analysis (no rules)
        var baselineResult = await RunAnalysisAsync(providerConfig, request, rulesEnabled: false, cancellationToken);
        
        // Run rule-augmented analysis
        var ruleAugmentedResult = await RunAnalysisAsync(providerConfig, request, rulesEnabled: true, cancellationToken);

        // Calculate metrics for both
        var baselineMetrics = _metricsCalculator.CalculateAllMetrics(baselineResult.Recommendation);
        var ruleMetrics = _metricsCalculator.CalculateAllMetrics(ruleAugmentedResult.Recommendation);

        var comparison = new ComparisonMetrics
        {
            ScreenshotPath = screenshotPath,
            
            // Store full recommendations for detailed review
            BaselineRecommendation = baselineResult.Recommendation,
            RuleAugmentedRecommendation = ruleAugmentedResult.Recommendation,
            
            BaselineSpecificityScore = baselineMetrics.specificity,
            BaselineTerminologyScore = baselineMetrics.terminology,
            BaselineActionabilityScore = baselineMetrics.actionability,
            BaselineConfidence = baselineResult.Recommendation.Confidence,
            BaselineRecommendationCount = baselineResult.Recommendation.Recommendations.Count,
            BaselineDuration = baselineResult.Duration,
            
            RuleAugmentedSpecificityScore = ruleMetrics.specificity,
            RuleAugmentedTerminologyScore = ruleMetrics.terminology,
            RuleAugmentedActionabilityScore = ruleMetrics.actionability,
            RuleAugmentedConfidence = ruleAugmentedResult.Recommendation.Confidence,
            RuleAugmentedRecommendationCount = ruleAugmentedResult.Recommendation.Recommendations.Count,
            RuleAugmentedDuration = ruleAugmentedResult.Duration
        };

        // Run subjective evaluation if criteria and evaluator provided
        if (evaluationCriteria != null && evaluationCriteria.Count > 0 && evaluatorClient != null)
        {
            _logger.LogInformation("Running subjective evaluation for both baseline and rule-augmented");
            var evaluationService = new SubjectiveEvaluationService(_logger);
            
            comparison.BaselineSubjectiveEvaluation = await evaluationService.EvaluateAsync(
                baselineResult.Recommendation,
                evaluationCriteria,
                evaluatorClient,
                cancellationToken
            );
            
            comparison.RuleAugmentedSubjectiveEvaluation = await evaluationService.EvaluateAsync(
                ruleAugmentedResult.Recommendation,
                evaluationCriteria,
                evaluatorClient,
                cancellationToken
            );
            
            if (comparison.SubjectiveScoreDelta.HasValue)
            {
                _logger.LogInformation("Subjective score delta: {Delta:F2}", comparison.SubjectiveScoreDelta.Value);
            }
        }

        _logger.LogInformation("Comparison complete. Overall improvement score: {Score:F3}", comparison.OverallImprovementScore);

        return comparison;
    }

    /// <summary>
    /// Runs comparison across multiple screenshots
    /// </summary>
    public async Task<List<ComparisonMetrics>> CompareBatchAsync(
        IEnumerable<string> screenshotPaths,
        ProviderConfigurationEntity providerConfig,
        string prompt = "Analyze this Warframe build and suggest improvements",
        List<string>? ruleFiles = null,
        List<EvaluationCriterion>? evaluationCriteria = null,
        ILLMClient? evaluatorClient = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ComparisonMetrics>();

        foreach (var path in screenshotPaths)
        {
            try
            {
                var comparison = await CompareAnalysisAsync(path, providerConfig, prompt, ruleFiles, evaluationCriteria, evaluatorClient, cancellationToken);
                results.Add(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process screenshot: {Path}", path);
            }
        }

        return results;
    }

    private async Task<AnalysisResult> RunAnalysisAsync(
        ProviderConfigurationEntity providerConfig,
        AnalysisRequest request,
        bool rulesEnabled,
        CancellationToken cancellationToken)
    {
        // Create a copy of the config with the desired rules setting
        var configCopy = new ProviderConfigurationEntity
        {
            Id = providerConfig.Id,
            Name = providerConfig.Name,
            ProviderType = providerConfig.ProviderType,
            ApiKey = providerConfig.ApiKey,
            Model = providerConfig.Model,
            BaseUrl = providerConfig.BaseUrl,
            Timeout = providerConfig.Timeout,
            RetrievalAugmentedGeneration = providerConfig.RetrievalAugmentedGeneration,
            UseGameRules = rulesEnabled,
            CreatedAt = providerConfig.CreatedAt
        };

        var llmClient = _providerFactory.GetProvider(configCopy);
        var analysisService = _providerFactory.GetAnalysisService(llmClient);

        var stopwatch = Stopwatch.StartNew();
        var recommendation = await analysisService.AnalyzeAsync(request, null, null, cancellationToken);
        stopwatch.Stop();

        return new AnalysisResult
        {
            Recommendation = recommendation,
            RulesEnabled = rulesEnabled,
            Timestamp = DateTime.UtcNow,
            Duration = stopwatch.Elapsed,
            ProviderName = providerConfig.Name,
            ScreenshotPath = request.ImageData?.SizeInBytes.ToString() ?? "unknown"
        };
    }

    private static string DetermineMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "image/png" // default
        };
    }
}

