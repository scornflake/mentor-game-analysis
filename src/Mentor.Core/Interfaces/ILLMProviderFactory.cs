using Mentor.Core.Data;
using Mentor.Core.Tools;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Interfaces;

public interface ILLMClient
{
    public ProviderConfigurationEntity Configuration { get; }
    public IChatClient ChatClient { get; }
}

public interface ILLMProviderFactory
{
    /// <summary>
    /// Gets an LLM provider client 
    /// </summary>
    ILLMClient GetProvider(ProviderConfigurationEntity config);
    
    IAnalysisService GetAnalysisService(ILLMClient client);
}