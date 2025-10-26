namespace Mentor.Core.Configuration;

public class LLMConfiguration
{
    public string DefaultProvider { get; set; } = "openai";
    public OpenAIConfiguration OpenAI { get; set; } = new();
}

