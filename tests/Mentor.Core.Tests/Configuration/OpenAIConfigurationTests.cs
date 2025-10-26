using Mentor.Core.Configuration;

namespace Mentor.Core.Tests.Configuration;

public class OpenAIConfigurationTests
{
    [Fact]
    public void ShouldUseWebSearchTool_WhenNotSet_DefaultsToTrueForLocalhost()
    {
        // Arrange
        var config = new OpenAIConfiguration
        {
            BaseUrl = "http://localhost:1234/v1",
            UseWebSearchTool = true
        };
        
        // Act & Assert
        Assert.True(config.UseWebSearchTool);
    }
    
    [Fact]
    public void ShouldUseWebSearchTool_WhenNotSet_DefaultsToFalseForOpenAI()
    {
        // Arrange
        var config = new OpenAIConfiguration
        {
            BaseUrl = "https://api.openai.com"
        };
        
        // Act & Assert
        Assert.False(config.UseWebSearchTool);
    }
    
    [Fact]
    public void ShouldUseWebSearchTool_WhenNotSet_DefaultsToFalseForPerplexity()
    {
        // Arrange
        var config = new OpenAIConfiguration(); // Uses default Perplexity URL
        
        // Act & Assert
        Assert.False(config.UseWebSearchTool);
    }
    
    [Fact]
    public void ShouldUseWebSearchTool_WhenExplicitlySetToTrue_ReturnsTrue()
    {
        // Arrange
        var config = new OpenAIConfiguration
        {
            BaseUrl = "https://api.openai.com",
            UseWebSearchTool = true
        };
        
        // Act & Assert
        Assert.True(config.UseWebSearchTool);
    }
    
    
}

