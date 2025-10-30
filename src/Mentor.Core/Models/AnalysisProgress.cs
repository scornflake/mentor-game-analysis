namespace Mentor.Core.Models;

public class AnalysisProgress
{
    public double TotalPercentage { get; set; }
    public List<AnalysisJob> Jobs { get; set; } = [];
}

