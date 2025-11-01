using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/*
 * Claude is good enough that we don't need to do anything special
 * Just fire off the request and use the response as-is
 */
public class ClaudeAnalysisService: AnalysisService
{
    public ClaudeAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory) : base(llmClient, logger, toolFactory)
    {
    }

    protected override bool UseJsonResponseFormat()
    {
        // Claude via OpenAI-compatible API doesn't support native structured output the same way
        return false;
    }

    public override async Task<Recommendation> AnalyzeAsync(AnalysisRequest request, IProgress<AnalysisProgress>? progress = null, IProgress<AIContent>? aiProgress = null, CancellationToken cancellationToken = default)
    {
        await base.AnalyzeAsync(request, progress, aiProgress, cancellationToken);
        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages_ForStreaming(request);
    
        var messages = new List<ChatMessage> { systemMessage };
        messages.AddRange(userMessage);

        var options = await CreateAIOptions();
        return await ExecuteAndParse_ForStreaming(messages, options, progress, aiProgress, cancellationToken);
    }
}

