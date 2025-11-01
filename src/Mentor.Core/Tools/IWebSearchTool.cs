using Mentor.Core.Data;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IWebSearchTool
{
    Task<List<SearchResult>> Search(SearchContext context, int maxResults = 8);
    Task<IList<SearchResult>> SearchStructured(SearchContext context, int maxResults = 8);

    void Configure(ToolConfigurationEntity configuration);
}

public class KnownSearchTools
{
    public const string Brave = "brave";
    public const string Tavily = "tavily";
}

public class KnownProviderTools
{
    public const string Perplexity = "perplexity";
    public const string Claude = "claude";
}

public class KnownTools
{
    public const string ArticleReader = "article-reader";
    public const string TextSummarizer = "text-summarizer";
    public const string BasicResearch = "basic-research";
}

public record ResultStructure (string Title, string Url, string Snippet);