using Mentor.Core.Configuration;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

/// <summary>
/// Integration tests that make real API calls to OpenAI-compatible endpoints.
/// These tests only run when a valid API key is available.
/// </summary>
public class AnalysisServiceIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILoggerFactory _loggerFactory;

    public AnalysisServiceIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        
        // Set up logging bridge from Microsoft.Extensions.Logging to xUnit
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new XUnitLoggerProvider(_testOutputHelper, LogLevel.Debug));
        });
    }

    private static byte[] LoadTestImage(string filename)
    {
        var projectRoot = ApiKeyHelper.FindProjectRoot(AppContext.BaseDirectory);
        if (projectRoot == null)
        {
            throw new InvalidOperationException("Could not find project root");
        }

        var imagePath = Path.Combine(projectRoot, "tests", "media", filename);
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Test image not found: {imagePath}");
        }

        return File.ReadAllBytes(imagePath);
    }

    private static ChatClient CreateChatClient()
    {
        var apiKey = ApiKeyHelper.GetOpenAIApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key not available for integration test");
        }

        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, OpenAIConfiguration>
            {
                ["openai"] = new OpenAIConfiguration
                {
                    ApiKey = apiKey,
                    Model = "sonar",
                    BaseUrl = "https://api.perplexity.ai",
                    Timeout = 60
                }
            }
        };

        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options);
        return factory.GetProvider("openai");
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithRealImage_ReturnsValidRecommendation()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<AnalysisServiceIntegrationTests>();
        logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithRealImage_ReturnsValidRecommendation ===");
        
        var chatClient = CreateChatClient();
        var service = new AnalysisService(chatClient);
        var imageData = LoadTestImage("acceltra prime rad build.png");
        
        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = "Analyze this Warframe build screenshot and provide recommendations for improvement."
        };

        logger.LogInformation("Sending request to API with image: acceltra prime rad build.png");

        // Act
        var result = await service.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Analysis);
        Assert.NotEmpty(result.Analysis);
        Assert.NotNull(result.Summary);
        Assert.NotEmpty(result.Summary);
        Assert.NotNull(result.Recommendations);
        Assert.InRange(result.Confidence, 0.0, 1.0);
        Assert.Equal("openai", result.ProviderUsed);
        Assert.True(result.GeneratedAt <= DateTime.UtcNow);
        
        // The LLM should provide at least some analysis
        Assert.True(result.Analysis.Length > 50, "Analysis should contain meaningful content");
        
        // Output results
        logger.LogInformation("=== API Response Received ===");
        logger.LogInformation($"Provider: {result.ProviderUsed}");
        logger.LogInformation($"Confidence: {result.Confidence:P0}");
        logger.LogInformation($"Generated At: {result.GeneratedAt:u}");
        logger.LogInformation($"\n--- SUMMARY ---\n{result.Summary}\n");
        logger.LogInformation($"\n--- ANALYSIS ---\n{result.Analysis}\n");
        
        if (result.Recommendations.Any())
        {
            logger.LogInformation($"\n--- RECOMMENDATIONS ({result.Recommendations.Count}) ---");
            for (int i = 0; i < result.Recommendations.Count; i++)
            {
                var rec = result.Recommendations[i];
                logger.LogInformation($"\n{i + 1}. [{rec.Priority}] {rec.Action}");
                logger.LogInformation($"   Reasoning: {rec.Reasoning}");
                if (!string.IsNullOrWhiteSpace(rec.Context))
                {
                    logger.LogInformation($"   Context: {rec.Context}");
                }
            }
        }
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithRealImage_ParsesRecommendationsCorrectly()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<AnalysisServiceIntegrationTests>();
        logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithRealImage_ParsesRecommendationsCorrectly ===");
        
        var chatClient = CreateChatClient();
        var service = new AnalysisService(chatClient);
        var imageData = LoadTestImage("phantasma rad build.png");
        
        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = "What are the top 3 things I should do to improve this build?"
        };

        logger.LogInformation("Sending request to API with image: phantasma rad build.png");

        // Act
        var result = await service.AnalyzeAsync(request);

        // Assert - Verify the structure is correct
        Assert.NotNull(result);
        Assert.NotNull(result.Recommendations);
        
        // The LLM should provide at least one recommendation
        Assert.NotEmpty(result.Recommendations);
        
        // Each recommendation should have required fields
        foreach (var recommendation in result.Recommendations)
        {
            Assert.NotNull(recommendation.Action);
            Assert.NotEmpty(recommendation.Action);
            Assert.Contains(recommendation.Priority, new[] { Priority.High, Priority.Medium, Priority.Low });
        }
        
        // Output results
        logger.LogInformation("=== API Response Received ===");
        logger.LogInformation($"Provider: {result.ProviderUsed}");
        logger.LogInformation($"Confidence: {result.Confidence:P0}");
        logger.LogInformation($"\n--- SUMMARY ---\n{result.Summary}\n");
        logger.LogInformation($"\n--- ANALYSIS ---\n{result.Analysis}\n");
        
        if (result.Recommendations.Any())
        {
            logger.LogInformation($"\n--- TOP RECOMMENDATIONS ({result.Recommendations.Count}) ---");
            for (int i = 0; i < result.Recommendations.Count; i++)
            {
                var rec = result.Recommendations[i];
                logger.LogInformation($"\n{i + 1}. [{rec.Priority}] {rec.Action}");
                logger.LogInformation($"   Reasoning: {rec.Reasoning}");
                if (!string.IsNullOrWhiteSpace(rec.Context))
                {
                    logger.LogInformation($"   Context: {rec.Context}");
                }
            }
        }
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithTimeout_CompletesWithinReasonableTime()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<AnalysisServiceIntegrationTests>();
        logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithTimeout_CompletesWithinReasonableTime ===");
        
        var chatClient = CreateChatClient();
        var service = new AnalysisService(chatClient);
        var imageData = LoadTestImage("acceltra prime rad build.png");
        
        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = "Quick analysis: what stands out in this build?"
        };

        logger.LogInformation("Sending request to API with 30 second timeout...");
        var startTime = DateTime.UtcNow;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var result = await service.AnalyzeAsync(request, cts.Token);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(result);
        Assert.False(cts.IsCancellationRequested, "Request should complete within 30 seconds");
        
        // Output results
        logger.LogInformation($"=== API Response Received in {elapsed.TotalSeconds:F2} seconds ===");
        logger.LogInformation($"Confidence: {result.Confidence:P0}");
        logger.LogInformation($"\n--- SUMMARY ---\n{result.Summary}\n");
        logger.LogInformation($"\n--- ANALYSIS ---\n{result.Analysis}\n");
        
        if (result.Recommendations.Any())
        {
            logger.LogInformation($"\n--- RECOMMENDATIONS ({result.Recommendations.Count}) ---");
            foreach (var rec in result.Recommendations)
            {
                logger.LogInformation($"  • [{rec.Priority}] {rec.Action}");
            }
        }
    }
}

