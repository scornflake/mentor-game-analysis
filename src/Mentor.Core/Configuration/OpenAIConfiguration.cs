namespace Mentor.Core.Configuration;

public class OpenAIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "sonar";
    public string BaseUrl { get; set; } = "https://api.perplexity.ai";
    public int Timeout { get; set; } = 60;
    public bool IsLocal { get; set; } = false;
}

