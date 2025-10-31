using Mentor.Core.Data;
using Mentor.Core.Models;
using Mentor.Core.Tools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mentor.Core.Tests.Tools;

public class BasicResearchToolTests
{
    private readonly Mock<IWebSearchTool> _mockWebSearchTool;
    private readonly Mock<IArticleReader> _mockArticleReader;
    private readonly Mock<IHtmlToMarkdownConverter> _mockHtmlToMarkdownConverter;
    private readonly Mock<ILogger<BasicResearchTool>> _mockLogger;
    private readonly BasicResearchTool _researchTool;

    public BasicResearchToolTests()
    {
        _mockWebSearchTool = new Mock<IWebSearchTool>();
        _mockArticleReader = new Mock<IArticleReader>();
        _mockHtmlToMarkdownConverter = new Mock<IHtmlToMarkdownConverter>();
        _mockLogger = new Mock<ILogger<BasicResearchTool>>();

        _researchTool = new BasicResearchTool(
            _mockWebSearchTool.Object,
            _mockArticleReader.Object,
            _mockHtmlToMarkdownConverter.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task PerformResearchAsync_ValidRequest_ReturnsResearchResults()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var searchResults = new List<SearchResult>
        {
            new SearchResult { Title = "Article 1", Url = "http://example.com/1", Content = "Description 1" },
            new SearchResult { Title = "Article 2", Url = "http://example.com/2", Content = "Description 2" }
        };

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ReturnsAsync(searchResults);

        _mockArticleReader.Setup(x => x.ReadArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Article content</html>");

        _mockHtmlToMarkdownConverter.Setup(x => x.ConvertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Article content");

        // Act
        var results = await _researchTool.PerformResearchAsync(request, ResearchMode.FullArticle);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("Article 1", results[0].Title);
        Assert.Equal("http://example.com/1", results[0].Url);
        Assert.Equal("# Article content", results[0].Content);
        Assert.Equal("Article 2", results[1].Title);
        Assert.Equal("http://example.com/2", results[1].Url);
        Assert.Equal("# Article content", results[1].Content);
    }

    [Fact]
    public async Task PerformResearchAsync_NoSearchResults_ReturnsEmptyList()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ReturnsAsync(new List<SearchResult>());

        // Act
        var results = await _researchTool.PerformResearchAsync(request, ResearchMode.FullArticle);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
        _mockArticleReader.Verify(x => x.ReadArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PerformResearchAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var searchResults = new List<SearchResult>
        {
            new SearchResult { Title = "Article 1", Url = "http://example.com/1", Content = "Description 1" }
        };

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ReturnsAsync(searchResults);

        _mockArticleReader.Setup(x => x.ReadArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Article content</html>");

        _mockHtmlToMarkdownConverter.Setup(x => x.ConvertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Article content");

        var progressReports = new List<AnalysisProgress>();
        // Use a synchronous progress reporter for tests
        var progress = new SynchronousProgress<AnalysisProgress>(p => progressReports.Add(p));

        // Act
        await _researchTool.PerformResearchAsync(request, ResearchMode.FullArticle, progress);

        // Assert
        Assert.NotEmpty(progressReports);
        // Should have at least one report with web search job
        var hasWebSearchJob = progressReports.Any(p => p.Jobs.Any(j => j.Tag == AnalysisJob.JobTag.WebSearch));
        Assert.True(hasWebSearchJob, "Expected at least one progress report with a web search job");
        
        // Should have a completed web search job in at least one report
        var hasCompletedWebSearch = progressReports.Any(p => 
            p.Jobs.Any(j => j.Tag == AnalysisJob.JobTag.WebSearch && j.Status == JobStatus.Completed));
        Assert.True(hasCompletedWebSearch, "Expected at least one progress report with completed web search");
    }
    
    // Helper class for synchronous progress reporting in tests
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

    [Fact]
    public async Task PerformResearchAsync_SearchFormatsQueryCorrectly()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "Warframe",
            Prompt = "best builds for mesa prime",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        SearchContext? capturedContext = null;
        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .Callback<SearchContext, int>((ctx, max) => capturedContext = ctx)
            .ReturnsAsync(new List<SearchResult>());

        // Act
        await _researchTool.PerformResearchAsync(request, ResearchMode.FullArticle);

        // Assert
        Assert.NotNull(capturedContext);
        // The query should be constructed with "Game: X, articles about: Y. Do not include from reddit or social media"
        // So we should check that the original prompt and game name are in the query
        var expectedQuery = "Game: Warframe, articles about: best builds for mesa prime. Do not include from reddit or social media";
        Assert.Equal(expectedQuery, capturedContext.Query);
    }

    [Fact]
    public void Configure_SetsConfiguration()
    {
        // Arrange
        var config = new ToolConfigurationEntity
        {
            ToolName = KnownTools.BasicResearch,
            Timeout = 60,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        _researchTool.Configure(config);

        // Assert - no exception means success
        // Configuration is stored internally for future use
    }

    [Fact]
    public async Task PerformResearchAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            _researchTool.PerformResearchAsync(request, ResearchMode.FullArticle, null, cts.Token));
    }
    
    [Fact]
    public async Task PerformResearchAsync_SummaryOnlyMode_UsesSearchResultSummaries()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var searchResults = new List<SearchResult>
        {
            new SearchResult { Title = "Article 1", Url = "http://example.com/1", Content = "Summary content 1" },
            new SearchResult { Title = "Article 2", Url = "http://example.com/2", Content = "Summary content 2" }
        };

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ReturnsAsync(searchResults);

        // Act
        var results = await _researchTool.PerformResearchAsync(request, ResearchMode.SummaryOnly);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("Article 1", results[0].Title);
        Assert.Equal("http://example.com/1", results[0].Url);
        Assert.Equal("Summary content 1", results[0].Content);
        Assert.Equal("Article 2", results[1].Title);
        Assert.Equal("http://example.com/2", results[1].Url);
        Assert.Equal("Summary content 2", results[1].Content);
        
        // Verify article reader was never called in summary mode
        _mockArticleReader.Verify(x => x.ReadArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockHtmlToMarkdownConverter.Verify(x => x.ConvertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PerformResearchAsync_SummaryOnlyMode_FiltersOutEmptyContent()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var searchResults = new List<SearchResult>
        {
            new SearchResult { Title = "Article 1", Url = "http://example.com/1", Content = "Summary content 1" },
            new SearchResult { Title = "Article 2", Url = "http://example.com/2", Content = "" },
            new SearchResult { Title = "Article 3", Url = "http://example.com/3", Content = "   " },
            new SearchResult { Title = "Article 4", Url = "http://example.com/4", Content = "Summary content 4" }
        };

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ReturnsAsync(searchResults);

        // Act
        var results = await _researchTool.PerformResearchAsync(request, ResearchMode.SummaryOnly);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("Article 1", results[0].Title);
        Assert.Equal("Summary content 1", results[0].Content);
        Assert.Equal("Article 4", results[1].Title);
        Assert.Equal("Summary content 4", results[1].Content);
    }

    [Fact]
    public async Task PerformResearchAsync_SummaryOnlyMode_MarksEmptyContentAsFailed()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            GameName = "TestGame",
            Prompt = "test prompt",
            ImageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png")
        };

        var searchResults = new List<SearchResult>
        {
            new SearchResult { Title = "Article 1", Url = "http://example.com/1", Content = "Summary content 1" },
            new SearchResult { Title = "Article 2", Url = "http://example.com/2", Content = "" },
            new SearchResult { Title = "Article 3", Url = "http://example.com/3", Content = "Summary content 3" }
        };

        _mockWebSearchTool.Setup(x => x.Search(It.IsAny<SearchContext>(), It.IsAny<int>()))
            .ReturnsAsync(searchResults);

        var progressReports = new List<AnalysisProgress>();
        var progress = new SynchronousProgress<AnalysisProgress>(p => progressReports.Add(p));

        // Act
        var results = await _researchTool.PerformResearchAsync(request, ResearchMode.SummaryOnly, progress);

        // Assert
        Assert.Equal(2, results.Count); // Only 2 valid results
        
        // Verify progress reporting
        Assert.NotEmpty(progressReports);
        
        // Should have a failed job for the article with empty content
        var hasFailedJob = progressReports.Any(p => 
            p.Jobs.Any(j => j.Status == JobStatus.Failed && j.Name.Contains("Article 2")));
        Assert.True(hasFailedJob, "Expected a failed job for article with empty content");
        
        // Should have completed jobs for articles with content
        var hasCompletedJobs = progressReports.Any(p => 
            p.Jobs.Any(j => j.Status == JobStatus.Completed && j.Name.Contains("Article 1")));
        Assert.True(hasCompletedJobs, "Expected completed jobs for articles with valid content");
    }
}

