using System.Text.Json;
using System.Text.Json.Serialization;
using Mentor.Core.Models;
using Mentor.Core.Tools;

namespace Mentor.Core.Serialization;

/// <summary>
/// JSON source generator context for preserving types during trimming.
/// This ensures that types used with Microsoft.Extensions.AI GetResponseAsync&lt;T&gt; are preserved.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LLMResponse))]
[JsonSerializable(typeof(LLMRecommendation))]
[JsonSerializable(typeof(ImageAnalysisResponse))]
[JsonSerializable(typeof(MarkdownResponse))]
[JsonSerializable(typeof(SummaryResponse))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(List<SearchResult>))]
[JsonSerializable(typeof(IList<SearchResult>))]
[JsonSerializable(typeof(BraveSearchResponse))]
[JsonSerializable(typeof(BraveWebResults))]
[JsonSerializable(typeof(TavilySearchRequest))]
[JsonSerializable(typeof(TavilySearchResponse))]
[JsonSerializable(typeof(TavilyResult))]
[JsonSerializable(typeof(GameRule))]
[JsonSerializable(typeof(List<GameRule>))]
// Note: SubjectiveEvaluation types are in a separate project, so they're handled via normal reflection
internal partial class MentorJsonSerializerContext : JsonSerializerContext
{
    /// <summary>
    /// Provides centralized JsonSerializerOptions configured with MentorJsonSerializerContext.
    /// Use this for all GetResponseAsync calls to ensure proper type resolution.
    /// </summary>
    public static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            TypeInfoResolver = MentorJsonSerializerContext.Default,
        };
    }
}

