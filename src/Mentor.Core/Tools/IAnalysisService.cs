using Mentor.Core.Models;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Tools;

public interface IAnalysisService
{
    /// <summary>
    /// Main entry point for screenshot analysis
    /// </summary>
    Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        IProgress<AnalysisProgress>? progress = null,
        IProgress<AIContent>? aiProgress = null,
        CancellationToken cancellationToken = default
    );
}

