using Mentor.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mentor.Core.Tests.Services;

public class UserDataPathServiceTests
{
    private readonly Mock<ILogger<UserDataPathService>> _mockLogger;
    private readonly UserDataPathService _service;

    public UserDataPathServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserDataPathService>>();
        _service = new UserDataPathService(_mockLogger.Object);
    }

    [Fact]
    public void GetBasePath_ReturnsCorrectPath()
    {
        // Act
        var path = _service.GetBasePath();

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith("Mentor", path);
        Assert.Contains(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path);
    }

    [Fact]
    public void GetRulesPath_ReturnsCorrectPath()
    {
        // Act
        var path = _service.GetRulesPath("warframe");

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith(Path.Combine("data", "rules", "warframe"), path);
        Assert.Contains("Mentor", path);
    }

    [Fact]
    public void GetRulesPath_ThrowsArgumentException_WhenGameNameIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GetRulesPath(null!));
    }

    [Fact]
    public void GetRulesPath_ThrowsArgumentException_WhenGameNameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GetRulesPath(string.Empty));
    }

    [Fact]
    public void GetRulesPath_ThrowsArgumentException_WhenGameNameIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GetRulesPath("   "));
    }

    [Fact]
    public void GetSavedAnalysisPath_ReturnsCorrectPath()
    {
        // Act
        var path = _service.GetSavedAnalysisPath();

        // Assert
        Assert.NotNull(path);
        Assert.EndsWith("Saved Analysis", path);
        Assert.Contains("Mentor", path);
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "MentorTest_" + Guid.NewGuid());

        try
        {
            Assert.False(Directory.Exists(testPath));

            // Act
            _service.EnsureDirectoryExists(testPath);

            // Assert
            Assert.True(Directory.Exists(testPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath);
            }
        }
    }

    [Fact]
    public void EnsureDirectoryExists_DoesNotThrow_WhenDirectoryAlreadyExists()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "MentorTest_" + Guid.NewGuid());
        Directory.CreateDirectory(testPath);

        try
        {
            // Act & Assert - should not throw
            _service.EnsureDirectoryExists(testPath);
            Assert.True(Directory.Exists(testPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath);
            }
        }
    }

    [Fact]
    public void EnsureDirectoryExists_ThrowsArgumentException_WhenPathIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.EnsureDirectoryExists(null!));
    }

    [Fact]
    public void EnsureDirectoryExists_ThrowsArgumentException_WhenPathIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.EnsureDirectoryExists(string.Empty));
    }

    [Fact]
    public void EnsureDirectoryExists_ThrowsArgumentException_WhenPathIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.EnsureDirectoryExists("   "));
    }
}

