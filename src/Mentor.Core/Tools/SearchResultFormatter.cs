using System.Text;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

/// <summary>
/// Service for formatting search results into different text representations
/// </summary>
public class SearchResultFormatter
{
    /// <summary>
    /// Formats search results as concatenated text content
    /// </summary>
    /// <param name="results">The search results to format</param>
    /// <returns>Concatenated content from all results</returns>
    public string FormatAsText(List<SearchResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.Content))
            {
                sb.AppendLine(result.Content);
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats search results as a summary with optional additional context
    /// </summary>
    /// <param name="results">The search results to format</param>
    /// <param name="additionalContext">Optional additional context (e.g., Tavily answer)</param>
    /// <returns>Formatted summary text</returns>
    public string FormatAsSummary(List<SearchResult> results, string? additionalContext = null)
    {
        if (results == null || results.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        
        // If additional context provided (e.g., Tavily answer), use it first
        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            sb.AppendLine(additionalContext);
            sb.AppendLine();
        }
        
        sb.AppendLine($"Found {results.Count} result(s):");
        sb.AppendLine();

        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.Content))
            {
                sb.AppendLine($"- {result.Content}");
            }
        }

        return sb.ToString().TrimEnd();
    }
}

