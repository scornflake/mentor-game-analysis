using System.Net;
using System.Text;
using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public class WebsearchTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WebsearchTests(ITestOutputHelper testOutputHelper)
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
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act
        var result = await service.Search("test query", SearchOutputFormat.Snippets, 5);

        // Assert
        Assert.Contains("First test snippet.", result);
        Assert.Contains("Second test snippet.", result);
        Assert.DoesNotContain("https://example.com", result);
        Assert.DoesNotContain("Test Result 1", result);
    }

    [Fact]
    public async Task Search_WithStructuredFormat_ReturnsFormattedResults()
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
                    }
                ]
            }
        }
        """;

        var mockFactory = CreateMockHttpClientFactory(mockResponse);
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act
        var result = await service.Search("test query", SearchOutputFormat.Structured, 5);

        // Assert
        Assert.Contains("Test Result 1", result);
        Assert.Contains("https://example.com/1", result);
        Assert.Contains("First test snippet.", result);
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
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act
        var result = await service.Search("test query", SearchOutputFormat.Summary, 5);

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
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act
        var result = await service.Search("test query", SearchOutputFormat.Structured, 2);

        // Assert
        Assert.Contains("Result 1", result);
        Assert.Contains("Result 2", result);
        Assert.DoesNotContain("Result 3", result);
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
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act
        var result = await service.Search("test query", SearchOutputFormat.Snippets, 5);

        // Assert
        Assert.Contains("No results found", result);
    }

    [Fact]
    public async Task Search_WithApiError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("Error", HttpStatusCode.Unauthorized);
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "invalid-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.Search("test query", SearchOutputFormat.Snippets, 5)
        );
    }

    [Fact]
    public async Task Search_WithNullQuery_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Search(null!, SearchOutputFormat.Snippets, 5)
        );
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = CreateMockHttpClientFactory("{}");
        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(mockFactory.Object, config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Search("", SearchOutputFormat.Snippets, 5)
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

        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(httpClientFactory.Object, config, logger);

        // Act
        var result = await service.Search("C# programming", SearchOutputFormat.Snippets, 3);

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

        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(httpClientFactory.Object, config, logger);

        // Act
        var result = await service.Search("dotnet 8", SearchOutputFormat.Structured, 2);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Structured): {result}");
        Assert.NotEmpty(result);
        Assert.Contains("http", result);
    }

    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task Search_WithRealApi_SummaryFormat_ReturnsResults()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("BRAVE_SEARCH_API_KEY");
        Assert.NotNull(apiKey);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var config = Options.Create(new BraveSearchConfiguration
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1"
        });

        var logger = NullLogger<Websearch>.Instance;
        var service = new Websearch(httpClientFactory.Object, config, logger);

        // Act
        var result = await service.Search("xUnit testing framework", SearchOutputFormat.Summary, 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result (Summary): {result}");
        Assert.NotEmpty(result);
    }
}

