using System.Text.Json.Serialization;

namespace Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// Response from MediaWiki API parse endpoint
/// </summary>
public class MediaWikiApiResponse
{
    [JsonPropertyName("parse")]
    public MediaWikiParse? Parse { get; set; }
}

public class MediaWikiParse
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("pageid")]
    public int PageId { get; set; }
    
    [JsonPropertyName("text")]
    public MediaWikiText? Text { get; set; }
}

public class MediaWikiText
{
    [JsonPropertyName("*")]
    public string? Content { get; set; }
}

/// <summary>
/// Individual characteristic extracted from wiki before categorization
/// </summary>
public class WikiCharacteristic
{
    public string Text { get; set; } = string.Empty;
    public int OriginalIndex { get; set; }
    public int IndentLevel { get; set; }
    public List<WikiCharacteristic> Children { get; set; } = new();
}

/// <summary>
/// LLM response for batch categorization of characteristics
/// </summary>
public class RuleCategorization
{
    [JsonPropertyName("categorizations")]
    public List<CategoryAssignment> Categorizations { get; set; } = new();
}

public class CategoryAssignment
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}

/// <summary>
/// Complete game rule ready for JSON output
/// </summary>
public class ParsedGameRule
{
    [JsonPropertyName("RuleId")]
    public string RuleId { get; set; } = string.Empty;
    
    [JsonPropertyName("RuleText")]
    public string RuleText { get; set; } = string.Empty;
    
    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("Children")]
    public List<ParsedGameRule> Children { get; set; } = new();
}

