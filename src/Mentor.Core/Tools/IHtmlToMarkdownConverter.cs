namespace Mentor.Core.Tools;

public interface IHtmlToMarkdownConverter
{
    /// <summary>
    /// Converts HTML content to Markdown format.
    /// </summary>
    /// <param name="htmlContent">The HTML content to convert.</param>
    /// <returns>The converted Markdown text.</returns>
    Task<string> ConvertAsync(string htmlContent, CancellationToken cancellationToken = default);
}

