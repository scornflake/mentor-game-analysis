using System.Diagnostics.CodeAnalysis;

namespace Mentor.Core.Models;

/// <summary>
/// Internal record for deserializing structured LLM responses
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public record ImageAnalysisResponse
{
    /// <summary>
    /// Detailed description of the image content
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Probability (0.0 to 1.0) that the image is related to the specified game
    /// </summary>
    public double GameRelevanceProbability { get; init; }
}

