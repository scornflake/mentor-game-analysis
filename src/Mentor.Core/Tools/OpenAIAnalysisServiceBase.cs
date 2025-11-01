using System.ComponentModel;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Serialization;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/// <summary>
/// Base class for OpenAI-based analysis services providing shared tool setup functionality
/// </summary>
public abstract class OpenAIAnalysisServiceBase : AnalysisService
{
    protected IWebSearchTool? _webSearchTool = null;
    protected List<ResearchResult>? _researchResults;
    protected readonly SearchResultFormatter _searchResultFormatter;

    protected OpenAIAnalysisServiceBase(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory, SearchResultFormatter searchResultFormatter) 
        : base(llmClient, logger, toolFactory)
    {
        _searchResultFormatter = searchResultFormatter ?? throw new ArgumentNullException(nameof(searchResultFormatter));
    }

    /// <summary>
    /// Sets up the web search tool (Tavily) for use by derived classes
    /// </summary>
    protected async Task SetupWebSearchTool()
    {
        _logger.LogInformation("Setting up web search tool");
        _webSearchTool = await _toolFactory.GetToolAsync(KnownSearchTools.Tavily);
        if (_webSearchTool == null)
        {
            throw new InvalidOperationException("Web search tool could not be created.");
        }
    }

    /// <summary>
    /// Performs a web search to find relevant information about the query
    /// </summary>
    [Description(
        "Performs a web search to find relevant information about the query. Returns a concise summary of up to 10 search results. Use this tool when you need actual content.")]
    protected async Task<string> SearchTheWebSummary([Description("The search query to perform.")] string query)
    {
        _logger.LogInformation("Performing web search for query: {Query}", query);
        var context = SearchContext.Create(query, _currentRequest?.GameName);
        var theseResults = await _webSearchTool!.Search(context, 10);
        return _searchResultFormatter.FormatAsSummary(theseResults);
    }

    /// <summary>
    /// Reads and extracts the full text content from an article or webpage
    /// </summary>
    [Description(
        "Reads and extracts the full text content from an article or webpage at the specified URL. Returns the complete article text for detailed analysis. Use this tool when you need comprehensive information from a specific webpage, whether found through search results or known URLs. Provides detailed content that can be cited in recommendations.")]
    protected async Task<string> ReadArticleContent(
        [Description("The full URL of the article or webpage to read. Must be a valid HTTP or HTTPS URL.")]
        string url)
    {
        _logger.LogInformation("Reading article content from URL: {Url}", url);
        var articleReader = await _toolFactory.GetArticleReaderAsync();
        return await articleReader.ReadArticleAsync(url);
    }

    protected override async Task<ChatOptions> CreateAIOptions()
    {
        var options = await base.CreateAIOptions();
        // add tools
        options.Tools = await SetupSearchAndArticleTools();
        options.ToolMode = ChatToolMode.RequireAny;
        return options;
    }

    /// <summary>
    /// Sets up AI tools for autonomous research (web search and article reading)
    /// </summary>
    protected async Task<IList<AITool>> SetupSearchAndArticleTools()
    {
        await SetupWebSearchTool();
        
        var options = new AIFunctionFactoryOptions { SerializerOptions = MentorJsonSerializerContext.CreateOptions() };
        return
        [
            AIFunctionFactory.Create(SearchTheWebSummary, options: options),
            AIFunctionFactory.Create(ReadArticleContent, options: options)
        ];
    }

    /// <summary>
    /// Override to inject research results into system prompt
    /// </summary>
    protected override string GetSystemPromptText(AnalysisRequest request)
    {
        var msg = base.GetSystemPromptText(request);
        if (_researchResults != null && _researchResults.Count > 0)
        {
            msg += "\n\nI have included results from my research below. Use this information to help you with your analysis.";
            foreach (var researchResult in _researchResults)
            {
                msg += $"\n\n{researchResult.Title}\n{researchResult.Url}\n{researchResult.Content}";
            }
        }

        return msg;
    }
}

