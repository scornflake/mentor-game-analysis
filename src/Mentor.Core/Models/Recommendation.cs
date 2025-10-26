namespace Mentor.Core.Models;

public class Recommendation
{
    public string Analysis { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<RecommendationItem> Recommendations { get; set; } = [];
    public double Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string ProviderUsed { get; set; } = string.Empty;
}

