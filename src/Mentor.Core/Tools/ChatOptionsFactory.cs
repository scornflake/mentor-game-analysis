using Microsoft.Extensions.AI;

namespace Mentor.Core.Tools;

/// <summary>
/// Factory class for creating ChatOptions instances with predefined configurations
/// for different use cases throughout the application.
/// </summary>
public static class ChatOptionsFactory
{
    /// <summary>
    /// Default maximum output tokens for all ChatOptions instances created by this factory.
    /// </summary>
    private const int DefaultMaxOutputTokens = 8000;

    /// <summary>
    /// Creates a base ChatOptions instance with default MaxOutputTokens.
    /// This is the single place where ChatOptions is constructed.
    /// </summary>
    /// <returns>A new ChatOptions instance with MaxOutputTokens set.</returns>
    private static ChatOptions CreateBase()
    {
        return new ChatOptions
        {
            MaxOutputTokens = DefaultMaxOutputTokens
        };
    }

    /// <summary>
    /// Creates default ChatOptions for basic use cases.
    /// </summary>
    /// <returns>A new ChatOptions instance with MaxOutputTokens set to 12000.</returns>
    public static ChatOptions CreateDefault()
    {
        return CreateBase();
    }

    /// <summary>
    /// Creates ChatOptions optimized for HTML to Markdown conversion.
    /// Uses lower temperature for more consistent output.
    /// </summary>
    /// <param name="temperature">Temperature setting (default: 0.3f for consistent output).</param>
    /// <returns>A ChatOptions instance configured for conversion tasks.</returns>
    public static ChatOptions CreateForConversion(float? temperature = 0.3f)
    {
        var options = CreateBase();
        options.Temperature = temperature;
        return options;
    }

    /// <summary>
    /// Creates ChatOptions optimized for text summarization.
    /// Uses lower temperature for more focused summaries.
    /// </summary>
    /// <param name="temperature">Temperature setting (default: 0.3f for focused summaries).</param>
    /// <returns>A ChatOptions instance configured for summarization tasks.</returns>
    public static ChatOptions CreateForSummarization(float? temperature = 0.3f)
    {
        var options = CreateBase();
        options.Temperature = temperature;
        return options;
    }

    /// <summary>
    /// Creates ChatOptions optimized for image analysis.
    /// Uses moderate temperature for balanced creative and analytical responses.
    /// </summary>
    /// <param name="temperature">Temperature setting (default: 0.7f for balanced responses).</param>
    /// <returns>A ChatOptions instance configured for image analysis tasks.</returns>
    public static ChatOptions CreateForImageAnalysis(float? temperature = 0.7f)
    {
        var options = CreateBase();
        options.Temperature = temperature;
        return options;
    }

    /// <summary>
    /// Creates ChatOptions with a custom temperature setting.
    /// Useful for generic use cases that only need temperature control.
    /// </summary>
    /// <param name="temperature">Temperature setting for the chat options.</param>
    /// <returns>A ChatOptions instance with the specified temperature and MaxOutputTokens set to 12000.</returns>
    public static ChatOptions CreateWithTemperature(float temperature)
    {
        var options = CreateBase();
        options.Temperature = temperature;
        return options;
    }
}

