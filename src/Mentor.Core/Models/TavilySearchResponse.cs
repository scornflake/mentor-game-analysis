using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mentor.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public class TavilySearchRequest
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("max_results")]
    public int MaxResults { get; set; } = 5;
    
    [JsonPropertyName("search_depth")]
    public string SearchDepth { get; set; } = "basic";
    
    [JsonPropertyName("include_answer")]
    public bool IncludeAnswer { get; set; }
    
    [JsonPropertyName("include_images")]
    public bool IncludeImages { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public class TavilySearchResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }
    
    [JsonPropertyName("results")]
    public List<TavilyResult> Results { get; set; } = new();
    
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }
    
    [JsonPropertyName("response_time")]
    public double ResponseTime { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
public class TavilyResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("score")]
    public double Score { get; set; }
    
    [JsonPropertyName("raw_content")]
    public string? RawContent { get; set; }
}

