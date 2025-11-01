using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/// <summary>
/// OpenAI analysis service with Retrieval-Augmented Generation (RAG).
/// Performs upfront research using a research tool, then includes results in the prompt.
/// Active when RetrievalAugmentedGeneration configuration flag is true.
/// </summary>
public class OpenAIAnalysisServiceRAG : OpenAIAnalysisServiceBase
{
    public OpenAIAnalysisServiceRAG(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory, SearchResultFormatter searchResultFormatter, GameRuleRepository? gameRuleRepository = null) 
        : base(llmClient, logger, toolFactory, searchResultFormatter, gameRuleRepository)
    {
    }

    public override async Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request, 
        IProgress<AnalysisProgress>? progress = null, 
        IProgress<AIContent>? aiProgress = null, 
        CancellationToken cancellationToken = default)
    {
        _ = base.AnalyzeAsync(request, progress, aiProgress, cancellationToken);

        // Create progress wrapper that merges research progress with our own
        IProgress<AnalysisProgress>? researchProgress = null;
        if (progress != null)
        {
            researchProgress = new Progress<AnalysisProgress>(researchProgressReport =>
            {
                // Merge research progress into our analysis progress
                _analysisProgress!.Merge(researchProgressReport);

                // Report combined progress to client
                _analysisProgress.ReportProgress(progress);
            });
        }

        // Get research tool and perform upfront research
        _logger.LogInformation("Performing upfront research for RAG");
        var researchTool = await _toolFactory.GetResearchToolAsync(
            KnownTools.BasicResearch, 
            KnownSearchTools.Tavily, 
            _llmClient);
        _researchResults = await researchTool.PerformResearchAsync(request, ResearchMode.SummaryOnly, researchProgress, cancellationToken);

        _logger.LogInformation("Research completed with {Count} results", _researchResults?.Count ?? 0);

        // Now perform the analysis with research results included in prompt
        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages(request);
        var messages = new List<ChatMessage> { systemMessage };
        messages.AddRange(userMessage);

        var options = await CreateAIOptions();
        
        return await ExecuteAndParse(messages, options, progress, aiProgress, cancellationToken);
    }

    protected override async Task<ChatOptions> CreateAIOptions()
    {
        var options = await base.CreateAIOptions();
        // No tools provided to LLM - research is done upfront
        options.Tools = [];
        return options;
    }
}

