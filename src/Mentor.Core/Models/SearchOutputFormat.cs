namespace Mentor.Core.Models;

/// <summary>
/// Defines the output format for web search results
/// </summary>
public enum SearchOutputFormat
{
    /// <summary>
    /// Plain text concatenated snippets from search results
    /// </summary>
    Snippets,
    
    /// <summary>
    /// Structured text with titles, URLs, and snippets for each result
    /// </summary>
    Structured,
    
    /// <summary>
    /// Condensed summary text of search results
    /// </summary>
    Summary
}

