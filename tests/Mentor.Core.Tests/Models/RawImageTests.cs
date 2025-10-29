using Mentor.Core.Helpers;
using Mentor.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mentor.Core.Tests.Models;

public class RawImageTests
{
    [Fact]
    public void ConvertToPng_WhenAlreadyPng_ReturnsSameInstance()
    {
        // Arrange
        var pngData = CreateTestPngImage();
        var rawImage = new RawImage(pngData, "image/png");

        // Act
        var result = rawImage.ConvertToPng();

        // Assert
        Assert.Same(rawImage, result);
        Assert.Equal("image/png", result.MimeType);
    }

    [Fact]
    public void ConvertToPng_WhenBmp_ConvertsToValidPng()
    {
        // Arrange - Create a minimal BMP manually (24-bit, 2x2 pixels, blue)
        var bmpData = CreateMinimalBmpImage();
        var rawImage = new RawImage(bmpData, "image/bmp");

        // Act
        var result = rawImage.ConvertToPng();

        // Assert
        Assert.NotSame(rawImage, result);
        Assert.Equal("image/png", result.MimeType);
        Assert.True(result.Data.Length > 0);
        
        // Verify it's a valid PNG by checking magic number
        Assert.True(IsPngFormat(result.Data), "Converted data should be valid PNG format");
    }

    [Fact]
    public void ConvertToPng_WhenJpeg_ConvertsToValidPng()
    {
        // Arrange
        var jpegData = CreateTestJpegImage();
        var rawImage = new RawImage(jpegData, "image/jpeg");

        // Act
        var result = rawImage.ConvertToPng();

        // Assert
        Assert.NotSame(rawImage, result);
        Assert.Equal("image/png", result.MimeType);
        Assert.True(result.Data.Length > 0);
        
        // Verify it's a valid PNG
        Assert.True(IsPngFormat(result.Data), "Converted data should be valid PNG format");
    }

    [Fact]
    public void ConvertToPng_WithInvalidImageData_ThrowsInvalidOperationException()
    {
        // Arrange - create invalid image data (just random bytes)
        var invalidData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var rawImage = new RawImage(invalidData, "image/bmp");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => rawImage.ConvertToPng());
        Assert.Contains("Failed to convert image", exception.Message);
    }

    [Fact]
    public void ConvertToPng_PreservesImageContent()
    {
        // Arrange - create a JPEG with specific content (red 10x10 image)
        var jpegData = CreateColoredJpegImage(Color.Red, 10, 10);
        var rawImage = new RawImage(jpegData, "image/jpeg");

        // Act
        var result = rawImage.ConvertToPng();

        // Assert - decode both and verify they have similar dimensions
        using var originalImage = Image.Load(jpegData);
        using var convertedImage = Image.Load(result.Data);
        
        Assert.Equal(originalImage.Width, convertedImage.Width);
        Assert.Equal(originalImage.Height, convertedImage.Height);
        
        // Note: We can't do exact pixel comparison with JPEG due to lossy compression
        // but we verify dimensions are preserved
    }

    private static byte[] CreateTestPngImage()
    {
        using var image = new Image<Rgba32>(100, 100);
        image.Mutate(ctx => ctx.BackgroundColor(Color.Blue));
        
        using var outputStream = new MemoryStream();
        image.SaveAsPng(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] CreateMinimalBmpImage()
    {
        // Create a minimal 2x2 pixel BMP (24-bit, blue pixels)
        // BMP file structure: File Header (14 bytes) + DIB Header (40 bytes) + Pixel Data
        var bmpData = new List<byte>();
        
        // BMP File Header (14 bytes)
        bmpData.AddRange(new byte[] { 0x42, 0x4D }); // "BM" signature
        bmpData.AddRange(BitConverter.GetBytes(70)); // File size (14 + 40 + 16)
        bmpData.AddRange(new byte[] { 0, 0, 0, 0 }); // Reserved
        bmpData.AddRange(BitConverter.GetBytes(54)); // Offset to pixel data
        
        // DIB Header (BITMAPINFOHEADER, 40 bytes)
        bmpData.AddRange(BitConverter.GetBytes(40)); // DIB header size
        bmpData.AddRange(BitConverter.GetBytes(2)); // Width
        bmpData.AddRange(BitConverter.GetBytes(2)); // Height
        bmpData.AddRange(BitConverter.GetBytes((short)1)); // Color planes
        bmpData.AddRange(BitConverter.GetBytes((short)24)); // Bits per pixel
        bmpData.AddRange(new byte[24]); // Rest of DIB header (compression, sizes, etc.)
        
        // Pixel data (2x2, 24-bit BGR, bottom-up)
        // Row 1 (bottom): Blue, Blue
        bmpData.AddRange(new byte[] { 255, 0, 0, 255, 0, 0 }); // BGR, BGR
        bmpData.AddRange(new byte[] { 0, 0 }); // Row padding
        // Row 2 (top): Blue, Blue
        bmpData.AddRange(new byte[] { 255, 0, 0, 255, 0, 0 }); // BGR, BGR
        bmpData.AddRange(new byte[] { 0, 0 }); // Row padding
        
        return bmpData.ToArray();
    }

    private static byte[] CreateTestJpegImage()
    {
        using var image = new Image<Rgba32>(100, 100);
        image.Mutate(ctx => ctx.BackgroundColor(Color.Yellow));
        
        using var outputStream = new MemoryStream();
        image.SaveAsJpeg(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] CreateColoredJpegImage(Color color, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.BackgroundColor(color));
        
        using var outputStream = new MemoryStream();
        image.SaveAsJpeg(outputStream);
        return outputStream.ToArray();
    }

    private static bool IsPngFormat(byte[] data)
    {
        // PNG files start with the magic number: 89 50 4E 47 0D 0A 1A 0A
        if (data.Length < 8)
        {
            return false;
        }

        return data[0] == 0x89 &&
               data[1] == 0x50 &&
               data[2] == 0x4E &&
               data[3] == 0x47 &&
               data[4] == 0x0D &&
               data[5] == 0x0A &&
               data[6] == 0x1A &&
               data[7] == 0x0A;
    }
}

