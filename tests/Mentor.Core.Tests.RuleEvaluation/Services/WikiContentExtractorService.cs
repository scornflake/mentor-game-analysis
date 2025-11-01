using System.Text.Json;
using System.Text.RegularExpressions;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Microsoft.Extensions.Logging;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Pages;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for fetching and extracting content from Warframe wiki pages
/// </summary>
public class WikiContentExtractorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikiContentExtractorService> _logger;
    private const string WikiApiEndpoint = "https://wiki.warframe.com/api.php";
    private WikiSite? _wikiSite;

    public WikiContentExtractorService(HttpClient httpClient, ILogger<WikiContentExtractorService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get or create the WikiSite instance
    /// </summary>
    private async Task<WikiSite> GetWikiSiteAsync()
    {
        if (_wikiSite == null)
        {
            var wikiClient = new WikiClient
            {
                ClientUserAgent = "MentorApp/1.0 (https://github.com/yourusername/mentor; contact@example.com)"
            };
            _wikiSite = new WikiSite(wikiClient, WikiApiEndpoint);
            await _wikiSite.Initialization;
        }
        return _wikiSite;
    }

    /// <summary>
    /// Extract characteristics directly from a wiki URL
    /// </summary>
    public async Task<List<WikiCharacteristic>> ExtractCharacteristicsFromUrlAsync(
        string wikiUrl,
        CancellationToken cancellationToken = default)
    {
        var pageName = ExtractPageNameFromUrl(wikiUrl);
        _logger.LogInformation("Extracting characteristics from wiki page: {PageName}", pageName);

        var wikitext = await FetchWikiPageAsync(pageName, cancellationToken);
        var characteristics = ExtractCharacteristics(wikitext);

        _logger.LogInformation("Extracted {Count} characteristics from {PageName}", characteristics.Count, pageName);
        return characteristics;
    }

    /// <summary>
    /// Extract page name from wiki URL
    /// </summary>
    public string ExtractPageNameFromUrl(string wikiUrl)
    {
        // Extract page name from URLs like:
        // https://wiki.warframe.com/w/Cedo_Prime
        // https://wiki.warframe.com/wiki/Cedo_Prime
        var match = Regex.Match(wikiUrl, @"/(?:w|wiki)/(.+?)(?:\?|#|$)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new ArgumentException($"Could not extract page name from URL: {wikiUrl}");
    }

    /// <summary>
    /// Fetch wiki page wikitext content using WikiClientLibrary
    /// </summary>
    public async Task<string> FetchWikiPageAsync(string pageName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching wiki page: {PageName}", pageName);
        
        var wikiSite = await GetWikiSiteAsync();
        var page = new WikiPage(wikiSite, pageName);
        
        await page.RefreshAsync(PageQueryOptions.FetchContent, cancellationToken);

        if (page.Content == null)
        {
            throw new InvalidOperationException($"Failed to fetch wiki page: {pageName}");
        }

        _logger.LogDebug("Fetched wikitext for {PageName} ({Length} chars)", pageName, page.Content.Length);
        return page.Content;
    }

    /// <summary>
    /// Extract characteristics from wikitext content
    /// </summary>
    public List<WikiCharacteristic> ExtractCharacteristics(string wikitext)
    {
        // Find the Characteristics section in wikitext
        // Look for == Characteristics == header
        // Stop at next section (==), template ({{), or end of string
        var characteristicsMatch = Regex.Match(
            wikitext,
            @"==\s*Characteristics\s*==(.*?)(?=^==\s*\w|^\{\{[A-Z]|\z)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        if (!characteristicsMatch.Success)
        {
            _logger.LogWarning("Could not find Characteristics section in wikitext");
            return new List<WikiCharacteristic>();
        }

        var characteristicsSection = characteristicsMatch.Groups[1].Value;

        // Extract all list items with their indentation levels
        // Match lines that start with one or more asterisks
        var listItemMatches = Regex.Matches(characteristicsSection, @"(?:^|\n)(\*+)\s*(.+?)(?=\n|$)", RegexOptions.Multiline);

        var flatList = new List<(int level, string text, int index)>();
        int globalIndex = 0;

        foreach (Match match in listItemMatches)
        {
            var asterisks = match.Groups[1].Value;
            var indentLevel = asterisks.Length;
            var itemText = match.Groups[2].Value;
            
            // Clean up wikitext markup
            var cleanedText = CleanWikitextMarkup(itemText);
            
            // Skip only truly empty characteristics
            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                continue;
            }

            flatList.Add((indentLevel, cleanedText, globalIndex++));
        }

        // Build hierarchical tree from flat list
        return BuildHierarchy(flatList);
    }

    /// <summary>
    /// Build hierarchical tree from flat list of characteristics with indent levels
    /// </summary>
    private List<WikiCharacteristic> BuildHierarchy(List<(int level, string text, int index)> flatList)
    {
        var rootCharacteristics = new List<WikiCharacteristic>();
        var stack = new Stack<(WikiCharacteristic characteristic, int level)>();

        foreach (var (level, text, index) in flatList)
        {
            var characteristic = new WikiCharacteristic
            {
                Text = text,
                OriginalIndex = index,
                IndentLevel = level
            };

            // Pop stack until we find the parent level
            while (stack.Count > 0 && stack.Peek().level >= level)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                // This is a root-level item
                rootCharacteristics.Add(characteristic);
            }
            else
            {
                // This is a child of the item on top of the stack
                stack.Peek().characteristic.Children.Add(characteristic);
            }

            // Push current item onto stack
            stack.Push((characteristic, level));
        }

        return rootCharacteristics;
    }

    /// <summary>
    /// Clean wikitext markup to plain text
    /// </summary>
    private string CleanWikitextMarkup(string wikitext)
    {
        var text = wikitext;
        
        // Handle templates {{template|param1|param2|...}}
        // Extract display text: if template has multiple params, use the last one as display text
        // Otherwise use the first param
        text = Regex.Replace(text, @"\{\{[^}|]+\|([^}]+)\}\}", match =>
        {
            var content = match.Groups[1].Value;
            // Split by pipe and take the last part (display text)
            var parts = content.Split('|');
            return parts[parts.Length - 1];
        });
        
        // Remove any remaining templates (those without pipes)
        text = Regex.Replace(text, @"\{\{[^}]*\}\}", " ");
        
        // Convert wiki links [[Page|Display]] to Display, or [[Page]] to Page
        text = Regex.Replace(text, @"\[\[(?:[^|\]]+\|)?([^\]]+)\]\]", "$1");
        
        // Remove bold/italic markup
        text = text.Replace("'''", "");  // Bold
        text = text.Replace("''", "");   // Italic
        
        // Remove external links [http://url text] to text
        text = Regex.Replace(text, @"\[https?://[^\s\]]+ ([^\]]+)\]", "$1");
        
        // Remove remaining external links [http://url]
        text = Regex.Replace(text, @"\[https?://[^\]]+\]", "");
        
        // Remove HTML tags (some wikitext may contain HTML)
        text = Regex.Replace(text, @"<[^>]+>", " ");
        
        // Remove references <ref>...</ref>
        text = Regex.Replace(text, @"<ref[^>]*>.*?</ref>", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<ref[^>]*/>", "");
        
        // Decode common HTML entities
        text = text
            .Replace("&nbsp;", " ")
            .Replace("&quot;", "\"")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&#39;", "'")
            .Replace("&times;", "×");
        
        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ");
        
        return text.Trim();
    }
}

