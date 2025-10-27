using Mentor.Core.Configuration;
using Mentor.Core.Data;
using Mentor.Core.Services;

namespace Mentor.Core.Tests.Services;

public class RealmConfigurationRepositoryTests : IDisposable
{
    private readonly string _testDatabasePath;
    private readonly RealmConfigurationRepository _repository;

    public RealmConfigurationRepositoryTests()
    {
        // Use a unique database file for each test
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.realm");
        _repository = new RealmConfigurationRepository(_testDatabasePath);
    }

    [Fact]
    public async Task SeedDefaultsAsync_WhenDatabaseIsEmpty_SeedsDefaultProviders()
    {
        // Act
        await _repository.SeedDefaultsAsync();
        var allProviders = await _repository.GetAllProvidersAsync();

        // Assert
        Assert.NotEmpty(allProviders);
        Assert.Equal(2, allProviders.Count);
        
        var perplexity = allProviders.FirstOrDefault(p => p.ProviderType == "perplexity");
        Assert.NotNull(perplexity);
        Assert.Equal("sonar", perplexity.Model);
        Assert.Equal("https://api.perplexity.ai", perplexity.BaseUrl);
        
        var local = allProviders.FirstOrDefault(p => p.BaseUrl == "http://localhost:1234");
        Assert.NotNull(local);
        Assert.Equal("openai", local.ProviderType);
        Assert.Equal("google/gemma-3-27b", local.Model);
    }

    [Fact]
    public async Task SeedDefaultsAsync_WhenDatabaseHasData_DoesNotSeedAgain()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var initialCount = (await _repository.GetAllProvidersAsync()).Count;

        // Act
        await _repository.SeedDefaultsAsync();
        var finalCount = (await _repository.GetAllProvidersAsync()).Count;

        // Assert
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public async Task GetActiveProviderAsync_WhenSeeded_ReturnsActiveProvider()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();

        // Act
        var activeProvider = await _repository.GetActiveProviderAsync();

