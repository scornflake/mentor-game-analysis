using System.Text;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mentor.Core.Tests.Services;

public class AnalysisExportServiceTests : IDisposable
{
    private readonly Mock<ILogger<AnalysisExportService>> _mockLogger;
    private readonly Mock<IUserDataPathService> _mockUserDataPathService;
    private readonly AnalysisExportService _service;
    private readonly string _testOutputPath;
    private readonly List<string> _pathsToCleanup = new();

    public AnalysisExportServiceTests()
    {
        _mockLogger = new Mock<ILogger<AnalysisExportService>>();
        _mockUserDataPathService = new Mock<IUserDataPathService>();
        
        // Use a test-specific output path in temp directory
        _testOutputPath = Path.Combine(Path.GetTempPath(), "MentorTests", "SavedAnalysis");
        Directory.CreateDirectory(_testOutputPath);
        
        // Mock the path service to return our test directory
        _mockUserDataPathService
            .Setup(x => x.GetSavedAnalysisPath())
            .Returns(_testOutputPath);
        
        _service = new AnalysisExportService(_mockLogger.Object, _mockUserDataPathService.Object);
    }

    public void Dispose()
    {
        // Cleanup test directories
        foreach (var path in _pathsToCleanup)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
        
        if (Directory.Exists(_testOutputPath))
        {
            try
            {
                Directory.Delete(_testOutputPath, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldCreateFolderWithGameNameAndTimestamp()
    {
        // Arrange
        var request = CreateSampleExportRequest("Warframe");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);

        // Assert
        Assert.NotNull(resultPath);
        Assert.NotEmpty(resultPath);
        var directoryName = Path.GetFileName(Path.GetDirectoryName(resultPath));
        Assert.StartsWith("Warframe_", directoryName);
        Assert.Matches(@"^Warframe_\d{4}-\d{2}-\d{2}_\d{6}$", directoryName);
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldCreateIndexHtmlFile()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);

        // Assert
        Assert.True(File.Exists(resultPath));
        Assert.Equal("index.html", Path.GetFileName(resultPath));
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldSaveScreenshotImage()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        var directory = Path.GetDirectoryName(resultPath)!;
        _pathsToCleanup.Add(directory);

        // Assert - screenshot should be in assets subfolder
        var assetsPath = Path.Combine(directory, "assets");
        Assert.True(Directory.Exists(assetsPath));
        var screenshotFiles = Directory.GetFiles(assetsPath, "screenshot_*");
        Assert.Single(screenshotFiles);
        Assert.EndsWith(".png", screenshotFiles[0]);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldContainAllRequiredSections()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("Analysis Results", html);
        Assert.Contains("Provider:", html);
        Assert.Contains("Confidence:", html);
        Assert.Contains("Generated:", html);
        Assert.Contains("Prompt", html);
        Assert.Contains("Screenshot", html);
        Assert.Contains("Summary", html);
        Assert.Contains("Detailed Analysis", html);
        Assert.Contains("Recommendations", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldContainMetadata()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("TestProvider", html);
        Assert.Contains("0.85", html);
        Assert.Contains(request.Recommendation.GeneratedAt.ToLocalTime().ToString("g"), html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldContainPrompt()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");
        request.Prompt = "How can I maximize damage?";

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("How can I maximize damage?", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldContainSummaryAndAnalysis()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("This is a test summary", html);
        Assert.Contains("This is detailed analysis", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldContainRecommendations()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("High", html);
        Assert.Contains("Recommendation 1", html);
        Assert.Contains("Because reason 1", html);
        Assert.Contains("Context 1", html);
        Assert.Contains("Medium", html);
        Assert.Contains("Recommendation 2", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldContainSearchResults()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("Search Results", html);
        Assert.Contains("Test Article 1", html);
        Assert.Contains("https://example.com/article1", html);
        Assert.Contains("Test snippet 1", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldReferenceScreenshotImage()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert - HTML should reference screenshot in assets folder with timestamp
        Assert.Contains("./assets/screenshot_", html);
        Assert.Contains(".png", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_HtmlShouldEscapeSpecialCharacters()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");
        request.Recommendation.Summary = "Test <script>alert('xss')</script>";

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldHandleEmptyRecommendations()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");
        request.Recommendation.Recommendations = new List<RecommendationItem>();

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.NotNull(resultPath);
        Assert.NotEmpty(resultPath);
        Assert.Contains("Recommendations", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldHandleEmptySearchResults()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");
        request.Recommendation.SearchResults = new List<SearchResult>();

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.NotNull(resultPath);
        Assert.NotEmpty(resultPath);
        Assert.Contains("Search Results", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldHandleJpegImage()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");
        request.ImageData = new RawImage(CreateSampleImageBytes(), "image/jpeg");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        var directory = Path.GetDirectoryName(resultPath)!;
        _pathsToCleanup.Add(directory);

        // Assert - JPEG should be in assets subfolder
        var assetsPath = Path.Combine(directory, "assets");
        var screenshotFiles = Directory.GetFiles(assetsPath, "screenshot_*");
        Assert.Single(screenshotFiles);
        Assert.EndsWith(".jpg", screenshotFiles[0]);
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldIncludeRecommendationReferenceLinks()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");
        request.Recommendation.Recommendations[0].ReferenceLink = "https://example.com/guide";

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);
        var html = await File.ReadAllTextAsync(resultPath);

        // Assert
        Assert.Contains("https://example.com/guide", html);
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldSanitizeGameNameInFolderName()
    {
        // Arrange
        var request = CreateSampleExportRequest("Test/Game:Name*");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        _pathsToCleanup.Add(Path.GetDirectoryName(resultPath)!);

        // Assert
        var directoryName = Path.GetFileName(Path.GetDirectoryName(resultPath));
        Assert.DoesNotContain("/", directoryName);
        Assert.DoesNotContain(":", directoryName);
        Assert.DoesNotContain("*", directoryName);
    }

    private ExportRequest CreateSampleExportRequest(string gameName)
    {
        var recommendation = new Recommendation
        {
            Summary = "This is a test summary",
            Analysis = "This is detailed analysis",
            Confidence = 0.85,
            GeneratedAt = DateTime.UtcNow,
            ProviderUsed = "TestProvider",
            Recommendations = new List<RecommendationItem>
            {
                new()
                {
                    Priority = Priority.High,
                    Action = "Recommendation 1",
                    Reasoning = "Because reason 1",
                    Context = "Context 1",
                    ReferenceLink = ""
                },
                new()
                {
                    Priority = Priority.Medium,
                    Action = "Recommendation 2",
                    Reasoning = "Because reason 2",
                    Context = "Context 2",
                    ReferenceLink = ""
                }
            },
            SearchResults = new List<SearchResult>
            {
                new()
                {
                    Title = "Test Article 1",
                    Url = "https://example.com/article1",
                    Content = "Test snippet 1"
                }
            }
        };

        return new ExportRequest
        {
            Recommendation = recommendation,
            ImageData = new RawImage(CreateSampleImageBytes(), "image/png"),
            Prompt = "Test prompt",
            GameName = gameName
        };
    }

    [Fact]
    public async Task ExportAnalysisAsync_ShouldCreateAssetsSubfolder()
    {
        // Arrange
        var request = CreateSampleExportRequest("TestGame");

        // Act
        var resultPath = await _service.ExportAnalysisAsync(request);
        var directory = Path.GetDirectoryName(resultPath)!;
        _pathsToCleanup.Add(directory);

        // Assert
        var assetsPath = Path.Combine(directory, "assets");
        Assert.True(Directory.Exists(assetsPath));
    }

    private byte[] CreateSampleImageBytes()
    {
        // Create a minimal valid PNG (1x1 transparent pixel)
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        };
    }
}

