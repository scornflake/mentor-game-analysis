namespace Mentor.Core.Models;

public class AnalysisProgress
{
    public double TotalPercentage { get; set; }
    public List<AnalysisJob> Jobs { get; set; } = [];

    public void UpdateJobProgress(string tag, JobStatus status, double? progress)
    {
        var job = Jobs.FirstOrDefault(j => j.Tag == tag);
        if (job == null)
        {
            throw new ArgumentException($"Job with tag '{tag}' not found.", nameof(tag));
        }

        job.Status = status;
        job.Progress = progress;
        RecalculateTotalPercentage();
    }

    public void RecalculateTotalPercentage()
    {
        if (Jobs.Count == 0)
        {
            TotalPercentage = 0;
            return;
        }

        TotalPercentage = Jobs
            .Select(j => j.Progress ?? 0)
            .DefaultIfEmpty(0)
            .Average();
        TotalPercentage = Math.Min(100, Math.Max(0, TotalPercentage));
    }

    public void ReportProgress(IProgress<AnalysisProgress>? progress)
    {
        if (progress != null)
        {
            RecalculateTotalPercentage();
            
            // Create a copy to avoid modification issues
            progress.Report(new AnalysisProgress
            {
                TotalPercentage = TotalPercentage,
                Jobs = Jobs.Select(j => new AnalysisJob
                {
                    Tag = j.Tag,
                    Name = j.Name,
                    Status = j.Status,
                    Progress = j.Progress
                }).ToList()
            });
        }
    }

    internal void AddJobAbove(string referenceTag, AnalysisJob newJob)
    {
        var referenceJob = Jobs.FirstOrDefault(j => j.Tag == referenceTag);
        if (referenceJob == null)
        {
            throw new ArgumentException($"Job with tag '{referenceTag}' not found.", nameof(referenceTag));
        }

        Jobs.Insert(Jobs.IndexOf(referenceJob), newJob);
        RecalculateTotalPercentage();
    }

    internal void SetName(string articleTag, string name)
    {
        var job = Jobs.FirstOrDefault(j => j.Tag == articleTag);
        if (job == null)
        {
            throw new ArgumentException($"Job with tag '{articleTag}' not found.", nameof(articleTag));
        }

        job.Name = name;
        RecalculateTotalPercentage();
    }
}

