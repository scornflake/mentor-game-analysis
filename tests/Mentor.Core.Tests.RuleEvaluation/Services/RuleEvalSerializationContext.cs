using System.Text.Json;
using System.Text.Json.Serialization;
using Mentor.Core.Tests.RuleEvaluation.Models;

/// <summary>
/// JSON source generator context for preserving types during trimming.
/// This ensures that types used with Microsoft.Extensions.AI GetResponseAsync&lt;T&gt; are preserved.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SubjectiveEvaluationLLMResponse))]
[JsonSerializable(typeof(SubjectiveCriterionScore))]
[JsonSerializable(typeof(RuleCategorization))]
[JsonSerializable(typeof(CategoryAssignment))]
[JsonSerializable(typeof(MediaWikiApiResponse))]
[JsonSerializable(typeof(ParsedGameRule))]
internal partial class RuleEvaluationJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = RuleEvaluationJsonSerializerContext.Default,
        };
    }
}
