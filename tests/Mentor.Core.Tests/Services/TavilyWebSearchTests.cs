using System.Net;
using System.Text;
using Mentor.Core.Data;
using Mentor.Core.Models;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public class TavilyWebSearchTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TavilyWebSearchTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static Mock<IHttpClientFactory> CreateMockHttpClientFactory(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return mockFactory;
    }

    [Fact]
    public void SupportedModes_ReturnsAllThreeModes()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);

        // Act
        var supportedModes = service.SupportedModes;

        // Assert
        Assert.Contains(SearchOutputFormat.Snippets, supportedModes);
        Assert.Contains(SearchOutputFormat.Summary, supportedModes);
        Assert.Contains(SearchOutputFormat.Structured, supportedModes);
        Assert.Equal(3, supportedModes.Count);
    }

    [Fact]
    public async Task Search_WithSnippetsFormat_ReturnsConcatenatedSnippets()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result 1",
                    "url": "https://example.com/1",
                    "content": "First test snippet.",
                    "score": 0.95
                },
                {
                    "title": "Test Result 2",
                    "url": "https://example.com/2",
                    "content": "Second test snippet.",
                    "score": 0.89
                }
            ]
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("test query"), SearchOutputFormat.Snippets, 5);

        // Assert
        Assert.Contains("First test snippet.", result);
        Assert.Contains("Second test snippet.", result);
        Assert.DoesNotContain("https://example.com", result);
        Assert.DoesNotContain("Test Result 1", result);
    }

    [Fact]
    public async Task Search_WithSummaryFormat_ReturnsTavilyAnswer()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "answer": "This is a comprehensive answer from Tavily's LLM.",
            "results": [
                {
                    "title": "Test Result 1",
                    "url": "https://example.com/1",
                    "content": "First test snippet.",
                    "score": 0.95
                }
            ]
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("test query"), SearchOutputFormat.Summary, 5);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("This is a comprehensive answer from Tavily's LLM.", result);
    }

    [Fact]
    public async Task SearchStructured_ReturnsResultsWithScore()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Result 1",
                    "url": "https://example.com/1",
                    "content": "Snippet 1.",
                    "score": 0.95
                },
                {
                    "title": "Result 2",
                    "url": "https://example.com/2",
                    "content": "Snippet 2.",
                    "score": 0.89
                }
            ]
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.SearchStructured(SearchContext.Create("test query"), 2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Result 1", result[0].Title);
        Assert.Equal("https://example.com/1", result[0].Url);
        Assert.Equal("Snippet 1.", result[0].Description);
        Assert.Equal(0.95, result[0].Score);
    }

    [Fact]
    public async Task Search_LimitsResultsToMaxResults()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {"title": "Result 1", "url": "https://example.com/1", "content": "Snippet 1.", "score": 0.95},
                {"title": "Result 2", "url": "https://example.com/2", "content": "Snippet 2.", "score": 0.90},
                {"title": "Result 3", "url": "https://example.com/3", "content": "Snippet 3.", "score": 0.85},
                {"title": "Result 4", "url": "https://example.com/4", "content": "Snippet 4.", "score": 0.80},
                {"title": "Result 5", "url": "https://example.com/5", "content": "Snippet 5.", "score": 0.75}
            ]
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.SearchStructured(SearchContext.Create("test query"), 2);

        Assert.Equal(2, result.Count);
        
        // Assert
        Assert.NotNull(result[0]);
        Assert.NotNull(result[1]);
    }

    [Fact]
    public async Task Search_WithEmptyResults_ReturnsNoResultsMessage()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": []
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("test query"), SearchOutputFormat.Snippets, 5);

        // Assert
        Assert.Contains("No results found", result);
    }

    [Fact]
    public async Task Search_WithApiError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("Error", HttpStatusCode.Unauthorized);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "invalid-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.Search(SearchContext.Create("test query"), SearchOutputFormat.Snippets, 5)
        );
    }

    [Fact]
    public async Task Search_WithNullContext_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.Search(null!, SearchOutputFormat.Snippets, 5)
        );
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Search(SearchContext.Create(""), SearchOutputFormat.Snippets, 5)
        );
    }

    [Fact]
    public async Task Search_WithQueryOver400Characters_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        var longQuery = new string('a', 401);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Search(SearchContext.Create(longQuery), SearchOutputFormat.Snippets, 5)
        );
    }
}

public class TavilyWebSearchIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TavilyWebSearchIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [ConditionalFact("TAVILY_API_KEY")]
    public async Task Search_WithRealApi_SnippetsFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(httpClientFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("C# programming"), SearchOutputFormat.Snippets, 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Snippets): {result}");
        Assert.NotEmpty(result);
        Assert.DoesNotContain("No results found", result);
    }

    [ConditionalFact("TAVILY_API_KEY")]
    public async Task Search_WithRealApi_StructuredFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(httpClientFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.SearchStructured(SearchContext.Create("dotnet 8"), 2);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Structured): {result}");
        Assert.NotEmpty(result);
        Assert.Contains("http", result[0].Url);
        Assert.NotNull(result[0].Score);
    }

    [ConditionalFact("TAVILY_API_KEY")]
    public async Task Search_WithRealApi_SummaryFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.tavily.com",
            Timeout = 30
        };

        var logger = NullLogger<TavilyWebSearch>.Instance;
        var service = new TavilyWebSearch(httpClientFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("xUnit testing framework"), SearchOutputFormat.Summary, 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Summary): {result}");
        Assert.NotEmpty(result);
    }
}

