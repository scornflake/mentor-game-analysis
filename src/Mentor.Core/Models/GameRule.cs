namespace Mentor.Core.Models;

public class GameRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; } = 0.0;
}

