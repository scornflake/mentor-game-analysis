using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Services;
using Mentor.Core.Tools;
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
    private readonly Mock<IWebSearchTool> _websearchMock;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMProviderFactoryTests> _logger;

    public LLMProviderFactoryTests(ITestOutputHelper testOutputHelper)
    {
        _websearchMock = new Mock<IWebSearchTool>();
        
        // Create a service provider with logging support for the tests
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        services.AddWebSearchTool(_websearchMock.Object);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<LLMProviderFactoryTests>>();
    }

    [Fact]
    public void GetProvider_WithProviderConfiguration_OpenAI_ReturnsLLMClient()
    {
        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "test-api-key",
            Model = "gpt-4o-mini",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        // Act
        var provider = factory.GetProvider(providerConfig);

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<ILLMClient>(provider);
        Assert.NotNull(provider.ChatClient);
    }

    [Fact]
    public void GetProvider_WithProviderConfiguration_Perplexity_ReturnsLLMClient()
    {
        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfiguration
        {
            ProviderType = "perplexity",
            ApiKey = "test-api-key",
            Model = "sonar",
            BaseUrl = "https://api.perplexity.ai"
        };

        // Act
        var provider = factory.GetProvider(providerConfig);

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<ILLMClient>(provider);
        Assert.NotNull(provider.ChatClient);
    }

    [Fact]
    public void GetProvider_WithProviderConfiguration_UnsupportedProvider_ThrowsArgumentException()
    {
        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfiguration
        {
            ProviderType = "unsupported-provider",
            ApiKey = "test-api-key"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.GetProvider(providerConfig));
    }

    [Fact]
    public void GetProvider_WithProviderConfiguration_EmptyApiKey_ThrowsInvalidOperationException()
    {
        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfiguration
        {
            ProviderType = "perplexity",
            ApiKey = "",
            Model = "sonar"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.GetProvider(providerConfig));
    }
}