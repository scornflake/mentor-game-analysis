using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tests.Helpers;
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

    public WebsearchIntegrationTest(ITestOutputHelper testOutputHelper)
    {
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        services.AddWebSearchTool();

        // need to configure service with IOptions<BraveSearchConfiguration> config
        var projectRoot = ApiKeyHelper.FindProjectRoot(AppContext.BaseDirectory);
        Assert.NotNull(projectRoot);
        var settingsPath = Path.Combine(projectRoot, "src", "Mentor.CLI");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(settingsPath)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        services.Configure<BraveSearchConfiguration>(configuration.GetSection("BraveSearch"));

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<WebsearchIntegrationTest>>();

        // API only does 1 fps
        Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task DoWebSearch_Snippets()
    {
        var config = _serviceProvider.GetRequiredService<IOptions<BraveSearchConfiguration>>();
        // dump the key/value pairs
        _logger.LogInformation("Brave API Key: {ApiKey}", config.Value.ApiKey);
        Assert.True(!string.IsNullOrEmpty(config.Value.ApiKey));

        _logger.LogInformation("Brave Search Config: {Config}", config.Value);
        var websearch = _serviceProvider.GetRequiredService<IWebsearch>();
        var results = await websearch.Search("What is the capital of France?", SearchOutputFormat.Snippets);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }
    
    [Fact]
    public async Task DoWebSearch_Summary()
    {
        var config = _serviceProvider.GetRequiredService<IOptions<BraveSearchConfiguration>>();
        // dump the key/value pairs
        _logger.LogInformation("Brave API Key: {ApiKey}", config.Value.ApiKey);
        Assert.True(!string.IsNullOrEmpty(config.Value.ApiKey));

        _logger.LogInformation("Brave Search Config: {Config}", config.Value);
        var websearch = _serviceProvider.GetRequiredService<IWebsearch>();
        var results = await websearch.Search("What is the capital of France?", SearchOutputFormat.Summary);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }

    [Fact]
    public async Task DoWebSearch_Structured()
    {
        var config = _serviceProvider.GetRequiredService<IOptions<BraveSearchConfiguration>>();
        // dump the key/value pairs
        _logger.LogInformation("Brave API Key: {ApiKey}", config.Value.ApiKey);
        Assert.True(!string.IsNullOrEmpty(config.Value.ApiKey));

        _logger.LogInformation("Brave Search Config: {Config}", config.Value);
        var websearch = _serviceProvider.GetRequiredService<IWebsearch>();
        var results = await websearch.Search("What is the capital of New Zealand?", SearchOutputFormat.Structured);
        _logger.LogInformation("Web search results: {Results}", results);
        Assert.NotNull(results);
    }
}