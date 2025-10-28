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
        Assert.Null(state.ImagePath);
        Assert.Null(state.Prompt);
        Assert.Null(state.Provider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WhenCalledWithValues_SavesSuccessfully()
    {
        // Arrange
        const string imagePath = "/path/to/image.png";
        const string prompt = "Test prompt";
        const string provider = "Test Provider";

        // Act
        await _repository.SaveUIStateAsync(imagePath, prompt, provider);

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(imagePath, state.ImagePath);
        Assert.Equal(prompt, state.Prompt);
        Assert.Equal(provider, state.Provider);
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
        await _repository.SaveUIStateAsync(initialImagePath, initialPrompt, initialProvider);
        await _repository.SaveUIStateAsync(updatedImagePath, updatedPrompt, updatedProvider);

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(updatedImagePath, state.ImagePath);
        Assert.Equal(updatedPrompt, state.Prompt);
        Assert.Equal(updatedProvider, state.Provider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WithNullValues_SavesNullsSuccessfully()
    {
        // Arrange
        const string imagePath = "/path/to/image.png";
        const string prompt = "Test prompt";
        const string provider = "Test Provider";

        // Act - First save with values
        await _repository.SaveUIStateAsync(imagePath, prompt, provider);
        
        // Act - Then save with nulls
        await _repository.SaveUIStateAsync(null, null, null);

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Null(state.ImagePath);
        Assert.Null(state.Prompt);
        Assert.Null(state.Provider);
    }

    [Fact]
    public async Task SaveUIStateAsync_WithEmptyStrings_SavesEmptyStringsSuccessfully()
    {
        // Arrange
        const string imagePath = "a";
        const string prompt = "a";
        const string provider = "a";

        // Act
        await _repository.SaveUIStateAsync(imagePath, prompt, provider);

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(imagePath, state.ImagePath);
        Assert.Equal(prompt, state.Prompt);
        Assert.Equal(provider, state.Provider);
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
        await _repository.SaveUIStateAsync(initialImagePath, initialPrompt, initialProvider);
        await _repository.SaveUIStateAsync(initialImagePath, updatedPrompt, initialProvider);

        // Assert
        var state = await _repository.GetUIStateAsync();
        Assert.Equal(initialImagePath, state.ImagePath);
        Assert.Equal(updatedPrompt, state.Prompt);
        Assert.Equal(initialProvider, state.Provider);
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

