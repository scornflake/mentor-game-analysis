using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Services;

public class LLMClient : ILLMClient
{
    public ProviderConfigurationEntity Configuration { get; }
    public IChatClient ChatClient { get; }

    public LLMClient(ProviderConfigurationEntity configuration, IChatClient chatClient)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ChatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }
}

