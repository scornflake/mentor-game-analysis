using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mentor.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public class BraveWebResults
{
    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();
}
