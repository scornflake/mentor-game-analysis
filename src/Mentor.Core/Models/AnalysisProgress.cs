namespace Mentor.Core.Models;

public class AnalysisProgress
{
    public double TotalPercentage { get; set; }
    public List<AnalysisJob> Jobs { get; set; } = [];

    /// <summary>
    /// Updates the progress and status of a specific job identified by its tag.
    /// If the job does not exist, it creates a new job with the provided tag and initializes it.
    /// Also recalculates the total percentage progress for all jobs.
    /// </summary>
    /// <param name="tag">The identifier for the job to update or create.</param>
    /// <param name="status">The new status to set for the job.</param>
    /// <param name="progress">The new progress value to set for the job, or null.</param>
    public void UpdateJobProgress(string tag, JobStatus status, double? progress)
    {
        var job = Jobs.FirstOrDefault(j => j.Tag == tag);
        if (job == null)
        {
            // make a new one
            job = new AnalysisJob { Tag = tag, Name = $"Job for {tag}", Status = JobStatus.Pending, Progress = 0 };
            Jobs.Add(job);
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

    public void Merge(AnalysisProgress other)
    {
        if (other == null) return;
        
        foreach (var otherJob in other.Jobs)
        {
            var existingJob = Jobs.FirstOrDefault(j => j.Tag == otherJob.Tag);
            if (existingJob != null)
            {
                // Replace existing job (newer wins)
                var index = Jobs.IndexOf(existingJob);
                Jobs[index] = new AnalysisJob
                {
                    Tag = otherJob.Tag,
                    Name = otherJob.Name,
                    Status = otherJob.Status,
                    Progress = otherJob.Progress
                };
            }
            else
            {
                // Add new job
                Jobs.Add(new AnalysisJob
                {
                    Tag = otherJob.Tag,
                    Name = otherJob.Name,
                    Status = otherJob.Status,
                    Progress = otherJob.Progress
                });
            }
        }
        
        RecalculateTotalPercentage();
    }

    public void AddJob(AnalysisJob analysisJob)
    {
        Jobs.Add(analysisJob);
    }
}

