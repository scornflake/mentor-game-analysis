namespace Mentor.Core.Configuration;

public class LLMConfiguration
{
    public string DefaultProvider { get; set; } = "perplexity";
    public Dictionary<string, OpenAIConfiguration> Providers { get; set; } = new();
}

