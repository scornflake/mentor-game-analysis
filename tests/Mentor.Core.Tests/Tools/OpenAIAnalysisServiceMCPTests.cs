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
/// Tests for OpenAIAnalysisServiceMCP implementation
/// </summary>
public class OpenAIAnalysisServiceMCPTests
{
    private readonly Mock<ILLMClient> _mockLlmClient;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<AnalysisService>> _mockLogger;
    private readonly Mock<IToolFactory> _mockToolFactory;
    private readonly OpenAIAnalysisServiceMCP _analysisService;

    public OpenAIAnalysisServiceMCPTests()
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
            ServerHasMcpSearch = true
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(mockConfig);
        _mockLlmClient.Setup(x => x.ChatClient).Returns(_mockChatClient.Object);

        var searchResultFormatter = new SearchResultFormatter();
        _analysisService = new OpenAIAnalysisServiceMCP(
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
                    ""Priority"": ""medium"",
                    ""Action"": ""Test action"",
                    ""Reasoning"": ""Test reasoning"",
                    ""Context"": ""Test context"",
                    ""ReferenceLink"": ""https://example.com""
                }
            ],
            ""Confidence"": 0.85
        }";

        SetupMockChatResponse(llmResponse);

        // Act
        var result = await _analysisService.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test analysis", result.Analysis);
        Assert.Equal("Test summary", result.Summary);
        Assert.Single(result.Recommendations);
        Assert.Equal(Priority.Medium, result.Recommendations[0].Priority);
        Assert.Equal(0.85, result.Confidence);
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
            p.Jobs.Any(j => j.Tag == AnalysisJob.JobTag.LLMAnalysis && j.Name.Contains("MCP")));
        Assert.True(hasLLMJob, "Expected progress report with LLM analysis job mentioning MCP");
    }

    [Fact]
    public async Task CreateAIOptions_MCPImplementation_ConfiguresForMCP()
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

        ChatOptions? capturedOptions = null;
        SetupMockChatResponseWithOptionsCapture(llmResponse, opts => capturedOptions = opts);

        // Act
        await _analysisService.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(capturedOptions);
        Assert.Equal(ChatToolMode.Auto, capturedOptions.ToolMode);
        Assert.NotNull(capturedOptions.Tools);
        Assert.Empty(capturedOptions.Tools);
    }

    [Fact]
    public void GetSystemPromptText_MCPImplementation_IncludesMCPGuidance()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 137, 80, 78, 71 }, "image/png")
        };

        // Use reflection to access the protected method
        var methodInfo = typeof(OpenAIAnalysisServiceMCP)
            .GetMethod("GetSystemPromptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = (string)methodInfo!.Invoke(_analysisService, new object[] { request })!;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("MCP", result);
        Assert.Contains("Model Context Protocol", result);
        Assert.Contains("server-side tools", result);
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotCallResearchTool()
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

        // Act
        await _analysisService.AnalyzeAsync(request);

        // Assert
        // Verify that no research tool was requested (MCP server handles research)
        _mockToolFactory.Verify(x => x.GetResearchToolAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ILLMClient>()), Times.Never);
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

    private void SetupMockChatResponseWithOptionsCapture(string responseText, Action<ChatOptions> captureAction)
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
            .Callback<IEnumerable<ChatMessage>, ChatOptions, CancellationToken>((msgs, opts, ct) => captureAction(opts))
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

