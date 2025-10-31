namespace Mentor.Core.Tools;

/// <summary>
/// Extracts plain text from HTML content, removing scripts, styles, navigation, and other non-content elements.
/// </summary>
public interface IHtmlTextExtractor
{
    /// <summary>
    /// Extracts plain text from HTML content.
    /// </summary>
    /// <param name="htmlContent">The HTML content to extract text from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted plain text.</returns>
    Task<string> ExtractTextAsync(string htmlContent, CancellationToken cancellationToken = default);
}

