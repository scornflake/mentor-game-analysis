using System.ComponentModel;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class OpenAIAnalysisService : AnalysisService
{
    private IWebSearchTool? _webSearchTool = null;
    private IArticleReader? _articleReader = null;
    private AnalysisRequest? _currentRequest = null;
    private List<SearchResult> _searchResults = new List<SearchResult>();

    public OpenAIAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory) : base(llmClient, logger, toolFactory)
    {
    }

    [Description("Tool to perform web searches and return structured results. Good if you want detailed information")]
    async Task<IList<SearchResult>> SearchTheWebStructured(string query)
    {
        _logger.LogInformation("Performing structured web search for query: {Query}", query);
        var context = SearchContext.Create(query, _currentRequest?.GameName);
        var theseResults = await _webSearchTool!.SearchStructured(context, 5);
        _searchResults.AddRange(theseResults);
        return theseResults;
    }

    [Description("Tool to read the full content of an article from a URL. Use this after finding relevant URLs from web search to get detailed information from specific articles.")]
    async Task<string> ReadArticleContent(string url)
    {
        _logger.LogInformation("Reading article content from URL: {Url}", url);
        return await _articleReader!.ReadArticleAsync(url);
    }

    protected override string GetSystemPromptText(AnalysisRequest request)
    {
        var msg = base.GetSystemPromptText(request);
        msg += "\n\n" +
               "You have access to a web search tool that can be used to find relevant information. " +
               "Use the web search tool to find relevant information when necessary. " +
               "After finding relevant URLs from web search results, you can use the article reading tool to read the full content of specific articles. " +
               "Read the full article content when you need detailed information from a specific source. " +
               "Use the article reading tool to get comprehensive information from articles returned by web search and use that information to backup your analysis and recommendations.";
        return msg;
    }

    internal override async Task<IList<AITool>> SetupTools()
    {
        await base.SetupTools();

        // this provider currently only supports Brave
        _webSearchTool = await _toolFactory.GetToolAsync(KnownSearchTools.Brave);
        if (_webSearchTool == null)
        {
            throw new InvalidOperationException("Web search tool could not be created.");
        }

        _articleReader = await _toolFactory.GetArticleReaderAsync();
        if (_articleReader == null)
        {
            throw new InvalidOperationException("Article reader tool could not be created.");
        }

        return [
            AIFunctionFactory.Create(SearchTheWebStructured), 
            AIFunctionFactory.Create(ReadArticleContent)
            // AIFunctionFactory.Create(SearchTheWebSnippets)
        ];
    }

    protected override async Task<ChatOptions> CreateAIOptions()
    {
        var options = await base.CreateAIOptions();
        options.AllowMultipleToolCalls = true;
        options.ToolMode = ChatToolMode.RequireAny;
        options.Tools = await SetupTools();

        return options;
    }

    public override async Task<Recommendation> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken = default)
    {
        request.ValidateRequest();

        // Store request for use by tool methods when creating search context
        _currentRequest = request;

        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages(request);

        var messages = new List<ChatMessage> { systemMessage, userMessage };
        var options = await CreateAIOptions();

        return await ExecuteAndParse(messages, options, cancellationToken);
    }
}