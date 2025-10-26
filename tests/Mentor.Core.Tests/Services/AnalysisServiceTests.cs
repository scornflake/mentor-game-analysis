using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public class AnalysisServiceTests
{
    private readonly ServiceProvider _provider;

    public AnalysisServiceTests(ITestOutputHelper output)
    {
        var webSearch = new Mock<IWebsearch>();
        var mockChatClient = new Mock<IChatClient>();
        var mockLLMClient = new Mock<ILLMClient>();
        mockLLMClient.Setup(x => x.ChatClient).Returns(mockChatClient.Object);
        
        var services = TestHelpers.CreateTestServices(output);
        services.AddWebSearchTool(webSearch.Object);
        services.AddSingleton(mockLLMClient.Object);
        services.AddTransient<IAnalysisService, AnalysisService>();
        _provider = services.BuildServiceProvider();
    }
    
    [Fact]
    public void AnalysisService_CanBeConstructed_WithLLMClient()
    {
        // Act
        var service = _provider.GetRequiredService<IAnalysisService>();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNullImageData_ThrowsArgumentException()
    {
        // Arrange
        var service = _provider.GetRequiredService<IAnalysisService>();
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
        var service = _provider.GetRequiredService<IAnalysisService>();
        var request = new AnalysisRequest
        {
            ImageData = [],
            Prompt = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeAsync(request));
    }
}

