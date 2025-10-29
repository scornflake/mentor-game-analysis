using Mentor.Core.Data;
using Mentor.Core.Services;
using Xunit;

namespace Mentor.Core.Tests.Services;

public class UIStateRepositoryTests : IDisposable
{
    private readonly ConfigurationRepository _repository;
    private readonly string _testDbPath;

    public UIStateRepositoryTests()
    {
        // Use a unique test database for each test instance
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_mentor_{Guid.NewGuid()}.db");
        _repository = new ConfigurationRepository(_testDbPath);
    }

    [Fact]
    public async Task GetUIStateAsync_WhenNoStateExists_ReturnsEmptyValues()
    {
        // Act
        var state = await _repository.GetUIStateAsync();

        // Assert
        Assert.Null(state.LastImagePath);
        Assert.Null(state.LastPrompt);
        Assert.Null(state.LastProvider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WhenCalledWithValues_SavesSuccessfully()
    {
        // Arrange
        const string imagePath = "/path/to/image.png";
        const string prompt = "Test prompt";
        const string provider = "Test Provider";

        // Act
        var entity = new UIStateEntity
        {
            LastImagePath = imagePath,
            LastPrompt = prompt,
            LastProvider = provider
        };
        await _repository.SaveUIStateAsync(entity);

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(imagePath, state.LastImagePath);
        Assert.Equal(prompt, state.LastPrompt);
        Assert.Equal(provider, state.LastProvider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WhenCalledTwice_UpdatesExistingState()
    {
        // Arrange
        const string initialImagePath = "/path/to/image1.png";
        const string initialPrompt = "Initial prompt";
        const string initialProvider = "Initial Provider";

        const string updatedImagePath = "/path/to/image2.png";
        const string updatedPrompt = "Updated prompt";
        const string updatedProvider = "Updated Provider";

        // Act
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = initialImagePath,
            LastPrompt = initialPrompt,
            LastProvider = initialProvider
        });
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = updatedImagePath,
            LastPrompt = updatedPrompt,
            LastProvider = updatedProvider
        });

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(updatedImagePath, state.LastImagePath);
        Assert.Equal(updatedPrompt, state.LastPrompt);
        Assert.Equal(updatedProvider, state.LastProvider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WithNullValues_SavesNullsSuccessfully()
    {
        // Arrange
        const string imagePath = "/path/to/image.png";
        const string prompt = "Test prompt";
        const string provider = "Test Provider";

        // Act - First save with values
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = imagePath,
            LastPrompt = prompt,
            LastProvider = provider
        });
        
        // Act - Then save with nulls
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = null,
            LastPrompt = null,
            LastProvider = null
        });

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Null(state.LastImagePath);
        Assert.Null(state.LastPrompt);
        Assert.Null(state.LastProvider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WithEmptyStrings_SavesEmptyStringsSuccessfully()
    {
        // Arrange
        const string imagePath = "a";
        const string prompt = "a";
        const string provider = "a";

        // Act
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = imagePath,
            LastPrompt = prompt,
            LastProvider = provider
        });

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(imagePath, state.LastImagePath);
        Assert.Equal(prompt, state.LastPrompt);
        Assert.Equal(provider, state.LastProvider);
    }

    [Fact]
    public async Task SaveUIStateAsync_PartialUpdate_UpdatesOnlyProvidedValues()
    {
        // Arrange
        const string initialImagePath = "/path/to/image1.png";
        const string initialPrompt = "Initial prompt";
        const string initialProvider = "Initial Provider";

        const string updatedPrompt = "Updated prompt only";

        // Act
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = initialImagePath,
            LastPrompt = initialPrompt,
            LastProvider = initialProvider
        });
        await _repository.SaveUIStateAsync(new UIStateEntity
        {
            LastImagePath = initialImagePath,
            LastPrompt = updatedPrompt,
            LastProvider = initialProvider
        });

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(initialImagePath, state.LastImagePath);
        Assert.Equal(updatedPrompt, state.LastPrompt);
        Assert.Equal(initialProvider, state.LastProvider);
    }

    public void Dispose()
    {
        _repository?.Dispose();

        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

