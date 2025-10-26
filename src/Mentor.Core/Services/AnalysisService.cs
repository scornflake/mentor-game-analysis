using System.ComponentModel;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Mentor.Core.Services;

public class AnalysisService : IAnalysisService
{
    private readonly ILLMClient _llmClient;
    private readonly IWebsearch _websearch;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(ILLMClient llmClient, IWebsearch websearch, ILogger<AnalysisService> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _websearch = websearch;
        _logger = logger;
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
            "Backup your analysis with relevant web search results when necessary. " +
            "Backup your recommendations using web search results");
        
        // should not be required as we request a structured response
        // "Return your response as JSON with the following structure: " +
        //     "{ \"analysis\": \"detailed analysis\", \"summary\": \"brief summary\", " +
        //     "\"recommendations\": [{ \"priority\": \"High|Medium|Low\", \"action\": \"what to do\", " +
        //     "\"reasoning\": \"why\", \"context\": \"relevant context\" }], \"confidence\": 0.0-1.0 }");

        var memoryStream = new ReadOnlyMemory<byte>(request.ImageData);
        var userImage = new DataContent(memoryStream, "image/png");
        List<AIContent> content = new List<AIContent> { new TextContent(request.Prompt), userImage };
        var userMessage = new ChatMessage(ChatRole.User, content);

        var messages = new List<ChatMessage> { systemMessage, userMessage };

        // Call the LLM
        [Description("Tool to perform web searches")]
        string SearchTheWeb(string query)
        {
            _logger.LogInformation("Performing web search for query: {Query}", query);
            return _websearch.Search(query, SearchOutputFormat.Summary, 5).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        
        IList<AITool> tools = [ AIFunctionFactory.Create(SearchTheWeb) ]; 
        var options = new ChatOptions()
        {
            ToolMode = ChatToolMode.RequireAny, 
        };
        if (_llmClient.Configuration.UseWebSearchTool)
        {
            options.Tools = tools;
        }

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