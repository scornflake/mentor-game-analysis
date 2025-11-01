using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/// <summary>
/// Basic OpenAI analysis service - no tools, no upfront research.
/// Just sends a prompt with the image and parses the response.
/// </summary>
public class OpenAIAnalysisService : OpenAIAnalysisServiceBase
{
    public OpenAIAnalysisService(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory, SearchResultFormatter searchResultFormatter, GameRuleRepository? gameRuleRepository = null) 
        : base(llmClient, logger, toolFactory, searchResultFormatter, gameRuleRepository)
    {
    }

    /// <summary>
    /// Static factory method to create the appropriate OpenAI analysis service based on configuration
    /// </summary>
    public static OpenAIAnalysisServiceBase Create(
        ILLMClient llmClient, 
        ILogger<AnalysisService> logger, 
        IToolFactory toolFactory,
        SearchResultFormatter searchResultFormatter,
        GameRuleRepository? gameRuleRepository = null)
    {
        // Then check RetrievalAugmentedGeneration
        if (llmClient.Configuration.RetrievalAugmentedGeneration)
        {
            logger.LogInformation("Creating OpenAIAnalysisServiceRAG for upfront research");
            return new OpenAIAnalysisServiceRAG(llmClient, logger, toolFactory, searchResultFormatter, gameRuleRepository);
        }

        // Default to basic implementation
        logger.LogInformation("Creating basic OpenAIAnalysisService");
        return new OpenAIAnalysisService(llmClient, logger, toolFactory, searchResultFormatter, gameRuleRepository);
    }

    public override async Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request, 
        IProgress<AnalysisProgress>? progress = null, 
        IProgress<AIContent>? aiProgress = null, 
        CancellationToken cancellationToken = default)
    {
        _ = base.AnalyzeAsync(request, progress, aiProgress, cancellationToken);

        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages_ForStreaming(request);
        var messages = new List<ChatMessage> { systemMessage };
        messages.AddRange(userMessage);

        var options = await CreateAIOptions();
        
        return await ExecuteAndParse(messages, options, progress, aiProgress, cancellationToken);
    }

}
