using Mentor.Core.Configuration;
using Mentor.Core.Tools;
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
    /// Gets an LLM provider client 
    /// </summary>
    ILLMClient GetProvider(ProviderConfiguration config);
    
    IAnalysisService GetAnalysisService(ILLMClient client);
}