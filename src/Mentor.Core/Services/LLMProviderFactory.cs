using Mentor.Core.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Mentor.Core.Interfaces;
using Mentor.Core.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Services;

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LLMProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAnalysisService GetAnalysisService(ILLMClient llmClient)
    {
        // make an AnalaysisService based on the name
        var toolFactory = _serviceProvider.GetRequiredService<IToolFactory>();
        var providerName = llmClient.Configuration.ProviderType;
        ValidateProviderName(providerName);

        if (providerName == "openai")
        {
            var analysisLogging = _serviceProvider.GetRequiredService<ILogger<OpenAIAnalysisService>>();
            analysisLogging.LogInformation("Creating AnalysisService for OpenAI provider.");
            return new OpenAIAnalysisService(llmClient, analysisLogging, toolFactory);
        }
        else if (providerName == "perplexity")
        {
            var analysisLogging = _serviceProvider.GetRequiredService<ILogger<PerplexityAnalysisService>>();
            analysisLogging.LogInformation("Creating AnalysisService for Perplexity provider.");
            return new PerplexityAnalysisService(llmClient, analysisLogging, toolFactory);
        }
        throw new NotSupportedException($"AnalysisService does not support provider '{providerName}'.");
    }

    private void ValidateProviderName(string providerName)
    {
        if (providerName != "openai" && providerName != "perplexity")
        {
            throw new NotSupportedException($"AnalysisService does not support provider '{providerName}'.");
        }
    }

    public ILLMClient GetProvider(ProviderConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ProviderType))
        {
            throw new ArgumentException("ProviderType must be specified", nameof(config));
        }

        var normalizedProviderType = config.ProviderType.ToLowerInvariant();

        // Validate required fields
        var baseUrl = config.BaseUrl;
        var model = config.Model;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("BaseUrl must be specified either in config or as a default", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model must be specified either in config or as a default", nameof(config));
        }

        // Instantiate the appropriate client based on provider type
        var chatClient = normalizedProviderType switch
        {
            "openai" => CreateOpenAIClient(config),
            "perplexity" => CreatePerplexityClient(config),
            _ => throw new ArgumentException(
                $"Provider type '{config.ProviderType}' does not have an implementation.",
                nameof(config))
        };

        return new LLMClient(config, chatClient);
    }

    private IChatClient CreateOpenAIClient(ProviderConfiguration config)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl),
        };

        // OpenAI does not necessarily require an API key (e.g., for local deployments)
        var apiKey = string.IsNullOrWhiteSpace(config.ApiKey) ? "not-needed" : config.ApiKey;
        var credential = new ApiKeyCredential(apiKey);
        var openAIClient = new ChatClient(config.Model, credential, options);

        // Wrap with middleware
        var newClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseLogging()
            .UseFunctionInvocation()
            .Build(_serviceProvider);

        return newClient;
    }

    private IChatClient CreatePerplexityClient(ProviderConfiguration config)
    {
        // Perplexity requires an API key
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                "API key for Perplexity provider is required.");
        }

        var credential = new ApiKeyCredential(config.ApiKey);

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl),
        };

        // Perplexity uses OpenAI-compatible API
        var openAIClient = new ChatClient(config.Model, credential, options);

        // Wrap with middleware
        var newClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseLogging()
            .Build(_serviceProvider);

        return newClient;
    }
}