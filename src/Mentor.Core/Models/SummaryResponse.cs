using System.Diagnostics.CodeAnalysis;

namespace Mentor.Core.Models;

/// <summary>
/// Response structure from the LLM containing the summary.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public record SummaryResponse
{
    /// <summary>
    /// The summarized text.
    /// </summary>
    public string Summary { get; init; } = string.Empty;
}


