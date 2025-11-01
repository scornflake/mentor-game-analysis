using System.Diagnostics;
using System.Text;
using System.Text.Json;
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
    protected AnalysisRequest? _currentRequest = null;
    protected AnalysisProgress? _analysisProgress;

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
               "Do some research first. Back up all findings with accurate and relevant articles. Make sure the articles are reasonably recent and representative.";

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
        _logger.LogInformation("User prompt: {Prompt}", GetUserPromptText(request));
        return new TextContent(GetUserPromptText(request));
    }

    protected virtual string GetUserPromptText(AnalysisRequest request)
    {
        return request.Prompt;
    }

    protected virtual List<ChatMessage> GetUserMessages(AnalysisRequest request)
    {
        // Prompt augmentation
        List<ChatMessage> userMessages = new List<ChatMessage>();
        // Convert image to PNG for LLM compatibility
        var pngImageData = request.ImageData.ConvertToPng();
        var imageContent = new DataContent(new ReadOnlyMemory<byte>(pngImageData.Data), pngImageData.MimeType);
        var content = new List<AIContent> { GetUserPrompt(request), imageContent };
        userMessages.Add(new ChatMessage(ChatRole.User, content));
        return userMessages;
    }

    protected virtual List<ChatMessage> GetUserMessages_ForStreaming(AnalysisRequest request)
    {
        // Prompt augmentation
        List<ChatMessage> userMessages = new List<ChatMessage>();
        // Convert image to PNG for LLM compatibility
        var pngImageData = request.ImageData.ConvertToPng();
        var imageContent = new DataContent(new ReadOnlyMemory<byte>(pngImageData.Data), pngImageData.MimeType);
        var content = new List<AIContent> { GetUserPrompt(request), imageContent };
        userMessages.Add(new ChatMessage(ChatRole.User, content));

        if (!UseJsonResponseFormat())
        {
            // need to add extra instructions to get structured output
            // When not using native JSON schema, augment the chat messages with a schema prompt
            var schema = ChatResponseForLLMResponseObject();
            if (schema is ChatResponseFormatJson responseFormatJson)
            {
                var promptAugmentation = new ChatMessage(ChatRole.User, $$"""
                                                                          Respond with a JSON value conforming to the following schema:
                                                                          ```
                                                                          {{responseFormatJson.Schema}}
                                                                          ```
                                                                          """);
                userMessages.Add(promptAugmentation);
            }
        }

        return userMessages;
    }

    public virtual async Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        IProgress<AnalysisProgress>? progress = null,
        IProgress<AIContent>? aiProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Store request for use by other tools
        _currentRequest = request;
        _currentRequest.ValidateRequest();

        // Initialize progress tracking
        _analysisProgress = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>()
        };
        progress?.Report(_analysisProgress);

        await Task.CompletedTask;
        return new Recommendation();
    }

    protected virtual bool UseJsonResponseFormat()
    {
        // default to something more ... stupid
        return false;
    }

    protected ChatResponseFormat GetChatResponseFormat()
    {
        if (UseJsonResponseFormat())
        {
            return ChatResponseForLLMResponseObject();
        }

        return ChatResponseFormat.Text;
    }

    private static ChatResponseFormat ChatResponseForLLMResponseObject()
    {
        var jsonOptions = MentorJsonSerializerContext.CreateOptions();
        var responseFormat = ChatResponseFormat.ForJsonSchema<LLMResponse>(jsonOptions);
        if (!responseFormat.Schema.HasValue)
        {
            throw new InvalidOperationException("ForJsonSchema did not generate a schema");
        }

        return responseFormat;
    }

    protected virtual async Task<ChatOptions> CreateAIOptions()
    {
        // Generate schema from your type
        await Task.CompletedTask;
        var options = ChatOptionsFactory.CreateDefault();
        options.ResponseFormat = GetChatResponseFormat();
        return options;
    }

    private record MapCallId(string CallId, string ToolName, string Parameters);

    protected virtual async Task<Recommendation> ExecuteAndParse(List<ChatMessage> messages, ChatOptions options, IProgress<AnalysisProgress>? progress = null, IProgress<AIContent>? aiProgress = null, CancellationToken cancellationToken = default)
    {
        DumpMessages(messages, options);

        // UpdateJobProgress will create a job if it doesn't exist
        _analysisProgress?.UpdateJobProgress(AnalysisJob.JobTag.LLMAnalysis, JobStatus.InProgress, 0);
        _analysisProgress?.ReportProgress(progress);

        try
        {
            var jsonOptions = MentorJsonSerializerContext.CreateOptions();
            var chatResponse = await _llmClient.ChatClient.GetResponseAsync<LLMResponse>(messages, jsonOptions, options, true, cancellationToken);
            var jsonResponse = chatResponse.Result;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing and parsing LLM response");
            throw;
        }
        finally
        {
            _analysisProgress?.UpdateJobProgress(AnalysisJob.JobTag.LLMAnalysis, JobStatus.Completed, 100);
            _analysisProgress?.ReportProgress(progress);
        }
    }

    private void DumpMessages(List<ChatMessage> messages, ChatOptions options)
    {
        _logger.LogInformation("Starting LLM streaming request with {ToolCount} tools available", options.Tools?.Count ?? 0);
        foreach (var message in messages)
        {
            _logger.LogDebug("Message Role: {Role}, Content: {Content}", message.Role, message.Contents);
        }
    }

    protected virtual async Task<Recommendation> ExecuteAndParse_ForStreaming(List<ChatMessage> messages, ChatOptions options, IProgress<AnalysisProgress>? progress = null, IProgress<AIContent>? aiProgress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LLM streaming request with {ToolCount} tools available", options.Tools?.Count ?? 0);
        foreach (var message in messages)
        {
            _logger.LogDebug("Message Role: {Role}, Content: {Content}", message.Role, message.Contents);
        }
    
        // UpdateJobProgress will create a job if it doesn't exist
        _analysisProgress?.UpdateJobProgress(AnalysisJob.JobTag.LLMAnalysis, JobStatus.InProgress, 0);
        _analysisProgress?.ReportProgress(progress);
    
        try
        {
            var updates = new List<ChatResponseUpdate>();
            var textBuilder = new StringBuilder();
    
            // Stream the completion to get real-time feedback
            // await _llmClient.ChatClient.GetResponseAsync<LLMResponse>(messages, options, cancellationToken);
            await foreach (var update in _llmClient.ChatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                updates.Add(update);
    
                // Report all content to aiProgress for UI display
                foreach (var content in update.Contents)
                {
                    aiProgress?.Report(content);
    
                    // Accumulate text for parsing
                    if (content is TextContent textContent && !string.IsNullOrEmpty(textContent.Text))
                    {
                        textBuilder.Append(textContent.Text);
                    }
                }
            }
    
            // Log final completion statistics
            var finalUpdate = updates.LastOrDefault();
            if (finalUpdate != null)
            {
                _logger.LogInformation("LLM streaming completed. Finish reason: {Reason}", finalUpdate.FinishReason);
            }
    
            // Collect all text for parsing
            var fullText = textBuilder.ToString();
            _logger.LogDebug("Full response text length: {Length} chars", fullText.Length);
    
            // Extract the JSON response from the text. We want the text between the <findings>...</findings> tags.
            var jsonText = await ExtractJsonFromResponse(fullText);
            if (string.IsNullOrEmpty(jsonText))
            {
                _logger.LogError("No JSON found in response");
                return new Recommendation
                {
                    Analysis = "No analysis provided",
                    Summary = "Nothing found in response from LLM server. Please check it's logs.",
                    Recommendations = [],
                    Confidence = 0.0
                };
            }
    
            // Parse the JSON response from the text
            var jsonOptions = MentorJsonSerializerContext.CreateOptions();
            LLMResponse jsonResponse;
            try
            {
                jsonResponse = JsonSerializer.Deserialize<LLMResponse>(jsonText, jsonOptions)
                               ?? throw new InvalidOperationException("Failed to deserialize LLM response");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response as JSON. Response text: {Text}",
                    jsonText.Length > 500 ? jsonText.Substring(0, 500) + "..." : jsonText);
                throw;
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
                    Context = r.Context ?? string.Empty,
                    ReferenceLink = r.ReferenceLink ?? string.Empty
                }).ToList() ?? [],
                Confidence = jsonResponse.Confidence,
                GeneratedAt = DateTime.UtcNow,
                ProviderUsed = _llmClient.Configuration.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing and parsing LLM response");
            throw;
        }
        finally
        {
            _analysisProgress?.UpdateJobProgress(AnalysisJob.JobTag.LLMAnalysis, JobStatus.Completed, 100);
            _analysisProgress?.ReportProgress(progress);
        }
    }

    /// <summary>
    /// Extracts the JSON response from the provided text, looking for <findings>...</findings> or ```json ... ```
    /// Returns the extracted JSON string or the original text if no tags are found.
    /// </summary>
    protected virtual async Task<string> ExtractJsonFromResponse(string fullText)
    {
        string? extracted = null;

        // Try to extract JSON between <findings>...</findings>
        var startFindings = fullText.IndexOf("<findings>", StringComparison.OrdinalIgnoreCase);
        var endFindings = fullText.IndexOf("</findings>", StringComparison.OrdinalIgnoreCase);
        if (startFindings != -1 && endFindings != -1 && endFindings > startFindings)
        {
            startFindings += "<findings>".Length;
            var length = endFindings - startFindings;
            if (length > 0)
            {
                extracted = fullText.Substring(startFindings, length);
                _logger.LogDebug("Extracted JSON from <findings> tags.");
            }
        }
        else
        {
            // Try to extract JSON between ```json and ```
            var startJson = fullText.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
            if (startJson != -1)
            {
                startJson += "```json".Length;
                var endJson = fullText.IndexOf("```", startJson, StringComparison.OrdinalIgnoreCase);
                if (endJson != -1 && endJson > startJson)
                {
                    // skip leading newline if present
                    if (startJson < fullText.Length && (fullText[startJson] == '\n' || fullText[startJson] == '\r'))
                        startJson++;
                    var length = endJson - startJson;
                    if (length > 0)
                    {
                        extracted = fullText.Substring(startJson, length).Trim();
                        _logger.LogDebug("Extracted JSON from ```json block.");
                    }
                }
            }
        }

        extracted ??= fullText; // fallback to original if nothing found

        _logger.LogDebug("JSON text: {JsonText}", extracted);

        await Task.CompletedTask;
        return extracted;
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