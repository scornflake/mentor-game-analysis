using Mentor.Core.Configuration;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public class LLMProviderFactoryTests
{
    private readonly Mock<IWebsearch> _websearchMock;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMProviderFactoryTests> _logger;

    public LLMProviderFactoryTests(ITestOutputHelper testOutputHelper)
    {
        _websearchMock = new Mock<IWebsearch>();
        
        // Create a service provider with logging support for the tests
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        TestHelpers.AddWebSearchTool(_websearchMock.Object);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<LLMProviderFactoryTests>>();
    }

    [Fact]
    public void GetProvider_WithOpenAI_ReturnsChatClient()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            DefaultProvider = "openai",
            Providers = new Dictionary<string, OpenAIConfiguration>
            {
                ["openai"] = new OpenAIConfiguration
                {
                    ApiKey = "test-api-key",
                    Model = "sonar",
                    BaseUrl = "https://api.perplexity.ai"
                }
            }
        };
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options, _websearchMock.Object, _serviceProvider);

        // Act
        var provider = factory.GetProvider("openai");

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<IChatClient>(provider);
    }

    [Fact]
    public void GetProvider_WithInvalidProvider_ThrowsArgumentException()
    {
        // Arrange
        var config = new LLMConfiguration();
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options, _websearchMock.Object, _serviceProvider);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.GetProvider("invalid-provider"));
    }

    [Fact]
    public void GetProvider_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, OpenAIConfiguration>
            {
                ["openai"] = new OpenAIConfiguration
                {
                    ApiKey = "",
                    Model = "sonar",
                    IsLocal = false
                }
            }
        };
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options, _websearchMock.Object, _serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.GetProvider("openai"));
    }

    [Fact]
    public void GetProvider_WithLocalLLM_DoesNotRequireApiKey()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, OpenAIConfiguration>
            {
                ["local"] = new OpenAIConfiguration
                {
                    ApiKey = "",
                    Model = "llama3",
                    BaseUrl = "http://localhost:11434",
                    IsLocal = true
                }
            }
        };
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options, _websearchMock.Object, _serviceProvider);

        // Act
        var provider = factory.GetProvider("local");

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<IChatClient>(provider);
    }

    [Fact]
    public void GetProvider_WithLocalLLM_AndApiKey_StillWorks()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, OpenAIConfiguration>
            {
                ["local"] = new OpenAIConfiguration
                {
                    ApiKey = "not-needed-but-present",
                    Model = "llama3",
                    BaseUrl = "http://localhost:11434",
                    IsLocal = true
                }
            }
        };
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options, _websearchMock.Object, _serviceProvider);

        // Act
        var provider = factory.GetProvider("local");

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<IChatClient>(provider);
    }

    [Fact]
    public void GetProvider_WithMultipleProviders_LookupsCorrectConfiguration()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, OpenAIConfiguration>
            {
                ["openai"] = new OpenAIConfiguration
                {
                    ApiKey = "openai-key",
                    Model = "gpt-4",
                    BaseUrl = "https://api.openai.com"
                },
                ["local"] = new OpenAIConfiguration
                {
                    ApiKey = "",
                    Model = "llama3.2-vision:11b",
                    BaseUrl = "http://localhost:11434/v1",
                    IsLocal = true
                }
            }
        };
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options, _websearchMock.Object, _serviceProvider);

        // Act
        var openaiProvider = factory.GetProvider("openai");
        var localProvider = factory.GetProvider("local");

        // Assert
        Assert.NotNull(openaiProvider);
        Assert.NotNull(localProvider);
        Assert.IsAssignableFrom<IChatClient>(openaiProvider);
        Assert.IsAssignableFrom<IChatClient>(localProvider);
    }
}