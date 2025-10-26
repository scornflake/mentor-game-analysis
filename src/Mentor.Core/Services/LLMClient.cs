using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.AI;

namespace Mentor.Core.Services;

public class LLMClient : ILLMClient
{
    public OpenAIConfiguration Configuration { get; }
    public IChatClient ChatClient { get; }

    public LLMClient(OpenAIConfiguration configuration, IChatClient chatClient)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ChatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }
}

