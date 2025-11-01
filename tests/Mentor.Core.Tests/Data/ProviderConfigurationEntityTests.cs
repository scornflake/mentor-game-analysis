using Mentor.Core.Data;

namespace Mentor.Core.Tests.Data;

public class ProviderConfigurationEntityTests
{
    [Fact]
    public void UseGameRules_DefaultsToFalse()
    {
        // Arrange & Act
        var entity = new ProviderConfigurationEntity();

        // Assert
        Assert.False(entity.UseGameRules);
    }

    [Fact]
    public void UseGameRules_CanBeSetToTrue()
    {
        // Arrange
        var entity = new ProviderConfigurationEntity
        {
            UseGameRules = true
        };

        // Act & Assert
        Assert.True(entity.UseGameRules);
    }

    [Fact]
    public void UseGameRules_CanBeSetToFalse()
    {
        // Arrange
        var entity = new ProviderConfigurationEntity
        {
            UseGameRules = false
        };

        // Act & Assert
        Assert.False(entity.UseGameRules);
    }
}

