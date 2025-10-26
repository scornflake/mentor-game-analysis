using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Moq;

namespace Mentor.Core.Tests.Services;

public class AnalysisServiceTests
{
    [Fact]
    public void AnalysisService_CanBeConstructed_WithChatClient()
    {
        // Arrange - Create a mock IChatClient
        var mockChatClient = new Mock<IChatClient>();

        // Act
        var service = new AnalysisService(mockChatClient.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNullImageData_ThrowsArgumentException()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var service = new AnalysisService(mockChatClient.Object);
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
        var mockChatClient = new Mock<IChatClient>();
        var service = new AnalysisService(mockChatClient.Object);
        var request = new AnalysisRequest
        {
            ImageData = [],
            Prompt = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeAsync(request));
    }
}

