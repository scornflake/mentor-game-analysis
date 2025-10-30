using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/*
 * Perplexity is good enough that we don't need to do anything
 * Just fire off the request and use the response as-is
 */
public class PerplexityAnalysisService: AnalysisService
{
    public PerplexityAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory) : base(llmClient, logger, toolFactory)
    {
    }

    public override async Task<Recommendation> AnalyzeAsync(AnalysisRequest request, IProgress<AnalysisProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        request.ValidateRequest();
        
        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages(request);
    
        var messages = new List<ChatMessage> { systemMessage, userMessage };
        var options = await CreateAIOptions();
        
        return await ExecuteAndParse(messages, options, progress, cancellationToken);
    }
}