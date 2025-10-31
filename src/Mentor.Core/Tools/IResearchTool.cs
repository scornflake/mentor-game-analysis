using Mentor.Core.Data;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IResearchTool
{
    /// <summary>
    /// Performs research based on the analysis request and returns a list of research results.
    /// </summary>
    /// <param name="request">The analysis request containing the prompt and context</param>
    /// <param name="mode">The research mode to use (defaults to FullArticle)</param>
    /// <param name="progress">Progress reporting for tracking research operations</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A list of research results with titles, URLs, and content</returns>
    Task<List<ResearchResult>> PerformResearchAsync(
        AnalysisRequest request,
        ResearchMode mode = ResearchMode.SummaryOnly,
        IProgress<AnalysisProgress>? progress = null, 
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Configures the research tool with the provided configuration
    /// </summary>
    /// <param name="configuration">Tool configuration entity</param>
    void Configure(ToolConfigurationEntity configuration);
}

public static class KnownResearchTools
{
    public const string BasicResearch = "basic-research";
}

