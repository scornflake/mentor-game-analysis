using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mentor.Core.Models;

/// <summary>
/// Response structure from the LLM containing the converted markdown.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
[JsonSerializable(typeof(MarkdownResponse))]
internal record MarkdownResponse
{
    /// <summary>
    /// The HTML content converted to Markdown format.
    /// </summary>
    public string Markdown { get; init; } = string.Empty;
}