        // Assert
        Assert.NotNull(activeProvider);
        Assert.Equal("http://localhost:1234", activeProvider.BaseUrl);
    }

    [Fact]
    public async Task SaveProviderAsync_WithNewProvider_CreatesProvider()
    {
        // Arrange
        var config = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        // Act
        await _repository.SaveProviderAsync("OpenAI", config);
        var savedProvider = await _repository.GetProviderByNameAsync("OpenAI");

        // Assert
        Assert.NotNull(savedProvider);
        Assert.Equal("openai", savedProvider.ProviderType);
        Assert.Equal("test-key", savedProvider.ApiKey);
        Assert.Equal("gpt-4o", savedProvider.Model);
    }

    [Fact]
    public async Task SaveProviderAsync_WithExistingProvider_UpdatesProvider()
    {
        // Arrange
        var initialConfig = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "initial-key",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };
        await _repository.SaveProviderAsync("OpenAI", initialConfig);

        var updatedConfig = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "updated-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 90
        };

        // Act
        await _repository.SaveProviderAsync("OpenAI", updatedConfig);
        var savedProvider = await _repository.GetProviderByNameAsync("OpenAI");

        // Assert
        Assert.NotNull(savedProvider);
        Assert.Equal("updated-key", savedProvider.ApiKey);
        Assert.Equal("gpt-4o", savedProvider.Model);
        Assert.Equal(90, savedProvider.Timeout);
    }

    [Fact]
    public async Task SetActiveProviderAsync_ChangesActiveProvider()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1"
        };
        await _repository.SaveProviderAsync("OpenAI", config);

        // Act
        await _repository.SetActiveProviderAsync("OpenAI");
        var activeProvider = await _repository.GetActiveProviderAsync();

        // Assert
        Assert.NotNull(activeProvider);
        Assert.Equal("gpt-4o", activeProvider.Model);
        Assert.Equal("https://api.openai.com/v1", activeProvider.BaseUrl);
    }

    [Fact]
    public async Task SetActiveProviderAsync_WithInvalidName_ThrowsArgumentException()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _repository.SetActiveProviderAsync("NonExistent"));
    }

    [Fact]
    public async Task DeleteProviderAsync_RemovesProvider()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1"
        };
        await _repository.SaveProviderAsync("OpenAI", config);

        // Act
        await _repository.DeleteProviderAsync("OpenAI");
        var deletedProvider = await _repository.GetProviderByNameAsync("OpenAI");

        // Assert
        Assert.Null(deletedProvider);
    }

    [Fact]
    public async Task GetAllProvidersAsync_ReturnsAllProviders()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new ProviderConfiguration
        {
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1"
        };
        await _repository.SaveProviderAsync("OpenAI", config);

        // Act
        var allProviders = await _repository.GetAllProvidersAsync();

        // Assert
        Assert.Equal(3, allProviders.Count);
    }

    [Fact]
    public async Task SeedDefaultsAsync_WhenDatabaseIsEmpty_SeedsDefaultTools()
    {
        // Act
        await _repository.SeedDefaultsAsync();
        var allTools = await _repository.GetAllToolsAsync();

        // Assert
        Assert.NotEmpty(allTools);
        Assert.Single(allTools);
        
        var braveTool = allTools.FirstOrDefault(t => t.ToolName == "Brave");
        Assert.NotNull(braveTool);
        Assert.Equal("https://api.search.brave.com/res/v1/web/search", braveTool.BaseUrl);
        Assert.Equal(30, braveTool.Timeout);
    }

    [Fact]
    public async Task SaveToolAsync_WithNewTool_CreatesTool()
    {
        // Arrange
        var config = new RealWebtoolToolConfiguration
        {
            ToolName = "TestSearch",
            ApiKey = "test-api-key",
            BaseUrl = "https://api.test.com/search",
            Timeout = 45
        };

        // Act
        await _repository.SaveToolAsync("TestSearch", config);
        var savedTool = await _repository.GetToolByNameAsync("TestSearch");

        // Assert
        Assert.NotNull(savedTool);
        Assert.Equal("TestSearch", savedTool.ToolName);
        Assert.Equal("test-api-key", savedTool.ApiKey);
        Assert.Equal("https://api.test.com/search", savedTool.BaseUrl);
        Assert.Equal(45, savedTool.Timeout);
    }

    [Fact]
    public async Task SaveToolAsync_WithExistingTool_UpdatesTool()
    {
        // Arrange
        var initialConfig = new RealWebtoolToolConfiguration
        {
            ToolName = "TestSearch",
            ApiKey = "initial-key",
            BaseUrl = "https://api.initial.com",
            Timeout = 30
        };
        await _repository.SaveToolAsync("TestSearch", initialConfig);

        var updatedConfig = new RealWebtoolToolConfiguration
        {
            ToolName = "TestSearch",
            ApiKey = "updated-key",
            BaseUrl = "https://api.updated.com",
            Timeout = 60
        };

        // Act
        await _repository.SaveToolAsync("TestSearch", updatedConfig);
        var savedTool = await _repository.GetToolByNameAsync("TestSearch");

        // Assert
        Assert.NotNull(savedTool);
        Assert.Equal("updated-key", savedTool.ApiKey);
        Assert.Equal("https://api.updated.com", savedTool.BaseUrl);
        Assert.Equal(60, savedTool.Timeout);
    }

    [Fact]
    public async Task DeleteToolAsync_RemovesTool()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new RealWebtoolToolConfiguration
        {
            ToolName = "TestSearch",
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };
        await _repository.SaveToolAsync("TestSearch", config);

        // Act
        await _repository.DeleteToolAsync("TestSearch");
        var deletedTool = await _repository.GetToolByNameAsync("TestSearch");

        // Assert
        Assert.Null(deletedTool);
    }

    [Fact]
    public async Task GetAllToolsAsync_ReturnsAllTools()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new RealWebtoolToolConfiguration
        {
            ToolName = "TestSearch",
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };
        await _repository.SaveToolAsync("TestSearch", config);

        // Act
        var allTools = await _repository.GetAllToolsAsync();

        // Assert
        Assert.Equal(2, allTools.Count);
    }

    [Fact]
    public async Task GetToolByNameAsync_WithExistingTool_ReturnsTool()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();

        // Act
        var tool = await _repository.GetToolByNameAsync("Brave");

        // Assert
        Assert.NotNull(tool);
        Assert.Equal("Brave", tool.ToolName);
    }

    [Fact]
    public async Task GetToolByNameAsync_WithNonExistingTool_ReturnsNull()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();

        // Act
        var tool = await _repository.GetToolByNameAsync("NonExistent");

        // Assert
        Assert.Null(tool);
    }

    public void Dispose()
    {
        _repository?.Dispose();
        
        // Clean up test database file
        if (File.Exists(_testDatabasePath))
        {
            try
            {
                File.Delete(_testDatabasePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

