namespace Mentor.Core.Models;

public class RecommendationItem
{
    public Priority Priority { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}

