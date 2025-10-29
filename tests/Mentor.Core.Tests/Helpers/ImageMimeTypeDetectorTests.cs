using Mentor.Core.Helpers;
using Xunit;

namespace Mentor.Core.Tests.Helpers;

public class ImageMimeTypeDetectorTests
{
    [Theory]
    [InlineData(".png", "image/png")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".PNG", "image/png")]
    [InlineData(".JPG", "image/jpeg")]
    public void DetectMimeType_WithFileExtension_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Arrange
        var filePath = $"test{extension}";
        var dummyData = new byte[] { 0x00, 0x01, 0x02 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(dummyData, filePath);

        // Assert
        Assert.Equal(expectedMimeType, result);
    }

    [Fact]
    public void DetectMimeType_WithPngMagicNumber_ReturnsPngMimeType()
    {
        // Arrange - PNG magic number: 89 50 4E 47
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(pngData);

        // Assert
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void DetectMimeType_WithJpegMagicNumber_ReturnsJpegMimeType()
    {
        // Arrange - JPEG magic number: FF D8 FF
        var jpegData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(jpegData);

        // Assert
        Assert.Equal("image/jpeg", result);
    }

    [Fact]
    public void DetectMimeType_WithGifMagicNumber_ReturnsGifMimeType()
    {
        // Arrange - GIF magic number: 47 49 46 38 (GIF8)
        var gifData = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(gifData);

        // Assert
        Assert.Equal("image/gif", result);
    }

    [Fact]
    public void DetectMimeType_WithBmpMagicNumber_ReturnsBmpMimeType()
    {
        // Arrange - BMP magic number: 42 4D (BM)
        var bmpData = new byte[] { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(bmpData);

        // Assert
        Assert.Equal("image/bmp", result);
    }

    [Fact]
    public void DetectMimeType_WithWebPMagicNumber_ReturnsWebPMimeType()
    {
        // Arrange - WebP magic number: RIFF...WEBP
        var webpData = new byte[] 
        { 
            0x52, 0x49, 0x46, 0x46, // RIFF
            0x00, 0x00, 0x00, 0x00, // size (placeholder)
            0x57, 0x45, 0x42, 0x50  // WEBP
        };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(webpData);

        // Assert
        Assert.Equal("image/webp", result);
    }

    [Fact]
    public void DetectMimeType_WithUnknownExtension_FallsBackToMagicNumber()
    {
        // Arrange
        var filePath = "test.unknown";
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(pngData, filePath);

        // Assert
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void DetectMimeType_WithNoExtensionAndUnknownMagicNumber_DefaultsToPng()
    {
        // Arrange
        var unknownData = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(unknownData);

        // Assert
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void DetectMimeType_WithEmptyData_DefaultsToPng()
    {
        // Arrange
        var emptyData = new byte[] { };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(emptyData);

        // Assert
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void DetectMimeType_WithNullFilePath_UsesMagicNumber()
    {
        // Arrange
        var jpegData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(jpegData, null);

        // Assert
        Assert.Equal("image/jpeg", result);
    }

    [Fact]
    public void DetectMimeType_WithTooShortData_DefaultsToPng()
    {
        // Arrange - Less than 4 bytes required for magic number detection
        var shortData = new byte[] { 0x89, 0x50 };

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(shortData);

        // Assert
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void DetectMimeType_FileExtensionTakesPrecedenceOverMagicNumber()
    {
        // Arrange - JPEG data but PNG extension
        var jpegData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var filePath = "test.png";

        // Act
        var result = ImageMimeTypeDetector.DetectMimeType(jpegData, filePath);

        // Assert - Extension should take precedence
        Assert.Equal("image/png", result);
    }
}

