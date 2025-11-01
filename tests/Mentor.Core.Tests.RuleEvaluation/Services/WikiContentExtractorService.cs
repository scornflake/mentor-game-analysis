using System.Text.Json;
using System.Text.RegularExpressions;
using Mentor.Core.Interfaces;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Microsoft.Extensions.AI;
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
    public async Task<List<WikiContent>> ExtractCharacteristicsFromUrlAsync(
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
    public List<WikiContent> ExtractCharacteristics(string wikitext)
    {
        // Find the Characteristics section and related templates
        // Look for == Characteristics == header and continue until the next major section
        var characteristicsMatch = Regex.Match(
            wikitext,
            @"==\s*Characteristics\s*==(.*?)(?=^==\s*(?!Characteristics)\w|\z)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        if (!characteristicsMatch.Success)
        {
            _logger.LogWarning("Could not find Characteristics section in wikitext");
            return new List<WikiContent>();
        }

        var characteristicsSection = characteristicsMatch.Groups[1].Value;

        var flatList = new List<(int level, string text, int index)>();
        int globalIndex = 0;

        // First, extract regular bullet points before any templates
        var beforeTemplates = Regex.Match(characteristicsSection, @"^(.*?)(?=\{\{(?:Advantages|Disadvantages)|\z)", RegexOptions.Singleline);
        if (beforeTemplates.Success)
        {
            var listItemMatches = Regex.Matches(beforeTemplates.Groups[1].Value, @"(?:^|\n)(\*+)\s*(.+?)(?=\n|$)", RegexOptions.Multiline);
            foreach (Match match in listItemMatches)
            {
                var asterisks = match.Groups[1].Value;
                var indentLevel = asterisks.Length;
                var itemText = match.Groups[2].Value;
                
                var cleanedText = CleanWikitextMarkup(itemText);
                if (string.IsNullOrWhiteSpace(cleanedText))
                {
                    continue;
                }

                flatList.Add((indentLevel, cleanedText, globalIndex++));
            }
        }

        // Now extract Advantages and Disadvantages templates
        // We need to handle nested templates, so we can't use a simple [^}]* pattern
        // Instead, match everything until we find the closing }} that's NOT part of a nested template
        var templateMatches = Regex.Matches(characteristicsSection, 
            @"\{\{(Advantages|Disadvantages)\s*\|(.*?)\n\}\}",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match templateMatch in templateMatches)
        {
            var templateName = templateMatch.Groups[1].Value;
            var templateContent = templateMatch.Groups[2].Value;
            
            // Extract bullet points within the template first to see if there's any content
            var templateListMatches = Regex.Matches(templateContent, @"(?:^|\n)(\*+)\s*(.+?)(?=\n|$)", RegexOptions.Multiline);
            
            // Skip this template if it has no bullet points
            if (templateListMatches.Count == 0)
            {
                continue;
            }

            // Create parent item for the template section
            var sectionTitle = $"{templateName} over other Primary weapons (excluding modular weapons):";
            var sectionIndex = globalIndex++;
            flatList.Add((1, sectionTitle, sectionIndex));

            // Add the bullet points
            foreach (Match match in templateListMatches)
            {
                var asterisks = match.Groups[1].Value;
                var indentLevel = asterisks.Length + 1; // Add 1 to make them children of the section title
                var itemText = match.Groups[2].Value;
                
                var cleanedText = CleanWikitextMarkup(itemText);
                if (string.IsNullOrWhiteSpace(cleanedText))
                {
                    continue;
                }

                flatList.Add((indentLevel, cleanedText, globalIndex++));
            }
        }

        // Build hierarchical tree from flat list
        return BuildHierarchy(flatList);
    }

    /// <summary>
    /// Build hierarchical tree from flat list of characteristics with indent levels
    /// </summary>
    private List<WikiContent> BuildHierarchy(List<(int level, string text, int index)> flatList)
    {
        var rootCharacteristics = new List<WikiContent>();
        var stack = new Stack<(WikiContent characteristic, int level)>();

        foreach (var (level, text, index) in flatList)
        {
            var characteristic = new WikiContent
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
            var displayText = parts[parts.Length - 1];
            
            // Remove MediaWiki numbered parameter syntax (e.g. "1=Something" -> "Something")
            displayText = Regex.Replace(displayText, @"^\d+=", "");
            
            return displayText;
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

    /// <summary>
    /// Extract enemy information (strengths and weaknesses) from a wiki URL using LLM
    /// </summary>
    public async Task<List<WikiContent>> ExtractEnemyInfoFromUrlAsync(
        string wikiUrl,
        ILLMClient llmClient,
        CancellationToken cancellationToken = default)
    {
        var pageName = ExtractPageNameFromUrl(wikiUrl);
        _logger.LogInformation("Extracting enemy information from wiki page: {PageName}", pageName);

        var wikitext = await FetchWikiPageAsync(pageName, cancellationToken);
        
        _logger.LogDebug("Fetched {Length} characters of wikitext, sending to LLM for extraction", wikitext.Length);

        // Create system message with instructions
        var systemMessage = new ChatMessage(
            ChatRole.System,
            @"You are a Warframe game knowledge extraction assistant. Your task is to extract enemy strengths and weaknesses from wiki page content.

Analyze the provided wiki page and extract ONLY combat-related strengths and weaknesses of the enemy faction or unit.

Return a JSON response with the following structure:
{
  ""enemyInfo"": [
    {
      ""text"": ""Strengths"",
      ""originalIndex"": 0,
      ""indentLevel"": 1,
      ""children"": [
        {
          ""text"": ""<specific strength here>"",
          ""originalIndex"": 1,
          ""indentLevel"": 2,
          ""children"": []
        }
      ]
    },
    {
      ""text"": ""Weaknesses"",
      ""originalIndex"": 10,
      ""indentLevel"": 1,
      ""children"": [
        {
          ""text"": ""<specific weakness here>"",
          ""originalIndex"": 11,
          ""indentLevel"": 2,
          ""children"": []
        }
      ]
    }
  ]
}

Rules:
- Focus ONLY on combat-related strengths and weaknesses (damage vulnerabilities, resistances, tactical advantages/disadvantages)
- Create exactly TWO top-level items: one for ""Strengths"" and one for ""Weaknesses""
- Each strength or weakness should be a child item with indentLevel=2
- Ignore lore, history, equipment details, and other non-combat information
- Set originalIndex sequentially starting at 0 for first top-level item
- If no strengths or weaknesses are found in a category, include the parent item but with an empty children array");

        var userMessage = new ChatMessage(
            ChatRole.User,
            $"Extract enemy strengths and weaknesses from this wiki page:\n\n{wikitext}");

        var messages = new List<ChatMessage> { systemMessage, userMessage };

        try
        {
            var jsonOptions = RuleEvaluationJsonSerializerContext.CreateOptions();
            var chatOptions = new ChatOptions
            {
                Temperature = 0.1f,
                ResponseFormat = ChatResponseFormat.Json
            };

            var chatResponse = await llmClient.ChatClient.GetResponseAsync<EnemyInfoResponse>(
                messages,
                jsonOptions,
                chatOptions,
                true, // useNativeJsonSchema
                cancellationToken);

            var enemyInfo = chatResponse?.Result?.EnemyInfo ?? new List<WikiContent>();
            
            _logger.LogInformation("Extracted {Count} top-level enemy info items from {PageName}", 
                enemyInfo.Count, pageName);
            
            return enemyInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting enemy information from wiki page: {PageName}", pageName);
            _logger.LogError("The system prompt was: {SystemPrompt}", messages[0].Text);
            _logger.LogError("The prompt was: {Prompt}", userMessage.Text);
            throw;
        }
    }
}

