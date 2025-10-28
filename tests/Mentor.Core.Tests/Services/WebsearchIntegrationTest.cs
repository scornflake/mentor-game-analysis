using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public class WebsearchIntegrationTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebsearchIntegrationTest> _logger;
    private readonly ToolConfigurationEntity _config;

    public WebsearchIntegrationTest(ITestOutputHelper testOutputHelper)
    {
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        services.AddWebSearchTool();

        // Load configuration from appsettings
        var projectRoot = ApiKeyHelper.FindProjectRoot(AppContext.BaseDirectory);
        Assert.NotNull(projectRoot);
        var settingsPath = Path.Combine(projectRoot, "src", "Mentor.CLI");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(settingsPath)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Create configuration from settings
        var braveSection = configuration.GetSection("BraveSearch");
        Assert.NotNull(braveSection);
        
        var apiKey = braveSection["ApiKey"];
        Assert.False(string.IsNullOrEmpty(apiKey), "Brave API key is not configured");
        _config = new ToolConfigurationEntity
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.search.brave.com/res/v1",
            Timeout = 30
        };

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<WebsearchIntegrationTest>>();

        // API only does 1 fps
        Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task DoWebSearch_Snippets()
    {
        _logger.LogInformation("Brave API Key: {ApiKey}", _config.ApiKey);
        Assert.True(!string.IsNullOrEmpty(_config.ApiKey));

        _logger.LogInformation("Brave Search Config: ApiKey={ApiKey}, BaseUrl={BaseUrl}, Timeout={Timeout}", 
            _config.ApiKey, _config.BaseUrl, _config.Timeout);
        
        var websearch = _serviceProvider.GetRequiredService<IWebSearchTool>();
        websearch.Configure(_config);
        
        var results = await websearch.Search("What is the capital of France?", SearchOutputFormat.Snippets);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }
    
    [Fact]
    public async Task DoWebSearch_Summary()
    {
        _logger.LogInformation("Brave API Key: {ApiKey}", _config.ApiKey);
        Assert.True(!string.IsNullOrEmpty(_config.ApiKey));

        _logger.LogInformation("Brave Search Config: ApiKey={ApiKey}, BaseUrl={BaseUrl}, Timeout={Timeout}", 
            _config.ApiKey, _config.BaseUrl, _config.Timeout);
        
        var websearch = _serviceProvider.GetRequiredService<IWebSearchTool>();
        websearch.Configure(_config);
        
        var results = await websearch.Search("What is the capital of France?", SearchOutputFormat.Summary);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }

    [Fact]
    public async Task DoWebSearch_Structured()
    {
        _logger.LogInformation("Brave API Key: {ApiKey}", _config.ApiKey);
        Assert.True(!string.IsNullOrEmpty(_config.ApiKey));

        _logger.LogInformation("Brave Search Config: ApiKey={ApiKey}, BaseUrl={BaseUrl}, Timeout={Timeout}", 
            _config.ApiKey, _config.BaseUrl, _config.Timeout);
        
        var websearch = _serviceProvider.GetRequiredService<IWebSearchTool>();
        websearch.Configure(_config);
        
        var results = await websearch.SearchStructured("What is the capital of New Zealand?", maxResults: 1);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Contains("Wellington", results[0].Description);
    }
}
