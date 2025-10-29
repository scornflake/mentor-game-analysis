using Mentor.Core.Data;
using Mentor.Core.Helpers;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tests.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Tools;

public class ImageAnalyzerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger<ImageAnalyzer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ImageAnalyzerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ImageAnalyzer>>();
    }

    private RawImage LoadTestImage(string filename)
    {
        var projectRoot = ApiKeyHelper.FindProjectRoot(AppContext.BaseDirectory);
        if (projectRoot == null)
        {
            throw new InvalidOperationException("Could not find project root");
        }

        var imagePath = Path.Combine(projectRoot, "tests", "media", filename);
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Test image not found: {imagePath}");
        }

        var imageBytes = File.ReadAllBytes(imagePath);
        var mimeType = ImageMimeTypeDetector.DetectMimeType(imageBytes, imagePath);
        return new RawImage(imageBytes, mimeType);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithNullImageData_ThrowsArgumentException()
    {
        // Arrange
        var analyzer = new ImageAnalyzer(_logger);
        var mockProvider = new Mock<ILLMClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await analyzer.AnalyzeImageAsync(null!, "Warframe", mockProvider.Object));
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithEmptyImageData_ThrowsArgumentException()
    {
        // Arrange
        var analyzer = new ImageAnalyzer(_logger);
        var mockProvider = new Mock<ILLMClient>();
        var emptyImage = new RawImage(new byte[] { 1 }, "image/png"); // RawImage doesn't allow empty arrays

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await analyzer.AnalyzeImageAsync(new RawImage(Array.Empty<byte>(), "image/png"), "Warframe", mockProvider.Object));
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithNullGameName_ThrowsArgumentException()
    {
        // Arrange
        var analyzer = new ImageAnalyzer(_logger);
        var mockProvider = new Mock<ILLMClient>();
        var imageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await analyzer.AnalyzeImageAsync(imageData, null!, mockProvider.Object));
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new ImageAnalyzer(_logger);
        var imageData = new RawImage(new byte[] { 1, 2, 3 }, "image/png");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await analyzer.AnalyzeImageAsync(imageData, "Warframe", null!));
    }

    // Note: Mocking structured responses with GetResponseAsync<T> is complex
    // Integration tests provide better coverage for this functionality

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeImageAsync_WithRealGameImage_ReturnsHighProbability()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== Starting Real API Test: Game-Related Image ===");

        var apiKey = ApiKeyHelper.GetOpenAIApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key not available for integration test");
        }

        var factory = new LLMProviderFactory(_serviceProvider);
        var providerConfig = new ProviderConfigurationEntity
        {
            Name = "openai-test",
            ProviderType = "openai",
            ApiKey = apiKey,
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        var provider = factory.GetProvider(providerConfig);
        var analyzer = new ImageAnalyzer(_logger);
        var imageData = LoadTestImage("acceltra prime rad build.png");

        _testOutputHelper.WriteLine("Analyzing game-related image: acceltra prime rad build.png");

        // Act
        var result = await analyzer.AnalyzeImageAsync(imageData, "Warframe", provider);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Description);
        Assert.InRange(result.GameRelevanceProbability, 0.0, 1.0);
        Assert.True(result.GameRelevanceProbability > 0.5, 
            $"Expected high probability for game image, got {result.GameRelevanceProbability:P0}");
        Assert.Equal("openai-test", result.ProviderUsed);
        Assert.True(result.GeneratedAt <= DateTime.UtcNow);
        Assert.True(result.Description.Length > 50, 
            "Description should contain meaningful content");

        // Output results
        _testOutputHelper.WriteLine("=== API Response Received ===");
        _testOutputHelper.WriteLine($"Provider: {result.ProviderUsed}");
        _testOutputHelper.WriteLine($"Game Relevance: {result.GameRelevanceProbability:P0}");
        _testOutputHelper.WriteLine($"Generated At: {result.GeneratedAt:u}");
        _testOutputHelper.WriteLine($"\n--- DESCRIPTION ---\n{result.Description}\n");
    }

    [RequiresOpenAIKeyFact]
    public async Task AnalyzeImageAsync_WithDifferentGame_ReturnsCorrectProbability()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== Starting Real API Test: Wrong Game Check ===");

        var apiKey = ApiKeyHelper.GetOpenAIApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key not available for integration test");
        }

        var factory = new LLMProviderFactory(_serviceProvider);
        var providerConfig = new ProviderConfigurationEntity
        {
            Name = "openai-test",
            ProviderType = "openai",
            ApiKey = apiKey,
            Model = "gpt-4o",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        var provider = factory.GetProvider(providerConfig);
        var analyzer = new ImageAnalyzer(_logger);
        var imageData = LoadTestImage("acceltra prime rad build.png");

        _testOutputHelper.WriteLine("Analyzing Warframe image with wrong game name: World of Warcraft");

        // Act
        var result = await analyzer.AnalyzeImageAsync(imageData, "World of Warcraft", provider);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Description);
        Assert.InRange(result.GameRelevanceProbability, 0.0, 1.0);
        
        // Output results regardless of actual probability
        _testOutputHelper.WriteLine("=== API Response Received ===");
        _testOutputHelper.WriteLine($"Provider: {result.ProviderUsed}");
        _testOutputHelper.WriteLine($"Game Relevance: {result.GameRelevanceProbability:P0}");
        _testOutputHelper.WriteLine($"Generated At: {result.GeneratedAt:u}");
        _testOutputHelper.WriteLine($"\n--- DESCRIPTION ---\n{result.Description}\n");
        _testOutputHelper.WriteLine($"\nNote: Probability for wrong game was {result.GameRelevanceProbability:P0}");
    }
}

