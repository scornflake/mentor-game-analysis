using Mentor.Core.Configuration;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

/// <summary>
/// Integration tests that make real API calls to OpenAI-compatible endpoints.
/// These tests only run when a valid API key is available.
/// </summary>
public class AnalysisServiceIntegrationTests
{
    private readonly Mock<IWebsearch> _webSearch;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalysisServiceIntegrationTests> _logger;

    public AnalysisServiceIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _webSearch = new Mock<IWebsearch>();
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        TestHelpers.AddWebSearchTool(_webSearch.Object);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<AnalysisServiceIntegrationTests>>();
    }

    private  byte[] LoadTestImage(string filename)
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

    private IChatClient CreateChatClient()
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

        var factory = new LLMProviderFactory(options, _webSearch.Object, _serviceProvider);
        return factory.GetProvider("openai");
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithRealImage_ReturnsValidRecommendation()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithRealImage_ReturnsValidRecommendation ===");
        
        var chatClient = CreateChatClient();
        var service = new AnalysisService(chatClient);
        var imageData = LoadTestImage("acceltra prime rad build.png");
        
        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = "Analyze this Warframe build screenshot and provide recommendations for improvement."
        };

        _logger.LogInformation("Sending request to API with image: acceltra prime rad build.png");

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
        _logger.LogInformation("=== API Response Received ===");
        _logger.LogInformation($"Provider: {result.ProviderUsed}");
        _logger.LogInformation($"Confidence: {result.Confidence:P0}");
        _logger.LogInformation($"Generated At: {result.GeneratedAt:u}");
        _logger.LogInformation($"\n--- SUMMARY ---\n{result.Summary}\n");
        _logger.LogInformation($"\n--- ANALYSIS ---\n{result.Analysis}\n");
        
        if (result.Recommendations.Any())
        {
            _logger.LogInformation($"\n--- RECOMMENDATIONS ({result.Recommendations.Count}) ---");
            for (int i = 0; i < result.Recommendations.Count; i++)
            {
                var rec = result.Recommendations[i];
                _logger.LogInformation($"\n{i + 1}. [{rec.Priority}] {rec.Action}");
                _logger.LogInformation($"   Reasoning: {rec.Reasoning}");
                if (!string.IsNullOrWhiteSpace(rec.Context))
                {
                    _logger.LogInformation($"   Context: {rec.Context}");
                }
            }
        }
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithRealImage_ParsesRecommendationsCorrectly()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithRealImage_ParsesRecommendationsCorrectly ===");
        
        var chatClient = CreateChatClient();
        var service = new AnalysisService(chatClient);
        var imageData = LoadTestImage("phantasma rad build.png");
        
        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = "What are the top 3 things I should do to improve this build?"
        };

        _logger.LogInformation("Sending request to API with image: phantasma rad build.png");

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
        _logger.LogInformation("=== API Response Received ===");
        _logger.LogInformation($"Provider: {result.ProviderUsed}");
        _logger.LogInformation($"Confidence: {result.Confidence:P0}");
        _logger.LogInformation($"\n--- SUMMARY ---\n{result.Summary}\n");
        _logger.LogInformation($"\n--- ANALYSIS ---\n{result.Analysis}\n");
        
        if (result.Recommendations.Any())
        {
            _logger.LogInformation($"\n--- TOP RECOMMENDATIONS ({result.Recommendations.Count}) ---");
            for (int i = 0; i < result.Recommendations.Count; i++)
            {
                var rec = result.Recommendations[i];
                _logger.LogInformation($"\n{i + 1}. [{rec.Priority}] {rec.Action}");
                _logger.LogInformation($"   Reasoning: {rec.Reasoning}");
                if (!string.IsNullOrWhiteSpace(rec.Context))
                {
                    _logger.LogInformation($"   Context: {rec.Context}");
                }
            }
        }
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithTimeout_CompletesWithinReasonableTime()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithTimeout_CompletesWithinReasonableTime ===");
        
        var chatClient = CreateChatClient();
        var service = new AnalysisService(chatClient);
        var imageData = LoadTestImage("acceltra prime rad build.png");
        
        var request = new AnalysisRequest
        {
            ImageData = imageData,
            Prompt = "Quick analysis: what stands out in this build?"
        };

        _logger.LogInformation("Sending request to API with 30 second timeout...");
        var startTime = DateTime.UtcNow;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var result = await service.AnalyzeAsync(request, cts.Token);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(result);
        Assert.False(cts.IsCancellationRequested, "Request should complete within 30 seconds");
        
        // Output results
        _logger.LogInformation($"=== API Response Received in {elapsed.TotalSeconds:F2} seconds ===");
        _logger.LogInformation($"Confidence: {result.Confidence:P0}");
        _logger.LogInformation($"\n--- SUMMARY ---\n{result.Summary}\n");
        _logger.LogInformation($"\n--- ANALYSIS ---\n{result.Analysis}\n");
        
        if (result.Recommendations.Any())
        {
            _logger.LogInformation($"\n--- RECOMMENDATIONS ({result.Recommendations.Count}) ---");
            foreach (var rec in result.Recommendations)
            {
                _logger.LogInformation($"  • [{rec.Priority}] {rec.Action}");
            }
        }
    }
}
