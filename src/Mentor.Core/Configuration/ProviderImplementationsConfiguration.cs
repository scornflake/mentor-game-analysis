namespace Mentor.Core.Configuration;

public class ProviderImplementationDetails
{
    public string DefaultBaseUrl { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
}

public class ProviderImplementationsConfiguration
{
    public Dictionary<string, ProviderImplementationDetails> ProviderImplementations { get; set; } = new();
}

