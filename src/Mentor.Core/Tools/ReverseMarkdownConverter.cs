using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using ReverseMarkdown;

namespace Mentor.Core.Tools;

public class ReverseMarkdownConverter : IHtmlToMarkdownConverter
{
    private readonly ILogger<ReverseMarkdownConverter> _logger;

    public ReverseMarkdownConverter(ILogger<ReverseMarkdownConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ConvertAsync(string htmlContent, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning("Received null or empty HTML content for conversion");
            return string.Empty;
        }

        try
        {
            var config = new ReverseMarkdown.Config
            {
                UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.PassThrough,
                GithubFlavored = true,
                RemoveComments = true,
                SmartHrefHandling = true
            };

            var converter = new ReverseMarkdown.Converter(config);
            var markdown = converter.Convert(htmlContent);
            
            // Clean up excessive whitespace and normalize line breaks
            markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
            markdown = Regex.Replace(markdown, @"[ \t]+\n", "\n");
            
            _logger.LogDebug("Successfully converted HTML to Markdown, output length: {Length}", markdown.Length);
            
            return markdown.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error converting HTML to Markdown, falling back to plain text extraction");
            // Fallback: Basic text extraction
            return ExtractPlainText(htmlContent);
        }
    }

    private string ExtractPlainText(string htmlContent)
    {
        try
        {
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = context.OpenAsync(req => req.Content(htmlContent)).GetAwaiter().GetResult();
            
            // Remove script and style elements
            document.QuerySelectorAll("script, style").ToList().ForEach(el => el.Remove());
            
            var plainText = document.Body?.TextContent ?? string.Empty;
            _logger.LogDebug("Extracted plain text as fallback, length: {Length}", plainText.Length);
            
            return plainText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract plain text from HTML");
            return string.Empty;
        }
    }
}

