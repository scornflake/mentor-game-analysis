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
}

