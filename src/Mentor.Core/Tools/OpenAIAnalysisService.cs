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
    private AnalysisRequest? _currentRequest = null;
    private List<SearchResult> _searchResults = new List<SearchResult>();

    public OpenAIAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory) : base(llmClient, logger, toolFactory)
    {
    }

    // [Description("Tool to perform web searchesa and return text snippets")]
    // async Task<string> SearchTheWebSnippets(string query)
    // {
    //     _logger.LogInformation("Performing web search for query: {Query}", query);
    //     var context = SearchContext.Create(query, _currentRequest?.GameName);
    //     return await _webSearchTool!.Search(context, SearchOutputFormat.Snippets, 5);
    // }

    [Description("Tool to perform web searches and return structured results. Good if you want detailed information")]
    async Task<IList<SearchResult>> SearchTheWebStructured(string query)
    {
        _logger.LogInformation("Performing structured web search for query: {Query}", query);
        var context = SearchContext.Create(query, _currentRequest?.GameName);
        var theseResults = await _webSearchTool!.SearchStructured(context, 5);
        _searchResults.AddRange(theseResults);
        return theseResults;
    }

    protected override string GetSystemPromptText(AnalysisRequest request)
    {
        var msg = base.GetSystemPromptText(request);
        msg += "\n\n" +
               "You have access to a web search tool that can be used to find relevant information. " +
               "Use the web search tool to find relevant information when necessary. " +
               "Read each of the articles returned by the web search tool and use the information to backup your analysis and recommendations. ";
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

        return [
            AIFunctionFactory.Create(SearchTheWebStructured), 
            // AIFunctionFactory.Create(SearchTheWebSnippets)
        ];
    }

    protected override async Task<ChatOptions> CreateAIOptions()
    {
        var options = await base.CreateAIOptions();
        // options.AllowMultipleToolCalls = true;
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