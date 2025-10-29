using System.ComponentModel;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class OpenAIAnalysisService : AnalysisService
{
    // Tool names - these match the method names that OpenAI will see
    private const string ToolNameSearchTheWebStructured = "SearchTheWebStructured";
    private const string ToolNameReadArticleContent = "ReadArticleContent";

    private IWebSearchTool? _webSearchTool = null;
    private IArticleReader? _articleReader = null;
    private AnalysisRequest? _currentRequest = null;
    private List<SearchResult> _searchResults = new List<SearchResult>();

    public OpenAIAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory) : base(llmClient, logger, toolFactory)
    {
    }

    [Description("Performs a structured web search to find relevant information about the query. Returns up to 5 search results with titles, URLs, descriptions, and snippets. Use this tool when you need to find factual information, current data, or specific details about game mechanics, builds, strategies, or other topics. The search results include structured metadata that can help you provide accurate recommendations with proper references. Call this tool with a clear, specific search query.")]
    async Task<IList<SearchResult>> SearchTheWebStructured(string query)
    {
        _logger.LogInformation("Performing structured web search for query: {Query}", query);
        var context = SearchContext.Create(query, _currentRequest?.GameName);
        var theseResults = await _webSearchTool!.SearchStructured(context, 5);
        _searchResults.AddRange(theseResults);
        return theseResults;
    }

    [Description("Reads and extracts the full text content from an article or webpage at the specified URL. Returns the complete article text for detailed analysis. Use this tool when you need comprehensive information from a specific webpage, whether found through search results or known URLs. Provides detailed content that can be cited in recommendations.")]
    async Task<string> ReadArticleContent(
        [Description("The full URL of the article or webpage to read. Must be a valid HTTP or HTTPS URL.")] 
        string url)
    {
        _logger.LogInformation("Reading article content from URL: {Url}", url);
        return await _articleReader!.ReadArticleAsync(url);
    }

    protected override string GetSystemPromptText(AnalysisRequest request)
    {
        var msg = base.GetSystemPromptText(request);
        msg += "\n\n" +
               $"You have access to two tools for gathering information:\n" +
               $"- {ToolNameSearchTheWebStructured}: Searches the web and returns up to 5 structured results with titles, URLs, descriptions, and snippets. Use when you need to find information, verify facts, or get current data.\n" +
               $"- {ToolNameReadArticleContent}: Reads the complete text content from any webpage URL. Use this with URLs from search results OR any known URLs when you need full article details. This provides comprehensive content for citations.\n\n" +
               $"Workflow: Search first using {ToolNameSearchTheWebStructured} to find relevant URLs, then use {ToolNameReadArticleContent} with specific URLs when you need deeper information. " +
               $"You can also use {ToolNameReadArticleContent} directly with any known URLs.";
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