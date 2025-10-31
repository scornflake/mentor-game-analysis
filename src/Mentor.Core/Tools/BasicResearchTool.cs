using Mentor.Core.Data;
using Mentor.Core.Models;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class BasicResearchTool : IResearchTool
{
    private readonly IWebSearchTool _webSearchTool;
    private readonly IArticleReader _articleReader;
    private readonly IHtmlToMarkdownConverter _htmlToMarkdownConverter;
    private readonly ILogger<BasicResearchTool> _logger;
    private ToolConfigurationEntity _config = new ToolConfigurationEntity();

    public BasicResearchTool(
        IWebSearchTool webSearchTool,
        IArticleReader articleReader,
        IHtmlToMarkdownConverter htmlToMarkdownConverter,
        ILogger<BasicResearchTool> logger)
    {
        _webSearchTool = webSearchTool ?? throw new ArgumentNullException(nameof(webSearchTool));
        _articleReader = articleReader ?? throw new ArgumentNullException(nameof(articleReader));
        _htmlToMarkdownConverter = htmlToMarkdownConverter ?? throw new ArgumentNullException(nameof(htmlToMarkdownConverter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Configure(ToolConfigurationEntity configuration)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<List<ResearchResult>> PerformResearchAsync(
        AnalysisRequest request,
        ResearchMode mode = ResearchMode.SummaryOnly,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var researchResults = new List<ResearchResult>();

        // Create initial progress with web search job
        var analysisProgress = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = AnalysisJob.JobTag.WebSearch, Name = "Searching web", Status = JobStatus.InProgress, Progress = 0 }
            }
        };
        analysisProgress.ReportProgress(progress);

        // Search based on the prompt
        var searchPrompt = "Game: " + request.GameName + ", articles about: " + request.Prompt;
        searchPrompt += ". Do not include from reddit or social media";
        
        var searchContext = SearchContext.Create(searchPrompt, request.GameName);
        var searchResults = await _webSearchTool.Search(searchContext, 8);

        // Update search job to completed
        analysisProgress.UpdateJobProgress("search-web", JobStatus.Completed, 100);
        analysisProgress.ReportProgress(progress);

        if (mode == ResearchMode.SummaryOnly)
        {
            // Summary-only mode: use search result summaries directly
            return await ProcessSummaryOnlyMode(searchResults, analysisProgress, progress);
        }
        else
        {
            // Full article mode: read and convert full articles
            return await ProcessFullArticleMode(searchResults, analysisProgress, progress, cancellationToken);
        }
    }

    private Task<List<ResearchResult>> ProcessSummaryOnlyMode(
        List<SearchResult> searchResults,
        AnalysisProgress analysisProgress,
        IProgress<AnalysisProgress>? progress)
    {
        var researchResults = new List<ResearchResult>();

        // Add summary processing jobs
        for (int i = 0; i < searchResults.Count; i++)
        {
            analysisProgress.Jobs.Add(new AnalysisJob
            {
                Tag = AnalysisJob.JobTag.GetArticleTag(i),
                Name = $"Processing summary {i + 1}: {searchResults[i].Title}",
                Status = JobStatus.Pending,
                Progress = 0
            });
        }

        analysisProgress.ReportProgress(progress);

        // Process summaries
        int summaryIndex = 0;
        foreach (var searchResult in searchResults)
        {
            var articleTag = AnalysisJob.JobTag.GetArticleTag(summaryIndex);

            // Update processing job
            analysisProgress.UpdateJobProgress(articleTag, JobStatus.InProgress, 0);
            analysisProgress.SetName(articleTag, $"Processing summary {summaryIndex + 1}: {searchResult.Title}");
            analysisProgress.ReportProgress(progress);

            // Check if content is not empty
            if (string.IsNullOrWhiteSpace(searchResult.Content))
            {
                // Mark as failed if no content
                analysisProgress.UpdateJobProgress(articleTag, JobStatus.Failed, 100);
                analysisProgress.SetName(articleTag, $"No content for summary {summaryIndex + 1}: {searchResult.Title}");
                analysisProgress.ReportProgress(progress);
                summaryIndex++;
                continue;
            }

            // Use summary content directly
            researchResults.Add(new ResearchResult
            {
                Title = searchResult.Title,
                Url = searchResult.Url,
                Content = searchResult.Content
            });

            analysisProgress.UpdateJobProgress(articleTag, JobStatus.Completed, 100);
            analysisProgress.SetName(articleTag, $"Processed summary {summaryIndex + 1}: {searchResult.Title}");
            analysisProgress.ReportProgress(progress);
            summaryIndex++;
        }

        return Task.FromResult(researchResults);
    }

    private async Task<List<ResearchResult>> ProcessFullArticleMode(
        List<SearchResult> searchResults,
        AnalysisProgress analysisProgress,
        IProgress<AnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var researchResults = new List<ResearchResult>();

        // Add article processing jobs (reading + converting combined)
        for (int i = 0; i < searchResults.Count; i++)
        {
            analysisProgress.Jobs.Add(new AnalysisJob
            {
                Tag = AnalysisJob.JobTag.GetArticleTag(i),
                Name = $"Processing article {i + 1}: {searchResults[i].Title}",
                Status = JobStatus.Pending,
                Progress = 0
            });
        }

        analysisProgress.ReportProgress(progress);

        // Process articles (read + convert combined)
        int articleIndex = 0;
        foreach (var searchResult in searchResults)
        {
            var articleTag = AnalysisJob.JobTag.GetArticleTag(articleIndex);

            // Update processing job
            analysisProgress.UpdateJobProgress(articleTag, JobStatus.InProgress, 0);
            analysisProgress.SetName(articleTag, $"Reading article {articleIndex + 1}: {searchResult.Title}");
            analysisProgress.ReportProgress(progress);

            try
            {
                // Read article (first half of the work)
                var articleContent = await _articleReader.ReadArticleAsync(searchResult.Url, cancellationToken);

                analysisProgress.UpdateJobProgress(articleTag, JobStatus.InProgress, 50);
                analysisProgress.SetName(articleTag, $"Converting article {articleIndex + 1}: {searchResult.Title}");
                analysisProgress.ReportProgress(progress);

                // Convert to markdown (second half of the work)
                var markdown = await _htmlToMarkdownConverter.ConvertAsync(articleContent, cancellationToken);

                researchResults.Add(new ResearchResult
                {
                    Title = searchResult.Title,
                    Url = searchResult.Url,
                    Content = markdown
                });

                analysisProgress.UpdateJobProgress(articleTag, JobStatus.Completed, 100);
                analysisProgress.SetName(articleTag, $"Converted article {articleIndex + 1}: {searchResult.Title}");

            }
            catch (Exception)
            {
                // Update processing job
                analysisProgress.UpdateJobProgress(articleTag, JobStatus.Failed, 100);
                analysisProgress.SetName(articleTag, $"Error with article {articleIndex + 1}: {searchResult.Title}");
            }

            analysisProgress.ReportProgress(progress);
            articleIndex++;
        }

        return researchResults;
    }
}

