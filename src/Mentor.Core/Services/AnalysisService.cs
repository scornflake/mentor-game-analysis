using System.Text.Json;
using Mentor.Core.Models;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Mentor.Core.Services;

public class AnalysisService : IAnalysisService
{
    private readonly IChatClient _chatClient;

    public AnalysisService(IChatClient chatClient)
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
        var systemMessage = new ChatMessage(ChatRole.System,
            "You are an expert game advisor. Analyze the provided screenshot and provide actionable recommendations. " +
            "Return your response as JSON with the following structure: " +
            "{ \"analysis\": \"detailed analysis\", \"summary\": \"brief summary\", " +
            "\"recommendations\": [{ \"priority\": \"High|Medium|Low\", \"action\": \"what to do\", " +
            "\"reasoning\": \"why\", \"context\": \"relevant context\" }], \"confidence\": 0.0-1.0 }");

        var memoryStream = new ReadOnlyMemory<byte>(request.ImageData);
        var userImage = new DataContent(memoryStream, "image/png");
        List<AIContent> content = new List<AIContent> { new TextContent(request.Prompt), userImage };
        var userMessage = new ChatMessage(ChatRole.User, content);

        var messages = new List<ChatMessage> { systemMessage, userMessage };

        // Call the LLM
        var options = new ChatOptions();
        var completion = await _chatClient.GetResponseAsync<LLMResponse>(messages, options, cancellationToken: cancellationToken);

        // Parse the response
        var jsonResponse = completion.Result;

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
    private record LLMResponse(string Analysis, string Summary, List<LLMRecommendation> Recommendations, double Confidence);

    private record LLMRecommendation(string Priority, string Action, string Reasoning, string Context);
}