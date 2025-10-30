using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tests.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Tools;

public class TextSummarizerTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TextSummarizerTests> _logger;
    private readonly ITestOutputHelper _testOutputHelper;

    public TextSummarizerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizerTests>>();
    }

    [Fact]
    public void ToolFactory_CreateTextSummarizer_WithValidLLMClient_ReturnsInstance()
    {
        // Arrange
        var services = TestHelpers.CreateTestServices(_testOutputHelper);
        var mockConfigRepo = new Mock<IConfigurationRepository>();
        services.AddSingleton<IConfigurationRepository>(mockConfigRepo.Object);
        var testServiceProvider = services.BuildServiceProvider();
        
        var factory = new ToolFactory(
            testServiceProvider.GetRequiredService<ILogger<ToolFactory>>(),
            testServiceProvider,
            mockConfigRepo.Object);
        
        var llmClient = CreateTestLLMClient();

        // Act
        var summarizer = factory.CreateTextSummarizer(llmClient);

        // Assert
        Assert.NotNull(summarizer);
        Assert.IsAssignableFrom<ITextSummarizer>(summarizer);
    }

    [Fact]
    public void ToolFactory_CreateTextSummarizer_WithNullLLMClient_ThrowsArgumentNullException()
    {
        // Arrange
        var services = TestHelpers.CreateTestServices(_testOutputHelper);
        var mockConfigRepo = new Mock<IConfigurationRepository>();
        services.AddSingleton<IConfigurationRepository>(mockConfigRepo.Object);
        var testServiceProvider = services.BuildServiceProvider();
        
        var factory = new ToolFactory(
            testServiceProvider.GetRequiredService<ILogger<ToolFactory>>(),
            testServiceProvider,
            mockConfigRepo.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.CreateTextSummarizer(null!));
    }

    [Fact]
    public async Task SummarizeAsync_WithNullContent_ThrowsArgumentException()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizer>>();
        var summarizer = new TextSummarizer(llmClient, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            summarizer.SummarizeAsync(null!, "Test prompt", 50));
    }

    [Fact]
    public async Task SummarizeAsync_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizer>>();
        var summarizer = new TextSummarizer(llmClient, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            summarizer.SummarizeAsync("", "Test prompt", 50));
    }

    [Fact]
    public async Task SummarizeAsync_WithZeroTargetWordCount_ThrowsArgumentException()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizer>>();
        var summarizer = new TextSummarizer(llmClient, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            summarizer.SummarizeAsync("Some content", "Test prompt", 0));
    }

    [Fact]
    public async Task SummarizeAsync_WithNegativeTargetWordCount_ThrowsArgumentException()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizer>>();
        var summarizer = new TextSummarizer(llmClient, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            summarizer.SummarizeAsync("Some content", "Test prompt", -10));
    }

    [RequiresOpenAIKeyFact]
    public async Task SummarizeAsync_WithRealLLM_ReturnsValidSummary()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real API Test: SummarizeAsync_WithRealLLM_ReturnsValidSummary ===");

        var llmClient = CreateLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizer>>();
        var summarizer = new TextSummarizer(llmClient, logger);

        var content = @"
Warframe is a free-to-play action role-playing third-person shooter multiplayer online game 
developed and published by Digital Extremes. Released for Windows personal computers in March 2013, 
it was ported to the PlayStation 4 in November 2013, the Xbox One in September 2014, the Nintendo 
Switch in November 2018, the PlayStation 5 in November 2020, and the Xbox Series X/S in April 2021.

Players control members of the Tenno, a race of ancient warriors who have awoken from centuries of 
suspended animation far into Earth's future to find themselves at war in the planetary system with 
different factions. The Tenno use their powered Warframes along with a variety of weapons and abilities 
to complete missions. While many of the game's missions use procedurally generated levels, newer updates 
have included large open world areas similar to other massively multiplayer online games as well as 
some story-specific missions with fixed level design.";

        var targetWordCount = 50;
        var prompt = "Provide a concise summary focusing on the key facts about the game.";

        _logger.LogInformation("Sending summarization request to API");
        _logger.LogInformation($"Original content length: {content.Length} characters, approximately {content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length} words");
        _logger.LogInformation($"Target word count: {targetWordCount}");

        // Act
        var result = await summarizer.SummarizeAsync(content, prompt, targetWordCount);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var resultWordCount = result.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        _logger.LogInformation($"Result word count: {resultWordCount}");
        _logger.LogInformation($"Summary: {result}");

        // The result should be shorter than the original
        Assert.True(result.Length < content.Length, "Summary should be shorter than original content");
        
        // The word count should be reasonably close to target (within 50% tolerance for flexibility)
        var minWords = targetWordCount / 2;
        var maxWords = targetWordCount * 2;
        Assert.InRange(resultWordCount, minWords, maxWords);
        
        _logger.LogInformation("=== Test completed successfully ===");
    }

    private ILLMClient CreateTestLLMClient()
    {
        // Create a minimal LLM client for validation tests that don't actually call the LLM
        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfigurationEntity
        {
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o-mini",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        return factory.GetProvider(providerConfig);
    }

    private ILLMClient CreateLLMClient()
    {
        var apiKey = ApiKeyHelper.GetOpenAIApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key not available for integration test");
        }

        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfigurationEntity
        {
            ProviderType = "perplexity",
            ApiKey = apiKey,
            Model = "sonar",
            BaseUrl = "https://api.perplexity.ai",
            Timeout = 60
        };

        return factory.GetProvider(providerConfig);
    }
}

