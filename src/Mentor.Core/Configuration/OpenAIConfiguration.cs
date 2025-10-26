namespace Mentor.Core.Configuration;

public class OpenAIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "sonar";
    public string BaseUrl { get; set; } = "https://api.perplexity.ai";
    public int Timeout { get; set; } = 60;
    
    /// <summary>
    /// Indicates whether to use web search tool with this provider.
    /// If null, defaults based on BaseUrl: true for localhost, false otherwise.
    /// </summary>
    public bool UseWebSearchTool { get; set; } = false;
    
    /// <summary>
    /// Gets whether an API key is required for this provider.
    /// In general this is true if ApiKey is set and not empty.
    /// </summary>
    public bool IsApiKeyPresent => !string.IsNullOrEmpty(ApiKey);
}

