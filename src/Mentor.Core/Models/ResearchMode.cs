namespace Mentor.Core.Models;

/// <summary>
/// Defines the mode of research to perform
/// </summary>
public enum ResearchMode
{
    /// <summary>
    /// Reads full articles from URLs and converts them to markdown
    /// </summary>
    FullArticle,
    
    /// <summary>
    /// Uses only search result summaries without reading full articles
    /// </summary>
    SummaryOnly
}

