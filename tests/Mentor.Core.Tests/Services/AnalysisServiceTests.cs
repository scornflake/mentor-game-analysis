using Mentor.Core.Models;
using Mentor.Core.Services;

namespace Mentor.Core.Tests.Services;

public class AnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsNonNullRecommendation()
    {
        // Arrange
        var service = new AnalysisService();
        var request = new AnalysisRequest
        {
            ImageData = Array.Empty<byte>(),
            Prompt = "Test prompt"
        };

        // Act
        var result = await service.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Analysis);
        Assert.NotEmpty(result.Summary);
        Assert.NotEmpty(result.Recommendations);
        Assert.True(result.Confidence > 0);
        Assert.Equal("Stub", result.ProviderUsed);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsMultipleRecommendations()
    {
        // Arrange
        var service = new AnalysisService();
        var request = new AnalysisRequest
        {
            ImageData = Array.Empty<byte>(),
            Prompt = "What should I do?"
        };

        // Act
        var result = await service.AnalyzeAsync(request);

        // Assert
        Assert.True(result.Recommendations.Count >= 2);
        Assert.Contains(result.Recommendations, r => r.Priority == Priority.High);
    }
}

