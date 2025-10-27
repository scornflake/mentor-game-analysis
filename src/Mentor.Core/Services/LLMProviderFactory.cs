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
    private readonly ProviderImplementationsConfiguration? _implementationsConfiguration;
    private readonly IServiceProvider _serviceProvider;

    public LLMProviderFactory(IOptions<ProviderImplementationsConfiguration> configuration, IServiceProvider serviceProvider)
    {
        _implementationsConfiguration = configuration.Value;
        _serviceProvider = serviceProvider;
    }

    public IAnalysisService GetAnalysisService(ILLMClient llmClient)
    {
        // make an AnalaysisService based on the name
        var analysisLogging = _serviceProvider.GetRequiredService<ILogger<AnalysisService>>();
        var toolFactory = _serviceProvider.GetRequiredService<IToolFactory>();
        var analysisService = new AnalysisService(llmClient, analysisLogging, toolFactory);
        return analysisService;
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

        // Get implementation details from configuration
        ProviderImplementationDetails? implementationDetails = null;
        if (_implementationsConfiguration?.ProviderImplementations != null)
        {
            _implementationsConfiguration.ProviderImplementations.TryGetValue(
                normalizedProviderType, out implementationDetails);
        }

        if (implementationDetails == null)
        {
            throw new ArgumentException(
                $"Provider type '{config.ProviderType}' is not supported. " +
                $"Add configuration under ProviderImplementations:{config.ProviderType} in appsettings.json",
                nameof(config));
        }

        // Apply defaults if not specified
        var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) 
            ? implementationDetails.DefaultBaseUrl 
            : config.BaseUrl;
        var model = string.IsNullOrWhiteSpace(config.Model)
            ? implementationDetails.DefaultModel
            : config.Model;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("BaseUrl must be specified either in config or as a default", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model must be specified either in config or as a default", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                $"API key for provider '{config.ProviderType}' is required. " +
                $"Provide it via the ApiKey property in ProviderConfiguration.");
        }

        // Create OpenAIConfiguration for compatibility with existing client creation
        var openAIConfig = new OpenAIConfiguration
        {
            ApiKey = config.ApiKey,
            Model = model,
            BaseUrl = baseUrl,
            Timeout = config.Timeout
        };

        // Instantiate the appropriate client based on provider type
        var chatClient = normalizedProviderType switch
        {
            "openai" => CreateOpenAIClient(openAIConfig),
            "perplexity" => CreatePerplexityClient(openAIConfig),
            _ => throw new ArgumentException(
                $"Provider type '{config.ProviderType}' does not have an implementation.",
                nameof(config))
        };

        return new LLMClient(openAIConfig, chatClient);
    }

    private IChatClient CreateOpenAIClient(OpenAIConfiguration config)
    {
        // OpenAI requires an API key
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                "API key for OpenAI provider is required.");
        }

        var credential = new ApiKeyCredential(config.ApiKey);
        
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl),
        };
        
        // Create OpenAI ChatClient
        var openAIClient = new ChatClient(config.Model, credential, options);
        
        // Wrap with middleware
        var newClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseLogging()
            .UseFunctionInvocation()
            .Build(_serviceProvider);
        
        return newClient;
    }

    private IChatClient CreatePerplexityClient(OpenAIConfiguration config)
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
            .UseFunctionInvocation()
            .Build(_serviceProvider);
        
        return newClient;
    }
}

