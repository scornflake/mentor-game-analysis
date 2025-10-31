using System.Diagnostics.CodeAnalysis;

namespace Mentor.Core.Models;

/// <summary>
/// Response structure from the LLM containing analysis and recommendations.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public record LLMResponse
{
    /// <summary>
    /// Detailed analysis of the screenshot/content provided by the user. Should be comprehensive and based on visual observation and any web search results.
    /// </summary>
    public string Analysis { get; init; } = string.Empty;

    /// <summary>
    /// Brief summary of key findings and insights. Should be concise but informative.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// List of actionable recommendations based on the analysis. Each recommendation should be specific and practical.
    /// </summary>
    public List<LLMRecommendation> Recommendations { get; init; } = [];

    /// <summary>
    /// Confidence score indicating how certain the analysis is, ranging from 0.0 (low confidence) to 1.0 (high confidence).
    /// </summary>
    public double Confidence { get; init; }
}