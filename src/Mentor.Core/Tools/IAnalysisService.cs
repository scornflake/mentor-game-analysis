using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IAnalysisService
{
    /// <summary>
    /// Main entry point for screenshot analysis
    /// </summary>
    Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default
    );
}

