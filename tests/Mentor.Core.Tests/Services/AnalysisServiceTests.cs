using Mentor.Core.Models;
using Mentor.Core.Services;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Mentor.Core.Tests.Services;

public class AnalysisServiceTests
{
    [Fact]
    public void AnalysisService_CanBeConstructed_WithChatClient()
    {
        // Arrange - Create a ChatClient (won't be used, just testing construction)
        var credential = new ApiKeyCredential("test-key");
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.perplexity.ai")
        };
        var chatClient = new ChatClient("sonar", credential, options);

        // Act
        var service = new AnalysisService(chatClient);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNullImageData_ThrowsArgumentException()
    {
        // Arrange
        var credential = new ApiKeyCredential("test-key");
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.perplexity.ai")
        };
        var chatClient = new ChatClient("sonar", credential, options);
        var service = new AnalysisService(chatClient);
        var request = new AnalysisRequest
        {
            ImageData = null!,
            Prompt = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeAsync(request));
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyImageData_ThrowsArgumentException()
    {
        // Arrange
        var credential = new ApiKeyCredential("test-key");
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.perplexity.ai")
        };
        var chatClient = new ChatClient("sonar", credential, options);
        var service = new AnalysisService(chatClient);
        var request = new AnalysisRequest
        {
            ImageData = Array.Empty<byte>(),
            Prompt = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeAsync(request));
    }
}

