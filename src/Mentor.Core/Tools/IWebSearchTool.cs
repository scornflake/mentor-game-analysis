using Mentor.Core.Data;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public class SearchResult
{
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
        
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
        
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public interface IWebSearchTool
{
    Task<string> Search(SearchContext context, SearchOutputFormat format, int maxResults = 5);
    Task<IList<SearchResult>> SearchStructured(SearchContext context, int maxResults = 5);

    void Configure(ToolConfigurationEntity configuration);
}

public class KnownSearchTools
{
    public const string Brave = "brave";
}

public class KnownTools
{
    public const string ArticleReader = "article-reader";
}

public record ResultStructure (string Title, string Url, string Snippet);