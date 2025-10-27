using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tools;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalysisServiceIntegrationTests> _logger;
    private readonly Mock<IToolFactory> _toolFactoryMock;

    public AnalysisServiceIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        var webSearch = new Mock<IWebSearchTool>();
        _toolFactoryMock = new Mock<IToolFactory>();
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        services.AddWebSearchTool(webSearch.Object);
        services.AddSingleton<IToolFactory, ToolFactory>();
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

    private ILLMClient CreateLLMClient()
    {
        var apiKey = ApiKeyHelper.GetOpenAIApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key not available for integration test");
        }

        var implConfig = new ProviderImplementationsConfiguration
        {
            ProviderImplementations = new Dictionary<string, ProviderImplementationDetails>
            {
                ["perplexity"] = new ProviderImplementationDetails
                {
                    DefaultBaseUrl = "https://api.perplexity.ai",
                    DefaultModel = "sonar"
                }
            }
        };

        var options = Options.Create(implConfig);
        var factory = new LLMProviderFactory(options, _serviceProvider);

        var providerConfig = new ProviderConfiguration
        {
            ProviderType = "perplexity",
            ApiKey = apiKey,
            Model = "sonar",
            BaseUrl = "https://api.perplexity.ai",
            Timeout = 60
        };

        return factory.GetProvider(providerConfig);
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeAsync_WithRealImage_ReturnsValidRecommendation()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real API Test: AnalyzeAsync_WithRealImage_ReturnsValidRecommendation ===");
        
        var llmClient = CreateLLMClient();
        var service = new AnalysisService(llmClient, 
            _serviceProvider.GetRequiredService<ILogger<AnalysisService>>(),
            _serviceProvider.GetRequiredService<IToolFactory>()
            );
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
        Assert.Equal("perplexity", result.ProviderUsed);
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
}
