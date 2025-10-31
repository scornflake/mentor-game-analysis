using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mentor.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public class BraveSearchResponse
{
    [JsonPropertyName("web")]
    public BraveWebResults? Web { get; set; }
}