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
/// Tests for OpenAIAnalysisServiceBase shared functionality
/// </summary>
public class OpenAIAnalysisServiceBaseTests
{
    private readonly Mock<ILLMClient> _mockLlmClient;
    private readonly Mock<ILogger<AnalysisService>> _mockLogger;
    private readonly Mock<IToolFactory> _mockToolFactory;
    private readonly Mock<IWebSearchTool> _mockWebSearchTool;
    private readonly Mock<IArticleReader> _mockArticleReader;
    private readonly TestAnalysisService _analysisService;

    public OpenAIAnalysisServiceBaseTests()
    {
        _mockLlmClient = new Mock<ILLMClient>();
        _mockLogger = new Mock<ILogger<AnalysisService>>();
        _mockToolFactory = new Mock<IToolFactory>();
        _mockWebSearchTool = new Mock<IWebSearchTool>();
        _mockArticleReader = new Mock<IArticleReader>();

        var mockConfig = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            RetrievalAugmentedGeneration = false,
        };

        _mockLlmClient.Setup(x => x.Configuration).Returns(mockConfig);

        var searchResultFormatter = new SearchResultFormatter();
        _analysisService = new TestAnalysisService(
            _mockLlmClient.Object,
            _mockLogger.Object,
            _mockToolFactory.Object,
            searchResultFormatter
        );
    }

    [Fact]
    public async Task SetupWebSearchTool_InitializesWebSearchTool()
    {
        // Arrange
        _mockToolFactory.Setup(x => x.GetToolAsync(KnownSearchTools.Tavily))
            .ReturnsAsync(_mockWebSearchTool.Object);

        // Act
        await _analysisService.TestSetupWebSearchTool();

        // Assert
        _mockToolFactory.Verify(x => x.GetToolAsync(KnownSearchTools.Tavily), Times.Once);
        Assert.NotNull(_analysisService.GetWebSearchTool());
    }

    [Fact]
    public async Task SetupWebSearchTool_ThrowsWhenToolCreationFails()
    {
        // Arrange
        _mockToolFactory.Setup(x => x.GetToolAsync(KnownSearchTools.Tavily))
            .ReturnsAsync((IWebSearchTool?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _analysisService.TestSetupWebSearchTool());
    }

    [Fact]
    public async Task SearchTheWebSummary_PerformsSearchAndReturnsSummary()
    {
        // Arrange
        _mockToolFactory.Setup(x => x.GetToolAsync(KnownSearchTools.Tavily))
            .ReturnsAsync(_mockWebSearchTool.Object);

        await _analysisService.TestSetupWebSearchTool();

        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };
        _analysisService.SetCurrentRequest(request);

        var searchResults = new List<SearchResult>
        {
            new SearchResult { Content = "Test content 1" },
            new SearchResult { Content = "Test content 2" }
        };
        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), 10))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _analysisService.TestSearchTheWebSummary("test query");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Found 2 result(s)", result);
        _mockWebSearchTool.Verify(x => x.Search(
            It.Is<SearchContext>(ctx => ctx.Query == "test query"),
            10), Times.Once);
    }

    [Fact]
    public async Task ReadArticleContent_ReadsArticleAndReturnsContent()
    {
        // Arrange
        var expectedContent = "Article content";
        _mockToolFactory.Setup(x => x.GetArticleReaderAsync())
            .ReturnsAsync(_mockArticleReader.Object);

        _mockArticleReader.Setup(x => x.ReadArticleAsync("https://example.com", default))
            .ReturnsAsync(expectedContent);

        // Act
        var result = await _analysisService.TestReadArticleContent("https://example.com");

        // Assert
        Assert.Equal(expectedContent, result);
        _mockToolFactory.Verify(x => x.GetArticleReaderAsync(), Times.Once);
        _mockArticleReader.Verify(x => x.ReadArticleAsync("https://example.com", default), Times.Once);
    }

    [Fact]
    public async Task SetupSearchAndArticleTools_ReturnsToolList()
    {
        // Arrange
        _mockToolFactory.Setup(x => x.GetToolAsync(KnownSearchTools.Tavily))
            .ReturnsAsync(_mockWebSearchTool.Object);

        // Act
        var tools = await _analysisService.TestSetupSearchAndArticleTools();

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(2, tools.Count);
        _mockToolFactory.Verify(x => x.GetToolAsync(KnownSearchTools.Tavily), Times.Once);
    }

    [Fact]
    public void GetSystemPromptText_WithNoResearchResults_ReturnsBasePrompt()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        // Act
        var result = _analysisService.TestGetSystemPromptText(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("You are an expert game advisor", result);
        Assert.Contains("TestGame", result);
        Assert.DoesNotContain("I have included results from my research", result);
    }

    [Fact]
    public void GetSystemPromptText_WithResearchResults_InjectsResultsIntoPrompt()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var researchResults = new List<ResearchResult>
        {
            new ResearchResult
            {
                Title = "Test Article",
                Url = "https://example.com",
                Content = "Test content"
            }
        };
        _analysisService.SetResearchResults(researchResults);

        // Act
        var result = _analysisService.TestGetSystemPromptText(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("I have included results from my research", result);
        Assert.Contains("Test Article", result);
        Assert.Contains("https://example.com", result);
        Assert.Contains("Test content", result);
    }

    [Fact]
    public void GetSystemPromptText_WithMultipleResearchResults_InjectsAllResults()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var researchResults = new List<ResearchResult>
        {
            new ResearchResult
            {
                Title = "Article 1",
                Url = "https://example.com/1",
                Content = "Content 1"
            },
            new ResearchResult
            {
                Title = "Article 2",
                Url = "https://example.com/2",
                Content = "Content 2"
            }
        };
        _analysisService.SetResearchResults(researchResults);

        // Act
        var result = _analysisService.TestGetSystemPromptText(request);

        // Assert
        Assert.Contains("Article 1", result);
        Assert.Contains("https://example.com/1", result);
        Assert.Contains("Content 1", result);
        Assert.Contains("Article 2", result);
        Assert.Contains("https://example.com/2", result);
        Assert.Contains("Content 2", result);
    }

    /// <summary>
    /// Concrete test implementation of OpenAIAnalysisServiceBase to test protected methods
    /// </summary>
    private class TestAnalysisService : OpenAIAnalysisServiceBase
    {
        public TestAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory, SearchResultFormatter searchResultFormatter)
            : base(llmClient, logger, toolFactory, searchResultFormatter)
        {
        }

        public async Task TestSetupWebSearchTool() => await SetupWebSearchTool();
        public async Task<string> TestSearchTheWebSummary(string query) => await SearchTheWebSummary(query);
        public async Task<string> TestReadArticleContent(string url) => await ReadArticleContent(url);
        public async Task<IList<AITool>> TestSetupSearchAndArticleTools() => await SetupSearchAndArticleTools();
        public string TestGetSystemPromptText(AnalysisRequest request) => GetSystemPromptText(request);

        public IWebSearchTool? GetWebSearchTool() => _webSearchTool;
        public void SetCurrentRequest(AnalysisRequest request) => _currentRequest = request;
        public void SetResearchResults(List<ResearchResult> results) => _researchResults = results;

        public override Task<Recommendation> AnalyzeAsync(AnalysisRequest request, IProgress<AnalysisProgress>? progress = null, IProgress<AIContent>? aiProgress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

