using System.ComponentModel;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace Mentor.Core.Tools;

public class OpenAIAnalysisService : AnalysisService
{

    private IWebSearchTool? _webSearchTool = null;
    private IArticleReader? _articleReader = null;
    private AnalysisRequest? _currentRequest = null;
    private List<ResearchResult>? _researchResults;
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
        if(_researchResults != null && _researchResults.Count > 0)
        {
            msg += "\n\nI have included results from my research below. Use this information to help you with your analysis.";
            foreach (var researchResult in _researchResults)
            {
                msg += $"\n\n{researchResult.Title}\n{researchResult.Url}\n{researchResult.Content}";
            }
        }
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

    public override async Task<Recommendation> AnalyzeAsync(AnalysisRequest request, IProgress<AnalysisProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        request.ValidateRequest();

        // Store request for use by tool methods when creating search context
        _currentRequest = request;

        var options = await CreateAIOptions();
        _researchResults = await PerformResearch(request, progress, cancellationToken);

        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages(request);

        var messages = new List<ChatMessage> { systemMessage, userMessage };

        return await ExecuteAndParse(messages, options, progress, cancellationToken);
    }

    private async Task<List<ResearchResult>> PerformResearch(AnalysisRequest request, IProgress<AnalysisProgress>? progress, CancellationToken cancellationToken)
    {
        var researchResults = new List<ResearchResult>();
        
        // Create and initialize progress tracking
        var analysisProgress = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new AnalysisJob { Tag = "search-web", Name = "Searching web", Status = JobStatus.InProgress, Progress = 0 },
                new AnalysisJob { Tag = "analyze-llm", Name = "Analyzing with LLM", Status = JobStatus.Pending, Progress = 0 }
            }
        };
        analysisProgress.ReportProgress(progress);
        
        // search based on the prompt
        var searchResults = await SearchTheWebStructured(request.Prompt);
        
        // Update search job to completed
        analysisProgress.UpdateJobProgress("search-web", JobStatus.Completed, 100);
        analysisProgress.ReportProgress(progress);
        
        // Add article processing jobs (reading + converting combined)
        for (int i = 0; i < searchResults.Count; i++)
        {
            analysisProgress.AddJobAbove(
                referenceTag: "analyze-llm",
                newJob: new AnalysisJob 
                { 
                    Tag = $"article-{i + 1}",
                    Name = $"Processing article {i + 1}: {searchResults[i].Title}", 
                    Status = JobStatus.Pending,
                    Progress = 0
                }
            );
        }
        
        //var summarizer = _toolFactory.CreateTextSummarizer(_llmClient);
        var htmlToMarkdownConverter = _toolFactory.CreateHtmlToMarkdownConverter(_llmClient);
        
        analysisProgress.ReportProgress(progress);

        // Process articles (read + convert combined)
        int articleIndex = 0;
        foreach (var searchResult in searchResults)
        {
            var articleTag = $"article-{articleIndex + 1}";
            
            // Update processing job
            analysisProgress.UpdateJobProgress(articleTag, JobStatus.InProgress, 0);
            analysisProgress.SetName(articleTag, $"Reading article {articleIndex + 1}: {searchResult.Title}");
            analysisProgress.ReportProgress(progress);
            
            // Read article (first half of the work)
            var articleContent = await ReadArticleContent(searchResult.Url);
            
            analysisProgress.UpdateJobProgress(articleTag, JobStatus.InProgress, 50);
            analysisProgress.SetName(articleTag, $"Converting article {articleIndex + 1}: {searchResult.Title}");
            analysisProgress.ReportProgress(progress);
            
            // Convert to markdown (second half of the work)
            var markdown = await htmlToMarkdownConverter.ConvertAsync(articleContent, cancellationToken);
            
            analysisProgress.UpdateJobProgress(articleTag, JobStatus.Completed, 100);
            analysisProgress.SetName(articleTag, $"Converted article {articleIndex + 1}: {searchResult.Title}");
            analysisProgress.ReportProgress(progress);
            
            researchResults.Add(new ResearchResult { Title = searchResult.Title, Url = searchResult.Url, Content = markdown });
            
            articleIndex++;
        }
        
        return researchResults;
    }
    
    protected override async Task<Recommendation> ExecuteAndParse(List<ChatMessage> messages, ChatOptions options, IProgress<AnalysisProgress>? progress, CancellationToken cancellationToken)
    {
        var analysisProgress = new AnalysisProgress();
        analysisProgress.UpdateJobProgress("analyze-llm", JobStatus.InProgress, 0);
        analysisProgress.ReportProgress(progress);
        
        var result = await base.ExecuteAndParse(messages, options, progress, cancellationToken);

        // Report completion
        analysisProgress.UpdateJobProgress("analyze-llm", JobStatus.Completed, 100);
        analysisProgress.ReportProgress(progress);

        return result;
    }
}

internal class ResearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}