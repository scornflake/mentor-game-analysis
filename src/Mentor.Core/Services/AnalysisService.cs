using System.Text.Json;
using Mentor.Core.Models;
using OpenAI.Chat;

namespace Mentor.Core.Services;

public class AnalysisService : IAnalysisService
{
    private readonly ChatClient _chatClient;

    public AnalysisService(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ImageData == null || request.ImageData.Length == 0)
        {
            throw new ArgumentException("Image data is required", nameof(request));
        }

        // Build the chat messages with image content
        var systemMessage = ChatMessage.CreateSystemMessage(
            "You are an expert game advisor. Analyze the provided screenshot and provide actionable recommendations. " +
            "Return your response as JSON with the following structure: " +
            "{ \"analysis\": \"detailed analysis\", \"summary\": \"brief summary\", " +
            "\"recommendations\": [{ \"priority\": \"High|Medium|Low\", \"action\": \"what to do\", " +
            "\"reasoning\": \"why\", \"context\": \"relevant context\" }], \"confidence\": 0.0-1.0 }");
        
        var userMessage = ChatMessage.CreateUserMessage(
            ChatMessageContentPart.CreateTextPart(request.Prompt),
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(request.ImageData), "image/png"));

        var messages = new List<ChatMessage> { systemMessage, userMessage };

        // Call the LLM
        var completion = await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        // Parse the response
        var responseText = completion.Value.Content[0].Text ?? string.Empty;
        
        // Try to parse as JSON
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<LLMResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (jsonResponse == null)
            {
                throw new InvalidOperationException("Failed to parse LLM response");
            }

            return new Recommendation
            {
                Analysis = jsonResponse.Analysis ?? "No analysis provided",
                Summary = jsonResponse.Summary ?? "No summary provided",
                Recommendations = jsonResponse.Recommendations?.Select(r => new RecommendationItem
                {
                    Priority = ParsePriority(r.Priority),
                    Action = r.Action ?? string.Empty,
                    Reasoning = r.Reasoning ?? string.Empty,
                    Context = r.Context ?? string.Empty
                }).ToList() ?? [],
                Confidence = jsonResponse.Confidence,
                GeneratedAt = DateTime.UtcNow,
                ProviderUsed = "openai"
            };
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return the raw response
            return new Recommendation
            {
                Analysis = responseText,
                Summary = "Response parsing failed",
                Recommendations = [],
                Confidence = 0.5,
                GeneratedAt = DateTime.UtcNow,
                ProviderUsed = "openai"
            };
        }
    }

    private static Priority ParsePriority(string? priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "high" => Priority.High,
            "medium" => Priority.Medium,
            "low" => Priority.Low,
            _ => Priority.Medium
        };
    }

    // Internal class for deserializing LLM response
    private class LLMResponse
    {
        public string? Analysis { get; set; }
        public string? Summary { get; set; }
        public List<LLMRecommendation>? Recommendations { get; set; }
        public double Confidence { get; set; }
    }

    private class LLMRecommendation
    {
        public string? Priority { get; set; }
        public string? Action { get; set; }
        public string? Reasoning { get; set; }
        public string? Context { get; set; }
    }
}

