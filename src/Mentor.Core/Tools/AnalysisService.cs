using Mentor.Core.Interfaces;
using Mentor.Core.Models;
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
    
    protected virtual ChatMessage GetSystemPrompt()
    {
        var systemMessage = new ChatMessage(ChatRole.System,
            "You are an expert game advisor. Analyze the provided screenshot and provide actionable recommendations. " +
            "Backup your analysis with relevant web search results when necessary. " +
            "Backup your recommendations using web search results");
        return systemMessage;
    }

    protected virtual TextContent GetUserPrompt(AnalysisRequest request)
    {
        return new TextContent(request.Prompt);
    }

    protected virtual ChatMessage GetUserMessages(AnalysisRequest request)
    {
        var content = new List<AIContent> { GetUserPrompt(request), request.GetImageAsReadOnlyMemory() };
        return new ChatMessage(ChatRole.User, content);
    }

    public virtual Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected virtual async Task<ChatOptions> CreateAIOptions()
    {
        await Task.CompletedTask;
        return new ChatOptions();
    }

    protected virtual async Task<Recommendation> ExecuteAndParse(List<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        var completion = await _llmClient.ChatClient.GetResponseAsync<LLMResponse>(messages, options, cancellationToken: cancellationToken);

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

    private record LLMResponse(string Analysis, string Summary, List<LLMRecommendation> Recommendations, double Confidence);

    private record LLMRecommendation(string Priority, string Action, string Reasoning, string Context);
}