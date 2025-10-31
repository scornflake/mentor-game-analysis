using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mentor.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public class SearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
        
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
        
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
