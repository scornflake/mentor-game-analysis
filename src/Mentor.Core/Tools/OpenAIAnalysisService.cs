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
        
        // Report initial jobs
        var jobs = new List<AnalysisJob>
        {
            new AnalysisJob { Name = "Searching web", Status = JobStatus.InProgress, Progress = 0 },
            new AnalysisJob { Name = "Analyzing with LLM", Status = JobStatus.Pending, Progress = 0 }
        };
        
        ReportProgress(progress, 0, jobs);
        
        // search based on the prompt
        var searchResults = await SearchTheWebStructured(request.Prompt);
        
        // Update search job to completed
        jobs[0].Status = JobStatus.Completed;
        jobs[0].Progress = 100;
        
        // Add article processing jobs (reading + converting combined)
        for (int i = 0; i < searchResults.Count; i++)
        {
            jobs.Add(new AnalysisJob 
            { 
                Name = $"Processing article {i + 1}: {searchResults[i].Title}", 
                Status = JobStatus.Pending,
                Progress = 0
            });
        }
        
        var summarizer = _toolFactory.CreateTextSummarizer(_llmClient);
        var htmlToMarkdownConverter = _toolFactory.CreateHtmlToMarkdownConverter(_llmClient);

        // Calculate progress weights:
        // - Search: 10% of research phase (7% of total)
        // - Processing articles (read + convert): 90% of research phase (63% of total)
        // Research phase total: 70% of overall
        const double researchPhaseWeight = 0.70;
        const double searchWeight = 0.10; // 10% of research phase
        const double processingWeight = 0.90; // 90% of research phase
        double searchProgress = searchWeight * researchPhaseWeight * 100; // ~7%
        
        ReportProgress(progress, searchProgress, jobs);

        // Process articles (read + convert combined)
        int articleIndex = 0;
        foreach (var searchResult in searchResults)
        {
            // Update processing job (single job per article now)
            int processingJobIndex = 1 + articleIndex; // Skip search job (index 0) and LLM job (last)
            
            jobs[processingJobIndex].Status = JobStatus.InProgress;
            jobs[processingJobIndex].Progress = 0;
            
            ReportProgress(progress, 0, jobs);
            
            // Read article (first half of the work)
            var articleContent = await ReadArticleContent(searchResult.Url);
            
            jobs[processingJobIndex].Progress = 50;
            ReportProgress(progress, 0, jobs);
            
            // Convert to markdown (second half of the work)
            var markdown = await htmlToMarkdownConverter.ConvertAsync(articleContent, cancellationToken);
            
            jobs[processingJobIndex].Status = JobStatus.Completed;
            jobs[processingJobIndex].Progress = 100;
            
            // Calculate overall progress: each article is equal portion of processing weight
            double progressPerArticle = processingWeight / searchResults.Count;
            double currentProgress = (searchWeight + (articleIndex + 1) * progressPerArticle) * researchPhaseWeight * 100;
            
            ReportProgress(progress, currentProgress, jobs);
            
            researchResults.Add(new ResearchResult { Title = searchResult.Title, Url = searchResult.Url, Content = markdown });
            
            articleIndex++;
        }
        
        // Research phase complete - report 70%
        ReportProgress(progress, researchPhaseWeight * 100, jobs);

        return researchResults;
    }
    
    private void ReportProgress(IProgress<AnalysisProgress>? progress, double totalPercentage, List<AnalysisJob> jobs)
    {
        if (progress != null)
        {
            progress.Report(new AnalysisProgress
            {
                TotalPercentage = Math.Min(100, Math.Max(0, totalPercentage)),
                Jobs = jobs.ToList() // Create a copy to avoid modification issues
            });
        }
    }
    
    protected override async Task<Recommendation> ExecuteAndParse(List<ChatMessage> messages, ChatOptions options, IProgress<AnalysisProgress>? progress, CancellationToken cancellationToken)
    {
        // Report LLM analysis starting
        var jobs = new List<AnalysisJob>
        {
            new AnalysisJob { Name = "Searching web", Status = JobStatus.Completed, Progress = 100 },
            new AnalysisJob { Name = "Analyzing with LLM", Status = JobStatus.InProgress, Progress = 0 }
        };
        
        // All research jobs are completed at this point
        // We'll add completed article jobs if we have research results
        if (_researchResults != null && _researchResults.Count > 0)
        {
            for (int i = 0; i < _researchResults.Count; i++)
            {
                jobs.Add(new AnalysisJob { Name = $"Processing article {i + 1}", Status = JobStatus.Completed, Progress = 100 });
            }
        }
        
        ReportProgress(progress, 70, jobs); // Research phase complete (70%)
        
        var result = await base.ExecuteAndParse(messages, options, progress, cancellationToken);

        // Report completion
        jobs[1].Status = JobStatus.Completed;
        jobs[1].Progress = 100;
        ReportProgress(progress, 100, jobs);

        return result;
    }
}

internal class ResearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}