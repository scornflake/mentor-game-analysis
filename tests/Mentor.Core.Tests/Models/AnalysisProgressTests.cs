using Mentor.Core.Models;

namespace Mentor.Core.Tests.Models;

public class AnalysisProgressTests
{
    [Fact]
    public void Merge_WithNoOverlappingJobs_AppendsAllJobs()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Job 1", Status = JobStatus.InProgress, Progress = 50 },
                new() { Tag = "job2", Name = "Job 2", Status = JobStatus.Completed, Progress = 100 }
            }
        };

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job3", Name = "Job 3", Status = JobStatus.Pending, Progress = 0 },
                new() { Tag = "job4", Name = "Job 4", Status = JobStatus.InProgress, Progress = 25 }
            }
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.Equal(4, progress1.Jobs.Count);
        Assert.Contains(progress1.Jobs, j => j.Tag == "job1");
        Assert.Contains(progress1.Jobs, j => j.Tag == "job2");
        Assert.Contains(progress1.Jobs, j => j.Tag == "job3");
        Assert.Contains(progress1.Jobs, j => j.Tag == "job4");
    }

    [Fact]
    public void Merge_WithOverlappingJobs_ReplacesWithNewerJobs()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Old Job 1", Status = JobStatus.Pending, Progress = 0 },
                new() { Tag = "job2", Name = "Job 2", Status = JobStatus.Completed, Progress = 100 }
            }
        };

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "New Job 1", Status = JobStatus.InProgress, Progress = 75 }
            }
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.Equal(2, progress1.Jobs.Count);
        var updatedJob = progress1.Jobs.First(j => j.Tag == "job1");
        Assert.Equal("New Job 1", updatedJob.Name);
        Assert.Equal(JobStatus.InProgress, updatedJob.Status);
        Assert.Equal(75, updatedJob.Progress);
    }

    [Fact]
    public void Merge_WithNull_DoesNotModifyProgress()
    {
        // Arrange
        var progress = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Job 1", Status = JobStatus.InProgress, Progress = 50 }
            }
        };

        // Act
        progress.Merge(null!);

        // Assert
        Assert.Single(progress.Jobs);
        Assert.Equal("job1", progress.Jobs[0].Tag);
    }

    [Fact]
    public void Merge_RecalculatesTotalPercentage()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Job 1", Status = JobStatus.InProgress, Progress = 50 }
            }
        };
        progress1.RecalculateTotalPercentage();
        var initialPercentage = progress1.TotalPercentage;

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job2", Name = "Job 2", Status = JobStatus.Completed, Progress = 100 }
            }
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.NotEqual(initialPercentage, progress1.TotalPercentage);
        Assert.Equal(75, progress1.TotalPercentage); // Average of 50 and 100
    }

    [Fact]
    public void Merge_PreservesJobOrder_ForNonOverlappingJobs()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Job 1", Status = JobStatus.InProgress, Progress = 50 },
                new() { Tag = "job2", Name = "Job 2", Status = JobStatus.Pending, Progress = 0 },
                new() { Tag = "job3", Name = "Job 3", Status = JobStatus.Completed, Progress = 100 }
            }
        };

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job4", Name = "Job 4", Status = JobStatus.InProgress, Progress = 25 }
            }
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.Equal(4, progress1.Jobs.Count);
        Assert.Equal("job1", progress1.Jobs[0].Tag);
        Assert.Equal("job2", progress1.Jobs[1].Tag);
        Assert.Equal("job3", progress1.Jobs[2].Tag);
        Assert.Equal("job4", progress1.Jobs[3].Tag); // Appended at end
    }

    [Fact]
    public void Merge_PreservesPosition_WhenReplacingJob()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Job 1", Status = JobStatus.InProgress, Progress = 50 },
                new() { Tag = "job2", Name = "Old Job 2", Status = JobStatus.Pending, Progress = 0 },
                new() { Tag = "job3", Name = "Job 3", Status = JobStatus.Completed, Progress = 100 }
            }
        };

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job2", Name = "New Job 2", Status = JobStatus.InProgress, Progress = 75 }
            }
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.Equal(3, progress1.Jobs.Count);
        Assert.Equal("job1", progress1.Jobs[0].Tag);
        Assert.Equal("job2", progress1.Jobs[1].Tag); // Still in position 1
        Assert.Equal("New Job 2", progress1.Jobs[1].Name); // But updated
        Assert.Equal("job3", progress1.Jobs[2].Tag);
    }

    [Fact]
    public void Merge_WithMultipleOverlaps_ReplacesAllCorrectly()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Old Job 1", Status = JobStatus.Pending, Progress = 0 },
                new() { Tag = "job2", Name = "Old Job 2", Status = JobStatus.Pending, Progress = 0 },
                new() { Tag = "job3", Name = "Job 3", Status = JobStatus.Completed, Progress = 100 }
            }
        };

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "New Job 1", Status = JobStatus.InProgress, Progress = 50 },
                new() { Tag = "job2", Name = "New Job 2", Status = JobStatus.Completed, Progress = 100 },
                new() { Tag = "job4", Name = "Job 4", Status = JobStatus.Pending, Progress = 0 }
            }
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.Equal(4, progress1.Jobs.Count);
        Assert.Equal("New Job 1", progress1.Jobs[0].Name);
        Assert.Equal(JobStatus.InProgress, progress1.Jobs[0].Status);
        Assert.Equal("New Job 2", progress1.Jobs[1].Name);
        Assert.Equal(JobStatus.Completed, progress1.Jobs[1].Status);
        Assert.Equal("Job 3", progress1.Jobs[2].Name);
        Assert.Equal("Job 4", progress1.Jobs[3].Name);
    }

    [Fact]
    public void Merge_WithEmptyOtherProgress_DoesNotModify()
    {
        // Arrange
        var progress1 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>
            {
                new() { Tag = "job1", Name = "Job 1", Status = JobStatus.InProgress, Progress = 50 }
            }
        };

        var progress2 = new AnalysisProgress
        {
            Jobs = new List<AnalysisJob>()
        };

        // Act
        progress1.Merge(progress2);

        // Assert
        Assert.Single(progress1.Jobs);
        Assert.Equal("job1", progress1.Jobs[0].Tag);
    }
}

