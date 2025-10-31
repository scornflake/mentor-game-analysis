using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mentor.Core.Tests.Tools;

/// <summary>
/// Tests for basic OpenAIAnalysisService implementation
/// </summary>
public class OpenAIAnalysisServiceTests
{
    private readonly Mock<ILLMClient> _mockLlmClient;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<AnalysisService>> _mockLogger;
    private readonly Mock<IToolFactory> _mockToolFactory;
    private readonly OpenAIAnalysisService _analysisService;

    public OpenAIAnalysisServiceTests()
    {
        _mockLlmClient = new Mock<ILLMClient>();
        _mockChatClient = new Mock<IChatClient>();
        _mockLogger = new Mock<ILogger<AnalysisService>>();
        _mockToolFactory = new Mock<IToolFactory>();

        var mockConfig = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = false,
            ServerHasMcpSearch = false
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(mockConfig);
        _mockLlmClient.Setup(x => x.ChatClient).Returns(_mockChatClient.Object);

        var searchResultFormatter = new SearchResultFormatter();
        _analysisService = new OpenAIAnalysisService(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );
    }

    [Fact]
    public async Task AnalyzeAsync_ValidRequest_ReturnsRecommendation()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 137, 80, 78, 71 }, "image/png")
        };

        var llmResponse = @"{
            ""Analysis"": ""Test analysis"",
            ""Summary"": ""Test summary"",
            ""Recommendations"": [
                {
                    ""Priority"": ""high"",
                    ""Action"": ""Test action"",
                    ""Reasoning"": ""Test reasoning"",
                    ""Context"": ""Test context"",
                    ""ReferenceLink"": ""https://example.com""
                }
            ],
            ""Confidence"": 0.95
        }";

        SetupMockChatResponse(llmResponse);

        // Act
        var result = await _analysisService.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test analysis", result.Analysis);
        Assert.Equal("Test summary", result.Summary);
        Assert.Single(result.Recommendations);
        Assert.Equal(Priority.High, result.Recommendations[0].Priority);
        Assert.Equal("Test action", result.Recommendations[0].Action);
        Assert.Equal(0.95, result.Confidence);
    }

    [Fact]
    public async Task AnalyzeAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 137, 80, 78, 71 }, "image/png")
        };

        var llmResponse = @"{
            ""Analysis"": ""Test"",
            ""Summary"": ""Test"",
            ""Recommendations"": [],
            ""Confidence"": 0.8
        }";

        SetupMockChatResponse(llmResponse);

        var progressReports = new List<AnalysisProgress>();
        var progress = new SynchronousProgress<AnalysisProgress>(p => progressReports.Add(p));

        // Act
        await _analysisService.AnalyzeAsync(request, progress);

        // Assert
        Assert.NotEmpty(progressReports);
        var hasLLMJob = progressReports.Any(p => 
            p.Jobs.Any(j => j.Tag == AnalysisJob.JobTag.LLMAnalysis));
        Assert.True(hasLLMJob, "Expected progress report with LLM analysis job");
    }

    [Fact]
    public async Task CreateAIOptions_BasicImplementation_NoTools()
    {
        // Use reflection to access the protected method
        var methodInfo = typeof(OpenAIAnalysisService)
            .GetMethod("CreateAIOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var task = (Task<ChatOptions>)methodInfo!.Invoke(_analysisService, null)!;
        var options = await task;

        // Assert
        Assert.NotNull(options);
        Assert.Null(options.Tools);
    }

    [Fact]
    public void Create_WithBasicConfiguration_ReturnsBasicImplementation()
    {
        // Arrange
        var config = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = false,
            ServerHasMcpSearch = false
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(config);

        // Act
        var searchResultFormatter = new SearchResultFormatter();
        var result = OpenAIAnalysisService.Create(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OpenAIAnalysisService>(result);
    }

    [Fact]
    public void Create_WithRAGEnabled_ReturnsRAGImplementation()
    {
        // Arrange
        var config = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = true,
            ServerHasMcpSearch = false
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(config);

        // Act
        var searchResultFormatter = new SearchResultFormatter();
        var result = OpenAIAnalysisService.Create(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OpenAIAnalysisServiceRAG>(result);
    }

    [Fact]
    public void Create_WithMCPEnabled_ReturnsMCPImplementation()
    {
        // Arrange
        var config = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = false,
            ServerHasMcpSearch = true
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(config);

        // Act
        var searchResultFormatter = new SearchResultFormatter();
        var result = OpenAIAnalysisService.Create(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OpenAIAnalysisServiceMCP>(result);
    }

    [Fact]
    public void Create_WithBothRAGAndMCPEnabled_PrioritizesMCP()
    {
        // Arrange - MCP should take precedence
        var config = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = true,
            ServerHasMcpSearch = true
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(config);

        // Act
        var searchResultFormatter = new SearchResultFormatter();
        var result = OpenAIAnalysisService.Create(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OpenAIAnalysisServiceMCP>(result);
    }

    private void SetupMockChatResponse(string responseText)
    {
        var updates = new List<ChatResponseUpdate>
        {
            new ChatResponseUpdate
            {
                Contents = [new TextContent(responseText)],
                FinishReason = ChatFinishReason.Stop
            }
        };

        _mockChatClient.Setup(x => x.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(updates));
    }

    private async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    private class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Report(T value)
        {
            _handler(value);
        }
    }
}

