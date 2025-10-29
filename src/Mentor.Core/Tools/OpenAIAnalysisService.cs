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

    public OpenAIAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory) : base(llmClient, logger, toolFactory)
    {
    }

    [Description("Tool to perform web searchesa and return text snippets")]
    async Task<string> SearchTheWebSnippets(string query)
    {
        _logger.LogInformation("Performing web search for query: {Query}", query);
        return await _webSearchTool!.Search(query, SearchOutputFormat.Snippets, 5);
    }

    [Description("Tool to perform web searches and return structured results. Good if you want detailed information")]
    async Task<IList<SearchResult>> SearchTheWebStructured(string query)
    {
        _logger.LogInformation("Performing structured web search for query: {Query}", query);
        return await _webSearchTool!.SearchStructured(query, 5);
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
            AIFunctionFactory.Create(SearchTheWebSnippets)
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

        var systemMessage = GetSystemPrompt();
        var userMessage = GetUserMessages(request);

        var messages = new List<ChatMessage> { systemMessage, userMessage };
        var options = await CreateAIOptions();

        return await ExecuteAndParse(messages, options, cancellationToken);
    }
}