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
/// Tests for OpenAIAnalysisServiceRAG implementation
/// </summary>
public class OpenAIAnalysisServiceRAGTests
{
    private readonly Mock<ILLMClient> _mockLlmClient;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<AnalysisService>> _mockLogger;
    private readonly Mock<IToolFactory> _mockToolFactory;
    private readonly Mock<IResearchTool> _mockResearchTool;
    private readonly OpenAIAnalysisServiceRAG _analysisService;

    public OpenAIAnalysisServiceRAGTests()
    {
        _mockLlmClient = new Mock<ILLMClient>();
        _mockChatClient = new Mock<IChatClient>();
        _mockLogger = new Mock<ILogger<AnalysisService>>();
        _mockToolFactory = new Mock<IToolFactory>();
        _mockResearchTool = new Mock<IResearchTool>();

        var mockConfig = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = true,
            ServerHasMcpSearch = false
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(mockConfig);
        _mockLlmClient.Setup(x => x.ChatClient).Returns(_mockChatClient.Object);

        var searchResultFormatter = new SearchResultFormatter();
        _analysisService = new OpenAIAnalysisServiceRAG(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );
    }

    [Fact]
    public async Task AnalyzeAsync_PerformsUpfrontResearch()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 137, 80, 78, 71 }, "image/png")
        };

        var researchResults = new List<ResearchResult>
        {
            new ResearchResult
            {
                Title = "Research Article",
                Url = "https://example.com",
                Content = "Research content"
            }
        };

        _mockToolFactory.Setup(x => x.GetResearchToolAsync(
            KnownTools.BasicResearch,
            KnownSearchTools.Tavily,
            _mockLlmClient.Object))
            .ReturnsAsync(_mockResearchTool.Object);

        _mockResearchTool.Setup(x => x.PerformResearchAsync(
            It.IsAny<AnalysisRequest>(),
            It.IsAny<ResearchMode>(),
            It.IsAny<IProgress<AnalysisProgress>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(researchResults);

        var llmResponse = @"{
            ""Analysis"": ""Test analysis"",
            ""Summary"": ""Test summary"",
            ""Recommendations"": [],
            ""Confidence"": 0.95
        }";

        SetupMockChatResponse(llmResponse);

        // Act
        var result = await _analysisService.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        _mockToolFactory.Verify(x => x.GetResearchToolAsync(
            KnownTools.BasicResearch,
            KnownSearchTools.Tavily,
            _mockLlmClient.Object), Times.Once);
        _mockResearchTool.Verify(x => x.PerformResearchAsync(
            It.IsAny<AnalysisRequest>(),
            It.IsAny<ResearchMode>(),
            It.IsAny<IProgress<AnalysisProgress>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WithResearchResults_InjectsIntoPrompt()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 137, 80, 78, 71 }, "image/png")
        };

        var researchResults = new List<ResearchResult>
        {
            new ResearchResult
            {
                Title = "Test Article",
                Url = "https://example.com/article",
                Content = "Important research content"
            }
        };

        _mockToolFactory.Setup(x => x.GetResearchToolAsync(
            KnownTools.BasicResearch,
            KnownSearchTools.Tavily,
            _mockLlmClient.Object))
            .ReturnsAsync(_mockResearchTool.Object);

        _mockResearchTool.Setup(x => x.PerformResearchAsync(
            It.IsAny<AnalysisRequest>(),
            It.IsAny<ResearchMode>(),
            It.IsAny<IProgress<AnalysisProgress>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(researchResults);

        var llmResponse = @"{
            ""Analysis"": ""Test analysis"",
            ""Summary"": ""Test summary"",
            ""Recommendations"": [],
            ""Confidence"": 0.95
        }";

        IEnumerable<ChatMessage>? capturedMessages = null;
        SetupMockChatResponseWithCapture(llmResponse, messages => capturedMessages = messages);

        // Act
        await _analysisService.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(capturedMessages);
        var systemMessage = capturedMessages.FirstOrDefault(m => m.Role == ChatRole.System);
        Assert.NotNull(systemMessage);
        
        var systemContent = systemMessage.Text;
        Assert.Contains("Test Article", systemContent);
        Assert.Contains("https://example.com/article", systemContent);
        Assert.Contains("Important research content", systemContent);
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

        var researchResults = new List<ResearchResult>();

        _mockToolFactory.Setup(x => x.GetResearchToolAsync(
            KnownTools.BasicResearch,
            KnownSearchTools.Tavily,
            _mockLlmClient.Object))
            .ReturnsAsync(_mockResearchTool.Object);

        _mockResearchTool.Setup(x => x.PerformResearchAsync(
            It.IsAny<AnalysisRequest>(),
            It.IsAny<ResearchMode>(),
            It.IsAny<IProgress<AnalysisProgress>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(researchResults);

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
        
        // Should have LLM analysis job progress
        var hasLLMJob = progressReports.Any(p => 
            p.Jobs.Any(j => j.Tag == AnalysisJob.JobTag.LLMAnalysis));
        Assert.True(hasLLMJob, "Expected progress with LLM analysis job");
    }

    [Fact]
    public async Task CreateAIOptions_RAGImplementation_NoTools()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 137, 80, 78, 71 }, "image/png")
        };

        _mockToolFactory.Setup(x => x.GetResearchToolAsync(
            KnownTools.BasicResearch,
            KnownSearchTools.Tavily,
            _mockLlmClient.Object))
            .ReturnsAsync(_mockResearchTool.Object);

        _mockResearchTool.Setup(x => x.PerformResearchAsync(
            It.IsAny<AnalysisRequest>(),
            It.IsAny<ResearchMode>(),
            It.IsAny<IProgress<AnalysisProgress>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResearchResult>());

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
        Assert.NotNull(capturedOptions.Tools);
        Assert.Empty(capturedOptions.Tools);
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

    private void SetupMockChatResponseWithCapture(string responseText, Action<IEnumerable<ChatMessage>> captureAction)
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
            .Callback<IEnumerable<ChatMessage>, ChatOptions, CancellationToken>((msgs, opts, ct) => captureAction(msgs))
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

