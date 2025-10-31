namespace Mentor.Core.Models;

public enum JobStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public class AnalysisJob
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public double? Progress { get; set; } // Nullable for discrete jobs that don't have progress

    public static class JobTag
    {
        public const string LLMAnalysis = "analyze-llm";
        public const string WebSearch = "search-web";
        public const string ArticleN = "article-";
        
        public static string GetArticleTag(int index) => $"{ArticleN}{index}";
    }
}

