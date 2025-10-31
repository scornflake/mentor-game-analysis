using Mentor.Core.Configuration;
using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Services;

public interface IToolFactory
{
    Task<IWebSearchTool> GetToolAsync(string toolName);
    Task<IArticleReader> GetArticleReaderAsync();
    ITextSummarizer CreateTextSummarizer(ILLMClient llmClient);
    IHtmlToMarkdownConverter CreateHtmlToMarkdownConverter(ILLMClient llmClient, int maxLinesToConvert = 1000);
}

public class ToolFactory : IToolFactory
{
    private readonly ILogger<ToolFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationRepository _configurationRepository;

    public ToolFactory(ILogger<ToolFactory> logger, IServiceProvider serviceProvider, IConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configurationRepository = configurationRepository;
    }

    public async Task<IWebSearchTool> GetToolAsync(string toolName) 
    {
        _logger.LogInformation("Creating tool of type '{ToolName}'", toolName);
        
        // we know of a fixed list
        if(toolName.ToLower() == KnownSearchTools.Brave)
        {
            var tool = _serviceProvider.GetRequiredKeyedService<IWebSearchTool>(toolName.ToLower());
            var config = await _configurationRepository.GetToolByNameAsync(toolName);
            if (config == null)
            {
                throw new InvalidOperationException("BraveWebSearch tool configuration is not found.");
            }
            tool.Configure(config);
            if (tool == null)
            {
                throw new InvalidOperationException("BraveWebSearch service is not registered.");
            }
            return tool;
        }
        else if(toolName.ToLower() == KnownSearchTools.Tavily)
        {
            var tool = _serviceProvider.GetRequiredKeyedService<IWebSearchTool>(toolName.ToLower());
            var config = await _configurationRepository.GetToolByNameAsync(toolName);
            if (config == null)
            {
                throw new InvalidOperationException("TavilyWebSearch tool configuration is not found.");
            }
            tool.Configure(config);
            if (tool == null)
            {
                throw new InvalidOperationException("TavilyWebSearch service is not registered.");
            }
            return tool;
        }
        else
        {
            throw new NotSupportedException($"Tool type '{toolName}' is not supported.");
        }
        
    }

    public async Task<IArticleReader> GetArticleReaderAsync()
    {
        _logger.LogInformation("Creating article reader tool");
        
        var articleReader = _serviceProvider.GetRequiredKeyedService<IArticleReader>(KnownTools.ArticleReader);
        var config = await _configurationRepository.GetToolByNameAsync(KnownTools.ArticleReader);
        
        // If no config exists, create a default one
        if (config == null)
        {
            _logger.LogInformation("No article reader configuration found, using default settings");
            config = new ToolConfigurationEntity
            {
                ToolName = KnownTools.ArticleReader,
                Timeout = 30,
                MaxArticleLength = 500,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        
        articleReader.Configure(config);
        return articleReader;
    }

    public ITextSummarizer CreateTextSummarizer(ILLMClient llmClient)
    {
        if (llmClient == null)
        {
            throw new ArgumentNullException(nameof(llmClient));
        }

        _logger.LogInformation("Creating text summarizer tool");
        
        var logger = _serviceProvider.GetRequiredService<ILogger<TextSummarizer>>();
        return new TextSummarizer(llmClient, logger);
    }

    public IHtmlToMarkdownConverter CreateHtmlToMarkdownConverter(ILLMClient llmClient, int maxLinesToConvert = 1000) {
        if (llmClient == null) {
            throw new ArgumentNullException(nameof(llmClient));
        }
        
        _logger.LogInformation("Creating HTML to Markdown converter tool");
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        return new LlmHtmlToMarkdownConverter(llmClient, logger, maxLinesToConvert);
    }
}