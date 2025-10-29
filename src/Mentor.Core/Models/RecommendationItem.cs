namespace Mentor.Core.Models;

public class RecommendationItem
{
    public Priority Priority { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    
    public string ReferenceLink { get; set; } = string.Empty;
    
    public bool HasReferenceLink => !string.IsNullOrWhiteSpace(ReferenceLink) && ReferenceLink.StartsWith("https", StringComparison.OrdinalIgnoreCase);
}

