using Mentor.Core.Configuration;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Interfaces;

public interface ILLMClient
{
    public OpenAIConfiguration Configuration { get; }
    public IChatClient ChatClient { get; }
}

public interface ILLMProviderFactory
{
    /// <summary>
    /// Gets an LLM provider client for the specified provider name
    /// </summary>
    ILLMClient GetProvider(string providerName);
}