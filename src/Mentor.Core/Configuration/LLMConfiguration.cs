namespace Mentor.Core.Configuration;

public class LLMConfiguration
{
    public string DefaultProvider { get; set; } = "openai";
    public Dictionary<string, OpenAIConfiguration> Providers { get; set; } = new();
}

