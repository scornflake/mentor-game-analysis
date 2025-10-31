using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/// <summary>
/// Extracts plain text from HTML content using HTML Agility Pack.
/// Removes scripts, styles, navigation, and other non-content elements.
/// </summary>
public class HtmlTextExtractor : IHtmlTextExtractor
{
    private readonly ILogger<HtmlTextExtractor> _logger;

    // Elements to remove completely
    private static readonly string[] ExcludedElements = 
    {
        "script", "style", "nav", "header", "footer", "aside",
        "iframe", "noscript", "svg", "button", "form"
    };

    // Class/ID patterns that indicate non-content elements
    private static readonly string[] ExcludedPatterns = 
    {
        "ad", "advertisement", "banner", "popup", "modal",
        "comment", "social", "share", "menu", "navigation",
        "sidebar", "widget", "promo", "sponsored"
    };

    public HtmlTextExtractor(ILogger<HtmlTextExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> ExtractTextAsync(string htmlContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("HTML content cannot be null or empty", nameof(htmlContent));
        }

        try
        {
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            // Remove excluded elements
            RemoveExcludedElements(document);

            // Remove elements with excluded class/id patterns
            RemoveElementsByPattern(document);

            // Extract text from remaining nodes
            var text = ExtractText(document.DocumentNode);

            // Clean up whitespace
            var cleanedText = CleanWhitespace(text);

            _logger.LogDebug("Extracted {Length} characters of text from HTML", cleanedText.Length);

            return Task.FromResult(cleanedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from HTML");
            throw;
        }
    }

    private void RemoveExcludedElements(HtmlDocument document)
    {
        foreach (var elementName in ExcludedElements)
        {
            var nodes = document.DocumentNode.SelectNodes($"//{elementName}");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }
        }
    }

    private void RemoveElementsByPattern(HtmlDocument document)
    {
        // Remove elements with excluded classes or IDs
        var allNodes = document.DocumentNode.SelectNodes("//*[@class or @id]");
        if (allNodes != null)
        {
            foreach (var node in allNodes.ToList())
            {
                var classAttr = node.GetAttributeValue("class", string.Empty).ToLowerInvariant();
                var idAttr = node.GetAttributeValue("id", string.Empty).ToLowerInvariant();

                foreach (var pattern in ExcludedPatterns)
                {
                    if (classAttr.Contains(pattern) || idAttr.Contains(pattern))
                    {
                        node.Remove();
                        break;
                    }
                }
            }
        }

        // Remove any elements with 'screen-reader' or 'sr-only' in class (accessibility content meant only for screen readers)
        var screenReaderNodes = document.DocumentNode.SelectNodes("//*[contains(@class, 'screen-reader') or contains(@class, 'sr-only') or contains(@class, 'visually-hidden')]");
        if (screenReaderNodes != null)
        {
            foreach (var node in screenReaderNodes.ToList())
            {
                node.Remove();
            }
        }

        // Remove custom elements that are typically UI components (web components with hyphens)
        var customElements = document.DocumentNode.SelectNodes("//*[contains(name(), '-')]");
        if (customElements != null)
        {
            foreach (var node in customElements.ToList())
            {
                // Only remove if it's a known UI component pattern
                var nodeName = node.Name.ToLowerInvariant();
                if (nodeName.Contains("tracker") || nodeName.Contains("loader") || 
                    nodeName.Contains("button") || nodeName.Contains("dropdown") || 
                    nodeName.Contains("menu") || nodeName.Contains("hovercard"))
                {
                    node.Remove();
                }
            }
        }
    }

    private string ExtractText(HtmlNode node)
    {
        var sb = new StringBuilder();

        foreach (var childNode in node.ChildNodes)
        {
            if (childNode.NodeType == HtmlNodeType.Text)
            {
                var text = HtmlEntity.DeEntitize(childNode.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.Append(text);
                    sb.Append(' ');
                }
            }
            else if (childNode.NodeType == HtmlNodeType.Element)
            {
                // Recursively extract text from child elements
                sb.Append(ExtractText(childNode));
            }
        }

        return sb.ToString();
    }

    private string CleanWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Replace multiple spaces with single space
        text = Regex.Replace(text, @"[ \t]+", " ");

        // Replace multiple newlines with double newline (preserve paragraph breaks)
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        // Trim whitespace from each line
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line));

        return string.Join('\n', lines).Trim();
    }
}

