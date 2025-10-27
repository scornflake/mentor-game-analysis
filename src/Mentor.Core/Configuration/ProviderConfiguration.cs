namespace Mentor.Core.Configuration;

public class ProviderConfiguration
{
    public string ProviderType { get; set; } = string.Empty; // "openai", "perplexity"
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60;
}

