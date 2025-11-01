using System.Text;
using System.Text.Json;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Serialization;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Uses an LLM to evaluate recommendations against subjective criteria
/// </summary>
public class SubjectiveEvaluationService
{
    private readonly ILogger _logger;

    public SubjectiveEvaluationService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SubjectiveEvaluationResult> EvaluateAsync(
        Recommendation recommendation,
        List<EvaluationCriterion> criteria,
        ILLMClient evaluatorClient,
        CancellationToken cancellationToken = default)
    {
        if (criteria == null || criteria.Count == 0)
        {
            return new SubjectiveEvaluationResult
            {
                OverallScore = 0,
                Evaluations = new()
            };
        }

        _logger.LogInformation("Running subjective evaluation with {Count} criteria", criteria.Count);

        var prompt = BuildEvaluationPrompt(recommendation, criteria);
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are an expert evaluator for Warframe game build recommendations. Provide objective, consistent scores based on the criteria."),
                new ChatMessage(ChatRole.User, prompt)
            };

            var options = new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json,
                Temperature = 0.3f // Lower temperature for more consistent evaluation
            };

            // Create JSON options with camelCase for consistency with LLM output
            var jsonOptions = RuleEvaluationJsonSerializerContext.CreateOptions();

            SubjectiveEvaluationLLMResponse? llmResult = null;

            try
            {
                // Try structured response first
                var response = await evaluatorClient.ChatClient.GetResponseAsync<SubjectiveEvaluationLLMResponse>(
                    messages,
                    jsonOptions,
                    options,
                    true,
                    cancellationToken
                );
                llmResult = response.Result;
            }
            catch (JsonException ex)
            {
                // LLM might have wrapped JSON in markdown code fences, try to extract and parse manually
                // _logger.LogWarning(ex, "Failed to parse structured response, attempting manual extraction from raw text");
                
                try
                {
                    // Make another call to get raw text (no structured parsing)
                    var textOptions = new ChatOptions
                    {
                        Temperature = 0.3f
                    };
                    var textMessages = new List<ChatMessage>(messages);
                    
                    var streamResponse = evaluatorClient.ChatClient.GetStreamingResponseAsync(textMessages, textOptions, cancellationToken);
                    var responseBuilder = new StringBuilder();
                    
                    await foreach (var update in streamResponse)
                    {
                        responseBuilder.Append(update.Text);
                    }
                    
                    var responseText = responseBuilder.ToString();
                    _logger.LogInformation("Raw LLM response: {Response}", responseText);
                    
                    // Extract JSON from markdown code fences
                    var jsonText = ExtractJsonFromResponse(responseText);
                    _logger.LogInformation("Extracted JSON: {Json}", jsonText);
                    
                    // Try deserializing the cleaned JSON
                    llmResult = JsonSerializer.Deserialize<SubjectiveEvaluationLLMResponse>(jsonText, jsonOptions);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(ex, "Failed to parse structured response...");
                    _logger.LogError(innerEx, "Failed to extract and parse JSON (fenced) from raw response");
                }
            }

            if (llmResult == null || llmResult.Evaluations == null)
            {
                _logger.LogWarning("Failed to parse evaluation response");
                return CreateFallbackResult(criteria);
            }

            // Map LLM results to our model
            var result = new SubjectiveEvaluationResult
            {
                Evaluations = llmResult.Evaluations.Select((e, idx) => new CriterionEvaluation
                {
                    CriterionIndex = e.CriterionIndex,
                    Criterion = criteria[e.CriterionIndex].Criterion,
                    Score = Math.Clamp(e.Score, 0, 10),
                    Reasoning = e.Reasoning ?? "No reasoning provided",
                    Weight = criteria[e.CriterionIndex].Weight
                }).ToList()
            };

            // Calculate weighted average
            var totalWeight = result.Evaluations.Sum(e => e.Weight);
            if (totalWeight > 0)
            {
                result.OverallScore = result.Evaluations.Sum(e => e.Score * e.Weight) / totalWeight;
            }
            else
            {
                result.OverallScore = result.Evaluations.Average(e => e.Score);
            }

            _logger.LogInformation("Subjective evaluation complete. Overall score: {Score:F2}/10", result.OverallScore);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during subjective evaluation");
            return CreateFallbackResult(criteria);
        }
    }

    private static string BuildEvaluationPrompt(Recommendation recommendation, List<EvaluationCriterion> criteria)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("You are evaluating Warframe build recommendations against specific criteria.");
        sb.AppendLine();
        sb.AppendLine("RECOMMENDATIONS TO EVALUATE:");
        sb.AppendLine("---");
        sb.AppendLine($"Summary: {recommendation.Summary}");
        sb.AppendLine();
        sb.AppendLine($"Analysis: {recommendation.Analysis}");
        sb.AppendLine();
        sb.AppendLine("Recommendations:");
        for (int i = 0; i < recommendation.Recommendations.Count; i++)
        {
            var rec = recommendation.Recommendations[i];
            sb.AppendLine($"{i + 1}. [{rec.Priority}] {rec.Action}");
            if (!string.IsNullOrWhiteSpace(rec.Reasoning))
            {
                sb.AppendLine($"   Reasoning: {rec.Reasoning}");
            }
        }
        sb.AppendLine("---");
        sb.AppendLine();
        
        sb.AppendLine("EVALUATION CRITERIA:");
        for (int i = 0; i < criteria.Count; i++)
        {
            var criterion = criteria[i];
            sb.AppendLine($"{i}. {criterion.Criterion}");
            sb.AppendLine($"   Expected: {criterion.Expectation} (weight: {criterion.Weight})");
        }
        sb.AppendLine();
        
        sb.AppendLine("For each criterion, provide:");
        sb.AppendLine("- Score: 0-10 (0=completely fails criterion, 10=perfectly meets criterion)");
        sb.AppendLine("- Reasoning: Brief explanation of the score");
        sb.AppendLine();
        sb.AppendLine("If expectation is 'negative', score 10 means the recommendation correctly AVOIDS the thing.");
        sb.AppendLine("If expectation is 'positive', score 10 means the recommendation strongly INCLUDES/RECOMMENDS the thing.");
        sb.AppendLine();
        sb.AppendLine("Return JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"evaluations\": [");
        sb.AppendLine("    {\"criterionIndex\": 0, \"score\": 8, \"reasoning\": \"explanation here\"},");
        sb.AppendLine("    {\"criterionIndex\": 1, \"score\": 6, \"reasoning\": \"explanation here\"}");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static SubjectiveEvaluationResult CreateFallbackResult(List<EvaluationCriterion> criteria)
    {
        return new SubjectiveEvaluationResult
        {
            OverallScore = 0,
            Evaluations = criteria.Select((c, idx) => new CriterionEvaluation
            {
                CriterionIndex = idx,
                Criterion = c.Criterion,
                Score = 0,
                Reasoning = "Evaluation failed",
                Weight = c.Weight
            }).ToList()
        };
    }

    private static string ExtractJsonFromResponse(string text)
    {
        var trimmed = text.Trim();
        
        // Check for markdown code fences (```json or ```)
        if (trimmed.StartsWith("```"))
        {
            var lines = trimmed.Split('\n');
            if (lines.Length < 3)
            {
                // Not enough lines for code fence, return as-is
                return trimmed;
            }
            
            // Skip first line (```json or ```) and last line (```)
            var jsonLines = new List<string>();
            for (int i = 1; i < lines.Length - 1; i++)
            {
                if (!lines[i].Trim().Equals("```"))
                {
                    jsonLines.Add(lines[i]);
                }
            }
            
            return string.Join('\n', jsonLines).Trim();
        }
        
        return trimmed;
    }
}

