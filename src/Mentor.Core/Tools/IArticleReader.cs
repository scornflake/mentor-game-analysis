using Mentor.Core.Data;

namespace Mentor.Core.Tools;

public interface IArticleReader
{
    Task<string> ReadArticleAsync(string url, CancellationToken cancellationToken = default);
    
    void Configure(ToolConfigurationEntity configuration);
}

