using Mentor.Core.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Mentor.Core.Services;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly LLMConfiguration _configuration;

    public LLMProviderFactory(IOptions<LLMConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public ChatClient GetProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "openai" => CreateOpenAIClient(),
            _ => throw new ArgumentException($"Unknown provider: {providerName}", nameof(providerName))
        };
    }

    private ChatClient CreateOpenAIClient()
    {
        var config = _configuration.OpenAI;
        
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI-compatible API key is not configured. " +
                "Set it in appsettings.json or via environment variable LLM__OpenAI__ApiKey");
        }

        // Using OpenAI ChatClient with custom endpoint (e.g., Perplexity is OpenAI-compatible)
        var credential = new ApiKeyCredential(config.ApiKey);
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl)
        };
        
        return new ChatClient(config.Model, credential, options);
    }
}

