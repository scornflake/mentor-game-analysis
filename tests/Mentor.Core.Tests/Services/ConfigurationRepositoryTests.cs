using Mentor.Core.Data;
using Mentor.Core.Services;

namespace Mentor.Core.Tests.Services;

public class ConfigurationRepositoryTests : IDisposable
{
    private readonly string _testDatabasePath;
    private readonly ConfigurationRepository _repository;

    public ConfigurationRepositoryTests()
    {
        // Use a unique database file for each test
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _repository = new ConfigurationRepository(_testDatabasePath);
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
        
        var local = allProviders.FirstOrDefault(p => p.BaseUrl == "http://localhost:1234/v1");
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
    public async Task SaveProviderAsync_WithNewProvider_CreatesProvider()
    {
        // Arrange
        var config = new ProviderConfigurationEntity
        {
            Name = "OpenAI",
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        // Act
        await _repository.SaveProviderAsync(config);
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
        var initialConfig = new ProviderConfigurationEntity
        {
            Name = "OpenAI",
            ProviderType = "openai",
            ApiKey = "initial-key",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };
        await _repository.SaveProviderAsync(initialConfig);
        var saved = await _repository.GetProviderByNameAsync("OpenAI");

        var updatedConfig = new ProviderConfigurationEntity
        {
            Id = saved!.Id,
            Name = "OpenAI",
            ProviderType = "openai",
            ApiKey = "updated-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 90
        };

        // Act
        await _repository.SaveProviderAsync(updatedConfig);
        var savedProvider = await _repository.GetProviderByNameAsync("OpenAI");

        // Assert
        Assert.NotNull(savedProvider);
        Assert.Equal("updated-key", savedProvider.ApiKey);
        Assert.Equal("gpt-4o", savedProvider.Model);
        Assert.Equal(90, savedProvider.Timeout);
    }

    [Fact]
    public async Task SaveProviderAsync_WithSameName_UpdatesExisting()
    {
        // Arrange
        var config1 = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            ApiKey = "test-key-1",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };
        await _repository.SaveProviderAsync(config1);
        var saved = await _repository.GetProviderByNameAsync("TestProvider");

        var config2 = new ProviderConfigurationEntity
        {
            Id = saved!.Id,
            Name = "TestProvider",
            ProviderType = "perplexity",
            ApiKey = "test-key-2",
            Model = "sonar",
            BaseUrl = "https://api.perplexity.ai",
            Timeout = 90
        };

        // Act - Save with same name should update, not create duplicate
        await _repository.SaveProviderAsync(config2);
        var allProviders = await _repository.GetAllProvidersAsync();
        var savedProvider = await _repository.GetProviderByNameAsync("TestProvider");

        // Assert - Should have updated the existing provider
        Assert.NotNull(savedProvider);
        Assert.Equal("perplexity", savedProvider.ProviderType);
        Assert.Equal("test-key-2", savedProvider.ApiKey);
        Assert.Equal("sonar", savedProvider.Model);
        Assert.Equal(90, savedProvider.Timeout);
    }

    [Fact]
    public async Task SaveProviderAsync_UpdateWithSameName_Succeeds()
    {
        // Arrange
        var config1 = new ProviderConfigurationEntity
        {
            Name = "TestProvider",
            ProviderType = "openai",
            ApiKey = "test-key-1",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };
        await _repository.SaveProviderAsync(config1);
        var saved = await _repository.GetProviderByNameAsync("TestProvider");

        var config2 = new ProviderConfigurationEntity
        {
            Id = saved!.Id,
            Name = "TestProvider",
            ProviderType = "openai",
            ApiKey = "updated-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 90
        };

        // Act
        await _repository.SaveProviderAsync(config2);
        var savedProvider = await _repository.GetProviderByNameAsync("TestProvider");

        // Assert
        Assert.NotNull(savedProvider);
        Assert.Equal("updated-key", savedProvider.ApiKey);
        Assert.Equal("gpt-4o", savedProvider.Model);
    }

    [Fact]
    public async Task SaveProviderAsync_RenameToExistingName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config1 = new ProviderConfigurationEntity
        {
            Name = "Provider1",
            ProviderType = "openai",
            ApiKey = "test-key-1",
            Model = "gpt-4",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };
        await _repository.SaveProviderAsync(config1);

        var config2 = new ProviderConfigurationEntity
        {
            Name = "Provider2",
            ProviderType = "perplexity",
            ApiKey = "test-key-2",
            Model = "sonar",
            BaseUrl = "https://api.perplexity.ai",
            Timeout = 60
        };
        await _repository.SaveProviderAsync(config2);
        var saved2 = await _repository.GetProviderByNameAsync("Provider2");

        // Try to rename Provider2 to Provider1
        saved2!.Name = "Provider1";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.SaveProviderAsync(saved2));
    }

    [Fact]
    public async Task DeleteProviderAsync_RemovesProvider()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new ProviderConfigurationEntity
        {
            Name = "OpenAI",
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1"
        };
        await _repository.SaveProviderAsync(config);
        var saved = await _repository.GetProviderByNameAsync("OpenAI");

        // Act
        await _repository.DeleteProviderAsync(saved!.Id);
        var deletedProvider = await _repository.GetProviderByNameAsync("OpenAI");

        // Assert
        Assert.Null(deletedProvider);
    }

    [Fact]
    public async Task GetAllProvidersAsync_ReturnsAllProviders()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new ProviderConfigurationEntity
        {
            Name = "OpenAI",
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1"
        };
        await _repository.SaveProviderAsync(config);

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
        Assert.Equal(2, allTools.Count);
        
        var braveTool = allTools.FirstOrDefault(t => t.ToolName == "Brave");
        Assert.NotNull(braveTool);
        Assert.Equal("https://api.search.brave.com/res/v1/web/search", braveTool.BaseUrl);
        Assert.Equal(30, braveTool.Timeout);
    }

    [Fact]
    public async Task SaveToolAsync_WithNewTool_CreatesTool()
    {
        // Arrange
        var config = new ToolConfigurationEntity
        {
            ToolName = "TestSearch",
            ApiKey = "test-api-key",
            BaseUrl = "https://api.test.com/search",
            Timeout = 45
        };

        // Act
        await _repository.SaveToolAsync(config);
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
        var initialConfig = new ToolConfigurationEntity
        {
            ToolName = "TestSearch",
            ApiKey = "initial-key",
            BaseUrl = "https://api.initial.com",
            Timeout = 30
        };
        await _repository.SaveToolAsync(initialConfig);
        var saved = await _repository.GetToolByNameAsync("TestSearch");

        var updatedConfig = new ToolConfigurationEntity
        {
            Id = saved!.Id,
            ToolName = "TestSearch",
            ApiKey = "updated-key",
            BaseUrl = "https://api.updated.com",
            Timeout = 60
        };

        // Act
        await _repository.SaveToolAsync(updatedConfig);
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
        var config = new ToolConfigurationEntity
        {
            ToolName = "TestSearch",
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };
        await _repository.SaveToolAsync(config);
        var saved = await _repository.GetToolByNameAsync("TestSearch");

        // Act
        await _repository.DeleteToolAsync(saved!.Id);
        var deletedTool = await _repository.GetToolByNameAsync("TestSearch");

        // Assert
        Assert.Null(deletedTool);
    }

    [Fact]
    public async Task GetAllToolsAsync_ReturnsAllTools()
    {
        // Arrange
        await _repository.SeedDefaultsAsync();
        var config = new ToolConfigurationEntity
        {
            ToolName = "TestSearch",
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com",
            Timeout = 30
        };
        await _repository.SaveToolAsync(config);

        // Act
        var allTools = await _repository.GetAllToolsAsync();

        // Assert
        Assert.Equal(3, allTools.Count);
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

    [Fact]
    public async Task GetAvailableProviderTypesAsync_ReturnsExpectedTypes()
    {
        // Act
        var providerTypes = await _repository.GetAvailableProviderTypesAsync();

        // Assert
        Assert.NotEmpty(providerTypes);
        Assert.Contains("openai", providerTypes);
        Assert.Contains("perplexity", providerTypes);
        Assert.Equal(2, providerTypes.Count);
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

