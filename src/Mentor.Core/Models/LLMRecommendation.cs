using System.Diagnostics.CodeAnalysis;

namespace Mentor.Core.Models;


/// <summary>
/// Individual recommendation item with priority, action, reasoning, and context.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public record LLMRecommendation
{
    /// <summary>
    /// Priority level for this recommendation. Must be exactly one of: 'high', 'medium', or 'low'.
    /// </summary>
    public string Priority { get; init; } = string.Empty;

    /// <summary>
    /// Specific actionable step or item that the user should take. Should be clear and concrete.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Explanation of why this recommendation is relevant and important. Should justify the priority level.
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Relevant context from the screenshot or analysis that supports this recommendation. Include specific details observed.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// URL from web search results that supports this recommendation, if applicable. Use empty string if no reference link is available.
    /// </summary>
    public string ReferenceLink { get; init; } = string.Empty;
}
