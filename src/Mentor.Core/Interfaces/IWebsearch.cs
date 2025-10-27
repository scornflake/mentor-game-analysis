using Mentor.Core.Models;

namespace Mentor.Core.Interfaces;

public interface IWebsearch
{
    Task<string> Search(string query, SearchOutputFormat format, int maxResults = 5);
}