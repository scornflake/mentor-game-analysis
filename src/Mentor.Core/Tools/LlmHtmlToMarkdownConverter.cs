using System.Text.RegularExpressions;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class LlmHtmlToMarkdownConverter : IHtmlToMarkdownConverter
{
    private readonly ILLMClient _llmClient;
    private readonly ILogger<LlmHtmlToMarkdownConverter> _logger;

    public LlmHtmlToMarkdownConverter(
        ILLMClient llmClient,
        ILogger<LlmHtmlToMarkdownConverter> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ConvertAsync(string htmlContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("HTML content cannot be null or empty", nameof(htmlContent));
        }

        var systemMessage = new ChatMessage(
            ChatRole.System,
            @"You are an expert at converting HTML to clean, readable Markdown.

1. Extract only the main content, ignoring:
   - Navigation menus, headers, footers
   - Advertisements and promotional content
   - Social media buttons and share widgets
   - Comments sections
   - Scripts, styles, and metadata

2. Preserve important formatting:
   - Headings (convert to # ## ### etc.)
   - Lists (ordered and unordered)
   - Links (use [text](url) format)
   - Bold and italic text
   - Code blocks if present

3. Output clean Markdown:
   - Remove excessive whitespace (max 2 consecutive newlines)
   - Keep content readable and well-structured
   - Focus on the actual article/content text
");

        var userMessage = new ChatMessage(
            ChatRole.User,
            $"Convert the following HTML to clean Markdown:\n\n{htmlContent}");

        var messages = new List<ChatMessage> { systemMessage, userMessage };
        // var messages = new List<ChatMessage> { userMessage };

        var chatOptions = ChatOptionsFactory.CreateForConversion();

        _logger.LogInformation("Starting HTML to Markdown conversion. Input length: {Length} characters", htmlContent.Length);
        // Call LLM
        var completion = await _llmClient.ChatClient.GetResponseAsync<MarkdownResponse>(
            messages,
            chatOptions,
            cancellationToken: cancellationToken);

        var markdown = completion?.Result?.Markdown ?? string.Empty;

        markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
        markdown = Regex.Replace(markdown, @"[ \t]+\n", "\n");

        _logger.LogDebug("LLM conversion successful, output length: {Length}", markdown.Length);

        return markdown.Trim();
    }

    /// <summary>
    /// Response structure from the LLM containing the converted markdown.
    /// </summary>
    private record MarkdownResponse
    {
        /// <summary>
        /// The HTML content converted to Markdown format.
        /// </summary>
        public string Markdown { get; init; } = string.Empty;
    }
}

