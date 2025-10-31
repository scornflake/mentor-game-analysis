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
    public async Task Search_ReturnsSearchResults()
    {
        // Arrange
        var mockResponse = """
        {
            "web": {
                "results": [
                    {
                        "title": "Test Result 1",
                        "url": "https://example.com/1",
                        "content": "First test snippet."
                    },
                    {
                        "title": "Test Result 2",
                        "url": "https://example.com/2",
                        "content": "Second test snippet."
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
        var result = await service.Search(SearchContext.Create("test query"), 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Test Result 1", result[0].Title);
        Assert.Equal("https://example.com/1", result[0].Url);
        Assert.Equal("First test snippet.", result[0].Content);
        Assert.Equal("Test Result 2", result[1].Title);
    }


    [Fact]
    public async Task Search_LimitsResultsToMaxResults()
    {
        // Arrange
        var mockResponse = """
        {
            "web": {
                "results": [
                    {"title": "Result 1", "url": "https://example.com/1", "content": "Snippet 1."},
                    {"title": "Result 2", "url": "https://example.com/2", "content": "Snippet 2."},
                    {"title": "Result 3", "url": "https://example.com/3", "content": "Snippet 3."},
                    {"title": "Result 4", "url": "https://example.com/4", "content": "Snippet 4."},
                    {"title": "Result 5", "url": "https://example.com/5", "content": "Snippet 5."}
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
    public async Task Search_WithEmptyResults_ReturnsEmptyList()
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
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.Search(SearchContext.Create("test query"), 5)
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
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };

        var logger = NullLogger<BraveWebSearch>.Instance;
        var service = new BraveWebSearch(mockFactory.Object, logger);
        service.Configure(config);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.Search(SearchContext.Create(""), 5)
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
    public async Task Search_WithRealApi_ReturnsResults()
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
        var result = await service.Search(SearchContext.Create("C# programming"), 3);

        // Assert
        _testOutputHelper.WriteLine($"Search Result Count: {result.Count}");
        Assert.NotEmpty(result);
        Assert.True(result.Count <= 3);
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

}

