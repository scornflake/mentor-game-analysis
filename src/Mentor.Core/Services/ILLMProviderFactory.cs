using OpenAI.Chat;

namespace Mentor.Core.Services;

public interface ILLMProviderFactory
{
    /// <summary>
    /// Gets an LLM provider client for the specified provider name
    /// </summary>
    ChatClient GetProvider(string providerName);
}

