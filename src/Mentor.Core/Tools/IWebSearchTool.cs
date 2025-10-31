using Mentor.Core.Data;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IWebSearchTool
{
    Task<string> Search(SearchContext context, SearchOutputFormat format, int maxResults = 5);
    Task<IList<SearchResult>> SearchStructured(SearchContext context, int maxResults = 5);

    void Configure(ToolConfigurationEntity configuration);
    
    IReadOnlySet<SearchOutputFormat> SupportedModes { get; }
}

public class KnownSearchTools
{
    public const string Brave = "brave";
    public const string Tavily = "tavily";
}

public class KnownProviderTools
{
    public const string Perplexity = "perplexity";
}

public class KnownTools
{
    public const string ArticleReader = "article-reader";
    public const string TextSummarizer = "text-summarizer";
}

public record ResultStructure (string Title, string Url, string Snippet);