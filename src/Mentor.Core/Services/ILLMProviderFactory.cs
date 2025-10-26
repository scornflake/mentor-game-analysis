using Microsoft.Extensions.AI;

namespace Mentor.Core.Services;

public interface ILLMProviderFactory
{
    /// <summary>
    /// Gets an LLM provider client for the specified provider name
    /// </summary>
    IChatClient GetProvider(string providerName);
}

