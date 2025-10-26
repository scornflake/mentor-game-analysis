using Mentor.Core.Configuration;
using Mentor.Core.Services;
using OpenAI.Chat;
using Microsoft.Extensions.Options;

namespace Mentor.Core.Tests.Services;

public class LLMProviderFactoryTests
{
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
        var factory = new LLMProviderFactory(options);

        // Act
        var provider = factory.GetProvider("openai");

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<ChatClient>(provider);
    }

    [Fact]
    public void GetProvider_WithInvalidProvider_ThrowsArgumentException()
    {
        // Arrange
        var config = new LLMConfiguration();
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options);

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
        var factory = new LLMProviderFactory(options);

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
        var factory = new LLMProviderFactory(options);

        // Act
        var provider = factory.GetProvider("local");

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<ChatClient>(provider);
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
        var factory = new LLMProviderFactory(options);

        // Act
        var provider = factory.GetProvider("local");

        // Assert
        Assert.NotNull(provider);
        Assert.IsAssignableFrom<ChatClient>(provider);
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
        var factory = new LLMProviderFactory(options);

        // Act
        var openaiProvider = factory.GetProvider("openai");
        var localProvider = factory.GetProvider("local");

        // Assert
        Assert.NotNull(openaiProvider);
        Assert.NotNull(localProvider);
        Assert.IsAssignableFrom<ChatClient>(openaiProvider);
        Assert.IsAssignableFrom<ChatClient>(localProvider);
    }
}

