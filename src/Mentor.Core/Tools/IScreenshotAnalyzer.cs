using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IScreenshotAnalyzer
{
    Task<string> AnalyzeScreenshotAsync(AnalysisRequest request);
}