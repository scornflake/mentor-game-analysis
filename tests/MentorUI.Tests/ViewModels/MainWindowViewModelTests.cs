using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using MentorUI.ViewModels;
using Moq;

namespace MentorUI.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<IAnalysisService> _mockAnalysisService;
    private readonly Mock<ILLMProviderFactory> _mockProviderFactory;

    public MainWindowViewModelTests()
    {
        _mockAnalysisService = new Mock<IAnalysisService>();
        _mockProviderFactory = new Mock<ILLMProviderFactory>();
    }

    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Act
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object);

        // Assert
        Assert.Null(viewModel.ImagePath);
        Assert.Equal("What should I do next?", viewModel.Prompt);
        Assert.Equal("perplexity", viewModel.SelectedProvider);
        Assert.False(viewModel.IsAnalyzing);
        Assert.Null(viewModel.Result);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public void Providers_ContainsExpectedProviders()
    {
        // Act
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object);

        // Assert
        Assert.Contains("openai", viewModel.Providers);
        Assert.Contains("perplexity", viewModel.Providers);
        Assert.Contains("local", viewModel.Providers);
    }

    [Fact]
    public void AnalyzeCommand_CannotExecute_WhenImagePathIsNull()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object);

        // Act
        var canExecute = viewModel.AnalyzeCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void AnalyzeCommand_CannotExecute_WhenImagePathIsEmpty()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
        {
            ImagePath = string.Empty
        };

        // Act
        var canExecute = viewModel.AnalyzeCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void AnalyzeCommand_CanExecute_WhenImagePathIsSet()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
        {
            ImagePath = "/path/to/image.png"
        };

        // Act
        var canExecute = viewModel.AnalyzeCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void AnalyzeCommand_CannotExecute_WhenIsAnalyzing()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
        {
            ImagePath = "/path/to/image.png"
        };

        // Simulate analyzing state by reflection or by triggering an analysis
        var isAnalyzingProperty = typeof(MainWindowViewModel).GetProperty("IsAnalyzing");
        isAnalyzingProperty?.SetValue(viewModel, true);

        // Act
        var canExecute = viewModel.AnalyzeCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public async Task AnalyzeAsync_SetsIsAnalyzing_DuringExecution()
    {
        // Arrange
        var testImagePath = Path.Combine(Path.GetTempPath(), "test_image.png");
        await File.WriteAllBytesAsync(testImagePath, new byte[] { 1, 2, 3, 4 });

        try
        {
            var recommendation = new Recommendation
            {
                Summary = "Test summary",
                Analysis = "Test analysis",
                Confidence = 0.9,
                ProviderUsed = "test",
                GeneratedAt = DateTime.UtcNow,
                Recommendations = new List<RecommendationItem>()
            };

            _mockAnalysisService
                .Setup(x => x.AnalyzeAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendation);

            var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
            {
                ImagePath = testImagePath
            };

            // Act
            var analyzeTask = viewModel.AnalyzeAsync();
            
            // Assert - should be analyzing during execution
            Assert.True(viewModel.IsAnalyzing);
            
            await analyzeTask;
            
            // Assert - should not be analyzing after completion
            Assert.False(viewModel.IsAnalyzing);
        }
        finally
        {
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_CallsAnalysisService_WithCorrectParameters()
    {
        // Arrange
        var testImagePath = Path.Combine(Path.GetTempPath(), "test_image.png");
        var testImageData = new byte[] { 1, 2, 3, 4 };
        await File.WriteAllBytesAsync(testImagePath, testImageData);

        try
        {
            var recommendation = new Recommendation
            {
                Summary = "Test summary",
                Analysis = "Test analysis",
                Confidence = 0.9,
                ProviderUsed = "test",
                GeneratedAt = DateTime.UtcNow,
                Recommendations = new List<RecommendationItem>()
            };

            _mockAnalysisService
                .Setup(x => x.AnalyzeAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendation);

            var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
            {
                ImagePath = testImagePath,
                Prompt = "Test prompt"
            };

            // Act
            await viewModel.AnalyzeAsync();

            // Assert
            _mockAnalysisService.Verify(x => x.AnalyzeAsync(
                It.Is<AnalysisRequest>(r => 
                    r.Prompt == "Test prompt" && 
                    r.ImageData.SequenceEqual(testImageData)),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_SetsResult_OnSuccess()
    {
        // Arrange
        var testImagePath = Path.Combine(Path.GetTempPath(), "test_image.png");
        await File.WriteAllBytesAsync(testImagePath, new byte[] { 1, 2, 3, 4 });

        try
        {
            var recommendation = new Recommendation
            {
                Summary = "Test summary",
                Analysis = "Test analysis",
                Confidence = 0.9,
                ProviderUsed = "test",
                GeneratedAt = DateTime.UtcNow,
                Recommendations = new List<RecommendationItem>
                {
                    new RecommendationItem
                    {
                        Priority = Priority.High,
                        Action = "Test action",
                        Reasoning = "Test reasoning",
                        Context = "Test context"
                    }
                }
            };

            _mockAnalysisService
                .Setup(x => x.AnalyzeAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendation);

            var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
            {
                ImagePath = testImagePath
            };

            // Act
            await viewModel.AnalyzeAsync();

            // Assert
            Assert.NotNull(viewModel.Result);
            Assert.Equal("Test summary", viewModel.Result.Summary);
            Assert.Equal("Test analysis", viewModel.Result.Analysis);
            Assert.Equal(0.9, viewModel.Result.Confidence);
            Assert.Single(viewModel.Result.Recommendations);
            Assert.Null(viewModel.ErrorMessage);
        }
        finally
        {
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_SetsErrorMessage_OnFileNotFound()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
        {
            ImagePath = "/nonexistent/path/image.png"
        };

        // Act
        await viewModel.AnalyzeAsync();

        // Assert
        Assert.Null(viewModel.Result);
        Assert.NotNull(viewModel.ErrorMessage);
        Assert.Contains("reading image", viewModel.ErrorMessage);
        Assert.False(viewModel.IsAnalyzing);
    }

    [Fact]
    public async Task AnalyzeAsync_SetsErrorMessage_OnAnalysisServiceException()
    {
        // Arrange
        var testImagePath = Path.Combine(Path.GetTempPath(), "test_image.png");
        await File.WriteAllBytesAsync(testImagePath, new byte[] { 1, 2, 3, 4 });

        try
        {
            _mockAnalysisService
                .Setup(x => x.AnalyzeAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));

            var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
            {
                ImagePath = testImagePath
            };

            // Act
            await viewModel.AnalyzeAsync();

            // Assert
            Assert.Null(viewModel.Result);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Test error", viewModel.ErrorMessage);
            Assert.False(viewModel.IsAnalyzing);
        }
        finally
        {
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ClearsErrorMessage_OnSuccess()
    {
        // Arrange
        var testImagePath = Path.Combine(Path.GetTempPath(), "test_image.png");
        await File.WriteAllBytesAsync(testImagePath, new byte[] { 1, 2, 3, 4 });

        try
        {
            var recommendation = new Recommendation
            {
                Summary = "Test summary",
                Analysis = "Test analysis",
                Confidence = 0.9,
                ProviderUsed = "test",
                GeneratedAt = DateTime.UtcNow,
                Recommendations = new List<RecommendationItem>()
            };

            _mockAnalysisService
                .Setup(x => x.AnalyzeAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendation);

            var viewModel = new MainWindowViewModel(_mockAnalysisService.Object, _mockProviderFactory.Object)
            {
                ImagePath = testImagePath
            };

            // Set an error message first
            var errorProperty = typeof(MainWindowViewModel).GetProperty("ErrorMessage");
            errorProperty?.SetValue(viewModel, "Previous error");

            // Act
            await viewModel.AnalyzeAsync();

            // Assert
            Assert.Null(viewModel.ErrorMessage);
            Assert.NotNull(viewModel.Result);
        }
        finally
        {
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }
}

