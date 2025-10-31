using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class TextSummarizer : ITextSummarizer
{
    private readonly ILLMClient _llmClient;
    private readonly ILogger<TextSummarizer> _logger;

    private const string DefaultPrompt = "Summarize the following content concisely while preserving the key information.";

    public TextSummarizer(ILLMClient llmClient, ILogger<TextSummarizer> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> SummarizeAsync(
        string content,
        string prompt,
        int targetWordCount,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty", nameof(content));
        }

        if (targetWordCount <= 0)
        {
            throw new ArgumentException("Target word count must be greater than zero", nameof(targetWordCount));
        }

        // Use default prompt if none provided
        var effectivePrompt = string.IsNullOrWhiteSpace(prompt) ? DefaultPrompt : prompt;

        _logger.LogInformation("Starting summarization. Original content length: {ContentLength} characters, Target word count: {TargetWordCount}",
            content.Length, targetWordCount);

        try
        {
            // Build the system message with instructions
            var systemMessage = new ChatMessage(
                ChatRole.System,
                $"You are a professional summarization assistant. {effectivePrompt} " +
                $"Target the summary to be approximately {targetWordCount} words. " +
                $"Be concise but ensure all critical information is retained. " +
                $"Respond with only the summary text, no additional formatting or preamble.");

            // Build the user message with the content
            var userMessage = new ChatMessage(
                ChatRole.User,
                content);

            var messages = new List<ChatMessage> { systemMessage, userMessage };

            // Call the LLM using the same pattern as other tools
            var chatOptions = ChatOptionsFactory.CreateForSummarization();

            var jsonOptions = MentorJsonSerializerContext.CreateOptions();
            var completion = await _llmClient.ChatClient.GetResponseAsync<SummaryResponse>(
                messages,
                jsonOptions,
                chatOptions,
                cancellationToken: cancellationToken);

            var summary = completion?.Result?.Summary ?? string.Empty;

            var summaryWordCount = CountWords(summary);
            _logger.LogInformation("Summarization complete. Summary length: {SummaryLength} characters, {WordCount} words",
                summary.Length, summaryWordCount);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during summarization");
            throw;
        }
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

