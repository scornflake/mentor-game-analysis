using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Serialization;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Mentor.Core.Tools;

public abstract class AnalysisService : IAnalysisService
{
    internal readonly ILLMClient _llmClient;
    internal readonly ILogger<AnalysisService> _logger;
    internal readonly IToolFactory _toolFactory;

    public AnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger;
        _toolFactory = toolFactory;
    }

    protected virtual string GetSystemPromptText(AnalysisRequest request)
    {
        var msg = "You are an expert game advisor. ";
        if (!string.IsNullOrEmpty(request.GameName))
        {
            msg += $"The game being analyzed is '{request.GameName}'. ";
        }
        msg += "Analyze the provided screenshot and provide actionable recommendations. " +
               "Do some research first, back up your findings with actual articles. Make sure the articles are reasonably recent and representative." +
               "\n\n" +
               "Your response must be structured as follows:\n" +
               "- Analysis: Provide a detailed, comprehensive analysis of the screenshot/content based on visual observation and any web search results.\n" +
               "- Summary: Provide a brief, concise summary of key findings and insights.\n" +
               "- Recommendations: Provide a list of actionable recommendations. Each recommendation must include:\n" +
               "  * Priority: Must be exactly one of 'high', 'medium', or 'low'.\n" +
               "  * Action: A specific, clear, and concrete actionable step or item the user should take.\n" +
               "  * Reasoning: Explain why this recommendation is relevant and important, justifying the priority level.\n" +
               "  * Context: Include relevant context from the screenshot or analysis that supports this recommendation, with specific details observed.\n" +
               "  * ReferenceLink: If available, include a URL from web search results that supports this recommendation. Use an empty string if no reference link is available.\n" +
               "- Confidence: Provide a confidence score from 0.0 (low confidence) to 1.0 (high confidence) indicating how certain you are about your analysis.";
        return msg;
    }

    protected virtual ChatMessage GetSystemPrompt(AnalysisRequest request)
    {
        var msg = GetSystemPromptText(request);
        var systemMessage = new ChatMessage(ChatRole.System, msg);
        return systemMessage;
    }

    protected virtual TextContent GetUserPrompt(AnalysisRequest request)
    {
        return new TextContent(request.Prompt);
    }

    protected virtual ChatMessage GetUserMessages(AnalysisRequest request)
    {
        // Convert image to PNG for LLM compatibility
        var pngImageData = request.ImageData.ConvertToPng();
        var imageContent = new DataContent(new ReadOnlyMemory<byte>(pngImageData.Data), pngImageData.MimeType);
        var content = new List<AIContent> { GetUserPrompt(request), imageContent };
        return new ChatMessage(ChatRole.User, content);
    }

    public virtual Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected virtual async Task<ChatOptions> CreateAIOptions()
    {
        await Task.CompletedTask;
        return ChatOptionsFactory.CreateDefault();
    }

    protected virtual async Task<Recommendation> ExecuteAndParse(List<ChatMessage> messages, ChatOptions options, IProgress<AnalysisProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var jsonOptions = MentorJsonSerializerContext.CreateOptions();
        var completion = await _llmClient.ChatClient.GetResponseAsync<LLMResponse>(messages, jsonOptions, options, cancellationToken: cancellationToken);

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
                Context = r.Context ?? string.Empty,
                ReferenceLink = r.ReferenceLink ?? string.Empty
            }).ToList() ?? [],
            Confidence = jsonResponse.Confidence,
            GeneratedAt = DateTime.UtcNow,
            ProviderUsed = _llmClient.Configuration.Name
        };
    }

    internal virtual async Task<IList<AITool>> SetupTools()
    {
        await Task.CompletedTask;
        return [];
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
}