namespace Mentor.Core.Configuration;

public class BraveSearchConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.search.brave.com/res/v1";
    public int Timeout { get; set; } = 30;
}

