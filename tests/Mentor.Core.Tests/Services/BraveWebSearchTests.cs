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

public class BraveWebSearchTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public BraveWebSearchTests(ITestOutputHelper testOutputHelper)
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
    public async Task Search_WithSnippetsFormat_ReturnsConcatenatedSnippets()
    {
        // Arrange
        var mockResponse = """
        {
            "web": {
                "results": [
                    {
                        "title": "Test Result 1",
                        "url": "https://example.com/1",
                        "description": "First test snippet."
                    },
                    {
                        "title": "Test Result 2",
                        "url": "https://example.com/2",
                        "description": "Second test snippet."
                    }
                ]
            }
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
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
    public async Task Search_WithSummaryFormat_ReturnsSummarizedText()
    {
        // Arrange
        var mockResponse = """
        {
            "web": {
                "results": [
                    {
                        "title": "Test Result 1",
                        "url": "https://example.com/1",
                        "description": "First test snippet."
                    },
                    {
                        "title": "Test Result 2",
                        "url": "https://example.com/2",
                        "description": "Second test snippet."
                    }
                ]
            }
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("test query"), SearchOutputFormat.Summary, 5);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("test query", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_LimitsResultsToMaxResults()
    {
        // Arrange
        var mockResponse = """
        {
            "web": {
                "results": [
                    {"title": "Result 1", "url": "https://example.com/1", "description": "Snippet 1."},
                    {"title": "Result 2", "url": "https://example.com/2", "description": "Snippet 2."},
                    {"title": "Result 3", "url": "https://example.com/3", "description": "Snippet 3."},
                    {"title": "Result 4", "url": "https://example.com/4", "description": "Snippet 4."},
                    {"title": "Result 5", "url": "https://example.com/5", "description": "Snippet 5."}
                ]
            }
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
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
            "web": {
                "results": []
            }
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
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
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.Search(SearchContext.Create("test query"), SearchOutputFormat.Snippets, 5)
        );
    }

    [Fact]
    public async Task Search_WithNullQuery_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var config = new ToolConfigurationEntity
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
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
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Search(SearchContext.Create(""), SearchOutputFormat.Snippets, 5)
        );
    }
}

public class WebsearchIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WebsearchIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task Search_WithRealApi_SnippetsFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("BRAVE_SEARCH_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(httpClientFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("C# programming"), SearchOutputFormat.Snippets, 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Snippets): {result}");
        Assert.NotEmpty(result);
        Assert.DoesNotContain("No results found", result);
    }

    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task Search_WithRealApi_StructuredFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("BRAVE_SEARCH_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(httpClientFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.SearchStructured(SearchContext.Create("dotnet 8"), 2);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Structured): {result}");
        Assert.NotEmpty(result);
        Assert.Contains("http", result[0].Url);
    }

    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task Search_WithRealApi_SummaryFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("BRAVE_SEARCH_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(httpClientFactory.Object, logger);
        service.Configure(config);

        // Act
        var result = await service.Search(SearchContext.Create("xUnit testing framework"), SearchOutputFormat.Summary, 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Summary): {result}");
        Assert.NotEmpty(result);
    }
}

