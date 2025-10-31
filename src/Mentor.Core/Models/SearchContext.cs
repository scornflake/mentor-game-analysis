using System.Security.Cryptography.X509Certificates;

namespace Mentor.Core.Models;

public class SearchContext
{
    public string Query { get; set; } = string.Empty;
    public string? GameName { get; set; }

    public static SearchContext Create(string query, string? gameName = null)
    {
        return new SearchContext
        {
            Query = query,
            GameName = gameName
        };

    }
    public int GetNumberOfWords()
    {
        return Query.GetNumberOfWords();
    }
}

public static class SearchContextExtensions
{
    public static int GetNumberOfWords(this string context)
    {
        return context.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
