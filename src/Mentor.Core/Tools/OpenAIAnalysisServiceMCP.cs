using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

/// <summary>
/// OpenAI analysis service for MCP (Model Context Protocol) enabled servers.
/// Similar to basic implementation but configures ToolMode for MCP server-side tools.
/// Active when ServerHasMcpSearch configuration flag is true.
/// </summary>
public class OpenAIAnalysisServiceMCP : OpenAIAnalysisServiceBase
{
    public OpenAIAnalysisServiceMCP(ILLMClient llmClient, ILogger<AnalysisService> logger, IToolFactory toolFactory, SearchResultFormatter searchResultFormatter) 
        : base(llmClient, logger, toolFactory, searchResultFormatter)
    {
    }

    public override async Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request, 
        IProgress<AnalysisProgress>? progress = null, 
        IProgress<AIContent>? aiProgress = null, 
        CancellationToken cancellationToken = default)
    {
        _ = base.AnalyzeAsync(request, progress, aiProgress, cancellationToken);
    
        _analysisProgress?.UpdateJobProgress(AnalysisJob.JobTag.LLMAnalysis, JobStatus.InProgress, 0);
        _analysisProgress?.ReportProgress(progress);

        var systemMessage = GetSystemPrompt(request);
        var userMessage = GetUserMessages_ForStreaming(request);
        var messages = new List<ChatMessage> { systemMessage };
        messages.AddRange(userMessage);

        var options = await CreateAIOptions();
        
        return await ExecuteAndParse(messages, options, progress, aiProgress, cancellationToken);
    }

    protected override string GetSystemPromptText(AnalysisRequest request)
    {
        var msg = base.GetSystemPromptText(request);
        
        // Add MCP-specific guidance to the prompt
        msg += "\n\nNote: You have access to server-side tools via MCP (Model Context Protocol). " +
               "Use these tools to perform research and gather information as needed.";
        
        return msg;
    }

    protected override async Task<ChatOptions> CreateAIOptions()
    {
        var options = await base.CreateAIOptions();
        
        // Configure for MCP - let the server handle tools
        options.ToolMode = ChatToolMode.Auto;
        // No local tools - MCP server provides them
        options.Tools = [];
        
        _logger.LogInformation("Configured ChatOptions for MCP with ToolMode.Auto");
        
        return options;
    }
}

