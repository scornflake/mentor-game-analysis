using Mentor.Core.Data;
using Mentor.Core.Models;

namespace Mentor.Core.Tools;

public interface IWebSearchTool
{
    Task<string> Search(string query, SearchOutputFormat format, int maxResults = 5);

    void Configure(RealWebtoolToolConfiguration configuration);
}

public class KnownSearchTools
{
    public const string Brave = "brave";
}