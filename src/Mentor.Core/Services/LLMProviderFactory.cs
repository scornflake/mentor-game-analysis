using Mentor.Core.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Services;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IWebsearch? _websearch;
    private readonly LLMConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public LLMProviderFactory(IOptions<LLMConfiguration> configuration, IWebsearch websearch, IServiceProvider serviceProvider)
    {
        _websearch = websearch;
        _configuration = configuration.Value;
        _serviceProvider = serviceProvider;
    }

    public IChatClient GetProvider(string providerName)
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

        return CreateOpenAICompatibleClient(config, providerName);
    }

    private IChatClient CreateOpenAICompatibleClient(OpenAIConfiguration config, string providerName)
    {
        // For non-local LLMs, API key is required
        if (!config.IsLocal && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                $"API key for provider '{providerName}' is not configured. " +
                $"Set it in appsettings.json under LLM:Providers:{providerName}:ApiKey " +
                $"or via environment variable LLM__Providers__{providerName}__ApiKey");
        }

        // Using OpenAI ChatClient with custom endpoint (works with any OpenAI-compatible API)
        // For local LLMs, use a dummy API key if none is provided
        var apiKey = string.IsNullOrWhiteSpace(config.ApiKey) ? "not-needed" : config.ApiKey;
        var credential = new ApiKeyCredential(apiKey);
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl)
        };
        
        var openAIClient = new ChatClient(config.Model, credential, options); 
        var newClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseLogging()
            .UseFunctionInvocation()
            .Build(_serviceProvider);
        return newClient;
    }
}

