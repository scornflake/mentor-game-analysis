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
    public async Task Search_ReturnsSearchResults()
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Test Result 1", result[0].Title);
        Assert.Equal("https://example.com/1", result[0].Url);
        Assert.Equal("First test snippet.", result[0].Content);
        Assert.Equal(0.95, result[0].Score);
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
        Assert.Equal("Snippet 1.", result[0].Content);
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
    public async Task Search_WithEmptyResults_ReturnsEmptyList()
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
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
            () => service.Search(SearchContext.Create("test query"), 5)
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
            () => service.Search(null!, 5)
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
            () => service.Search(SearchContext.Create(""), 5)
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
            () => service.Search(SearchContext.Create(longQuery), 5)
        );
    }

    [Fact]
    public async Task Search_StripMarkdown_RemovesInlineLinks()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "Check out [this link](https://example.com) for more info.",
                    "raw_content": "Check out [this link](https://example.com) for more info.",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Check out  for more info.", result[0].Content);
        Assert.DoesNotContain("[this link]", result[0].Content);
        Assert.DoesNotContain("(https://example.com)", result[0].Content);
    }

    [Fact]
    public async Task Search_StripMarkdown_RemovesMultipleInlineLinks()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "Visit [site A](https://a.com) and [site B](https://b.com) today.",
                    "raw_content": "Visit [site A](https://a.com) and [site B](https://b.com) today.",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Visit  and  today.", result[0].Content);
    }

    [Fact]
    public async Task Search_StripMarkdown_RemovesReferenceStyleLinks()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "Check the [documentation][docs] for details.",
                    "raw_content": "Check the [documentation][docs] for details.",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Check the  for details.", result[0].Content);
        Assert.DoesNotContain("[documentation][docs]", result[0].Content);
    }

    [Fact]
    public async Task Search_StripMarkdown_RemovesAutolinks()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "Visit <https://example.com> for more.",
                    "raw_content": "Visit <https://example.com> for more.",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Visit  for more.", result[0].Content);
        Assert.DoesNotContain("<https://example.com>", result[0].Content);
    }

    [Fact]
    public async Task Search_StripMarkdown_PreservesOtherMarkdown()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "**Bold** text with [link](https://example.com) and *italic* content.",
                    "raw_content": "**Bold** text with [link](https://example.com) and *italic* content.",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("**Bold** text with  and *italic* content.", result[0].Content);
        Assert.Contains("**Bold**", result[0].Content);
        Assert.Contains("*italic*", result[0].Content);
        Assert.DoesNotContain("[link]", result[0].Content);
    }

    [Fact]
    public async Task Search_StripMarkdown_HandlesTextWithoutLinks()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "Plain text without any links.",
                    "raw_content": "Plain text without any links.",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Plain text without any links.", result[0].Content);
    }

    [Fact]
    public async Task Search_StripMarkdown_HandlesEmptyContent()
    {
        // Arrange
        var mockResponse = """
        {
            "query": "test query",
            "results": [
                {
                    "title": "Test Result",
                    "url": "https://example.com",
                    "content": "",
                    "raw_content": "",
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("", result[0].Content);
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
        var result = await service.Search(SearchContext.Create("C# programming"), 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result Count: {result.Count}");
        Assert.NotEmpty(result);
        Assert.True(result.Count <= 3);
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

}

