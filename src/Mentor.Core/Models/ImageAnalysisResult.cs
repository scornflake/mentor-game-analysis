namespace Mentor.Core.Models;

public class ImageAnalysisResult
{
    /// <summary>
    /// Detailed text description of what the image represents
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Probability (0.0 to 1.0) that the image is about the specified game
    /// </summary>
    public double GameRelevanceProbability { get; set; }
    
    /// <summary>
    /// Timestamp when the analysis was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }
    
    /// <summary>
    /// Name of the LLM provider used for analysis
    /// </summary>
    public string ProviderUsed { get; set; } = string.Empty;
}

