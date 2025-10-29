using Mentor.Core.Data;
using Mentor.Core.Models;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public class WebsearchIntegrationTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebsearchIntegrationTest> _logger;
    private readonly ToolConfigurationEntity? _config;

    public WebsearchIntegrationTest(ITestOutputHelper testOutputHelper)
    {
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        services.AddWebSearchTool();

        // Load configuration from multiple sources (priority: User Secrets > appsettings.Development.json > Environment Variables)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<WebsearchIntegrationTest>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Create configuration from settings
        var braveSection = configuration.GetSection("BraveSearch");
        var apiKey = braveSection["ApiKey"];
        
        // Only create config if API key is available
        if (!string.IsNullOrEmpty(apiKey))
        {
            _config = new ToolConfigurationEntity
            {
                ApiKey = apiKey,
                BaseUrl = braveSection["BaseUrl"] ?? "https://api.search.brave.com/res/v1",
                Timeout = int.TryParse(braveSection["Timeout"], out var timeout) ? timeout : 30
            };
        }

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<WebsearchIntegrationTest>>();
    }

    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task DoWebSearch_Snippets()
    {
        // Skip if no config
        if (_config == null)
        {
            _logger.LogWarning("Skipping test - no API key configured");
            return;
        }

        _logger.LogInformation("Brave Search Config: ApiKey={ApiKey}, BaseUrl={BaseUrl}, Timeout={Timeout}", 
            _config.ApiKey, _config.BaseUrl, _config.Timeout);
        
        var websearch = _serviceProvider.GetRequiredService<IWebSearchTool>();
        websearch.Configure(_config);
        
        var results = await websearch.Search(SearchContext.Create("What is the capital of France?"), SearchOutputFormat.Snippets);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }
    
    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task DoWebSearch_Summary()
    {
        // Skip if no config
        if (_config == null)
        {
            _logger.LogWarning("Skipping test - no API key configured");
            return;
        }

        _logger.LogInformation("Brave Search Config: ApiKey={ApiKey}, BaseUrl={BaseUrl}, Timeout={Timeout}", 
            _config.ApiKey, _config.BaseUrl, _config.Timeout);
        
        var websearch = _serviceProvider.GetRequiredService<IWebSearchTool>();
        websearch.Configure(_config);
        
        var results = await websearch.Search(SearchContext.Create("What is the capital of France?"), SearchOutputFormat.Summary);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }

    [ConditionalFact("BRAVE_SEARCH_API_KEY")]
    public async Task DoWebSearch_Structured()
    {
        // Skip if no config
        if (_config == null)
        {
            _logger.LogWarning("Skipping test - no API key configured");
            return;
        }

        _logger.LogInformation("Brave Search Config: ApiKey={ApiKey}, BaseUrl={BaseUrl}, Timeout={Timeout}", 
            _config.ApiKey, _config.BaseUrl, _config.Timeout);
        
        var websearch = _serviceProvider.GetRequiredService<IWebSearchTool>();
        websearch.Configure(_config);
        
        var results = await websearch.SearchStructured(SearchContext.Create("What is the capital of New Zealand?"), maxResults: 1);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Contains("Wellington", results[0].Description);
    }
}
