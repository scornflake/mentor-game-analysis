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
    public string Name { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public double? Progress { get; set; } // Nullable for discrete jobs that don't have progress
}

