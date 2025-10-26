using Mentor.Core.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Services;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly LLMConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public LLMProviderFactory(IOptions<LLMConfiguration> configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration.Value;
        _serviceProvider = serviceProvider;
    }

    public ILLMClient GetProvider(string providerName)
    {
        var normalizedProviderName = providerName.ToLowerInvariant();
        
        // Look up the configuration for the specified provider
        if (!_configuration.Providers.TryGetValue(normalizedProviderName, out var config))
        {
            throw new ArgumentException(
                $"Provider '{providerName}' is not configured. " +
                $"Add configuration under LLM:Providers:{providerName} in appsettings.json",
                nameof(providerName));
        }

        var chatClient = CreateOpenAICompatibleClient(config, providerName);
        return new LLMClient(config, chatClient);
    }

    private IChatClient CreateOpenAICompatibleClient(OpenAIConfiguration config, string providerName)
    {
        // For providers that require an API key, ensure it's configured
        if (config.IsApiKeyPresent && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                $"API key for provider '{providerName}' is not configured. " +
                $"Set it in appsettings.json under LLM:Providers:{providerName}:ApiKey " +
                $"or via environment variable LLM__Providers__{providerName}__ApiKey");
        }

        // Using OpenAI ChatClient with custom endpoint (works with any OpenAI-compatible API)
        // For providers that don't require an API key, use a dummy value if none is provided
        var apiKey = string.IsNullOrWhiteSpace(config.ApiKey) ? "not-needed" : config.ApiKey;
        var credential = new ApiKeyCredential(apiKey);
        
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl),
        };
        
        // first make the actual OpenAI ChatClient
        var openAIClient = new ChatClient(config.Model, credential, options);
        
        // wrap it to add logging, function invocation, and other middleware
        var newClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseLogging()
            .UseFunctionInvocation()
            .Build(_serviceProvider);
        
        return newClient;
    }
}

