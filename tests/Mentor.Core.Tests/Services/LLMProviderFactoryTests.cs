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
            OpenAI = new OpenAIConfiguration
            {
                ApiKey = "test-api-key",
                Model = "sonar",
                BaseUrl = "https://api.perplexity.ai"
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
            OpenAI = new OpenAIConfiguration
            {
                ApiKey = "",
                Model = "sonar"
            }
        };
        var options = Options.Create(config);
        var factory = new LLMProviderFactory(options);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.GetProvider("openai"));
    }
}

