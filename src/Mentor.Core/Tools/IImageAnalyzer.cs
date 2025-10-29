using Mentor.Core.Interfaces;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IImageAnalyzer
{
    /// <summary>
    /// Analyzes an image and provides a detailed description along with
    /// a probability score indicating if the image is related to the specified game
    /// </summary>
    /// <param name="imageData">The image data with MIME type information</param>
    /// <param name="gameName">The name of the game to check relevance against</param>
    /// <param name="provider">The LLM provider to use for analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An ImageAnalysisResult containing the description and game relevance probability</returns>
    Task<ImageAnalysisResult> AnalyzeImageAsync(
        RawImage imageData,
        string gameName,
        ILLMClient provider,
        CancellationToken cancellationToken = default);
}

