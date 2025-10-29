using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class ImageAnalyzer : IImageAnalyzer
{
    private readonly ILogger<ImageAnalyzer> _logger;

    public ImageAnalyzer(ILogger<ImageAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(
        RawImage imageData,
        string gameName,
        ILLMClient provider,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (imageData == null || imageData.Data == null || imageData.Data.Length == 0)
        {
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
        }

        if (string.IsNullOrWhiteSpace(gameName))
        {
            throw new ArgumentException("Game name cannot be null or empty", nameof(gameName));
        }

        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        _logger.LogInformation("Analyzing image for game relevance: {GameName}, type: {Type} using {LLM}", gameName, imageData.MimeType, provider.Configuration.Name);

        // Create system prompt
        var systemPrompt = GetSystemPrompt(gameName);

        // Convert image to PNG for LLM compatibility
        var pngImageData = imageData.ConvertToPng();
        
        // Create user message with image
        var userMessage = GetUserMessage(pngImageData);

        // Create messages list
        var messages = new List<ChatMessage>
        {
            systemPrompt,
            userMessage
        };

        // Create options for structured response
        var options = new ChatOptions
        {
            Temperature = 0.7f
        };

        try
        {
            // Get structured response from LLM
            var completion = await provider.ChatClient.GetResponseAsync<ImageAnalysisResponse>(
                messages, 
                options, 
                cancellationToken: cancellationToken);

            var response = completion.Result;

            _logger.LogInformation(
                "Analysis complete. Game relevance: {Probability:P0}", 
                response.GameRelevanceProbability);

            // Map to result model
            return new ImageAnalysisResult
            {
                Description = response.Description ?? string.Empty,
                GameRelevanceProbability = response.GameRelevanceProbability,
                GeneratedAt = DateTime.UtcNow,
                ProviderUsed = provider.Configuration.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image for game: {GameName}", gameName);
            throw;
        }
    }

    private ChatMessage GetSystemPrompt(string gameName)
    {
        var promptText = 
            $"You are an expert image analyzer. Your task is to:\n" +
            $"1. Provide a detailed, comprehensive description of the image content.\n" +
            $"2. Assess the probability (0.0 to 1.0) that the image is related to the game '{gameName}'.\n\n" +
            $"When assessing game relevance:\n" +
            $"- Look for UI elements, game mechanics, characters, or branding specific to '{gameName}'\n" +
            $"- Consider visual style and art direction typical of '{gameName}'\n" +
            $"- A probability of 1.0 means you're highly confident it's from '{gameName}'\n" +
            $"- A probability of 0.0 means you're highly confident it's NOT from '{gameName}'\n" +
            $"- Use intermediate values (0.3-0.7) when you're uncertain\n\n";
            // $"Your response must include:\n" +
            // $"- Description: A detailed description of what you see in the image\n" +
            // $"- GameRelevanceProbability: A number between 0.0 and 1.0 indicating likelihood the image is from '{gameName}'";

        return new ChatMessage(ChatRole.System, promptText);
    }

    private ChatMessage GetUserMessage(RawImage imageData)
    {
        var imageContent = new DataContent(new ReadOnlyMemory<byte>(imageData.Data), imageData.MimeType);
        var textContent = new TextContent("Analyze this image and assess its relevance to the specified game.");

        var content = new List<AIContent> { textContent, imageContent };
        return new ChatMessage(ChatRole.User, content);
    }

    /// <summary>
    /// Internal record for deserializing structured LLM responses
    /// </summary>
    private record ImageAnalysisResponse
    {
        /// <summary>
        /// Detailed description of the image content
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Probability (0.0 to 1.0) that the image is related to the specified game
        /// </summary>
        public double GameRelevanceProbability { get; init; }
    }
}

