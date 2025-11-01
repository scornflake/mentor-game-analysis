using System.Text.Json;
using System.Text.RegularExpressions;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Service for fetching and extracting content from Warframe wiki pages
/// </summary>
public class WikiContentExtractorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikiContentExtractorService> _logger;
    private const string MediaWikiApiBase = "https://wiki.warframe.com/api.php";

    public WikiContentExtractorService(HttpClient httpClient, ILogger<WikiContentExtractorService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        var htmlContent = await FetchWikiPageAsync(pageName, cancellationToken);
        var characteristics = ExtractCharacteristics(htmlContent);

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
    /// Fetch wiki page HTML content from MediaWiki API
    /// </summary>
    public async Task<string> FetchWikiPageAsync(string pageName, CancellationToken cancellationToken = default)
    {
        // Use MediaWiki API to get parsed HTML
        var url = $"{MediaWikiApiBase}?action=parse&format=json&page={Uri.EscapeDataString(pageName)}";
        
        _logger.LogDebug("Fetching wiki page from: {Url}", url);
        
        // Set User-Agent header - MediaWiki requires this to identify the application
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "MentorApp/1.0 (https://github.com/yourusername/mentor; contact@example.com)");
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<MediaWikiApiResponse>(json);

        if (apiResponse?.Parse?.Text?.Content == null)
        {
            throw new InvalidOperationException($"Failed to fetch wiki page: {pageName}");
        }

        return apiResponse.Parse.Text.Content;
    }

    /// <summary>
    /// Extract characteristics from HTML content
    /// </summary>
    public List<WikiCharacteristic> ExtractCharacteristics(string htmlContent)
    {
        var characteristics = new List<WikiCharacteristic>();
        
        // Find the Characteristics section
        // Look for <h2> or <h3> tag with "Characteristics" text
        var characteristicsMatch = Regex.Match(
            htmlContent,
            @"<h[23][^>]*>\s*<span[^>]*>\s*Characteristics\s*</span>.*?</h[23]>(.*?)(?=<h[23]|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase
        );

        if (!characteristicsMatch.Success)
        {
            _logger.LogWarning("Could not find Characteristics section in HTML");
            return characteristics;
        }

        var characteristicsSection = characteristicsMatch.Groups[1].Value;

        // Extract all list items from the section
        var listItemMatches = Regex.Matches(characteristicsSection, @"<li[^>]*>(.*?)</li>", RegexOptions.Singleline);

        int index = 0;
        foreach (Match match in listItemMatches)
        {
            var itemHtml = match.Groups[1].Value;
            
            // Clean up HTML tags and decode entities
            var cleanedText = CleanHtmlText(itemHtml);
            
            // Skip empty or very short characteristics
            if (string.IsNullOrWhiteSpace(cleanedText) || cleanedText.Length < 10)
            {
                continue;
            }

            characteristics.Add(new WikiCharacteristic
            {
                Text = cleanedText,
                OriginalIndex = index++
            });
        }

        return characteristics;
    }

    /// <summary>
    /// Clean HTML text by removing tags and decoding entities
    /// </summary>
    private string CleanHtmlText(string html)
    {
        // Remove HTML tags
        var text = Regex.Replace(html, @"<[^>]+>", " ");
        
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

