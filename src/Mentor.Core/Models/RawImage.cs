using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace Mentor.Core.Models;

/// <summary>
/// Represents raw image data with its associated MIME type
/// </summary>
public class RawImage
{
    /// <summary>
    /// The raw image data as bytes
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// The MIME type of the image (e.g., "image/png", "image/jpeg")
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// Creates a new RawImage instance with the specified data and MIME type
    /// </summary>
    /// <param name="data">The raw image data</param>
    /// <param name="mimeType">The MIME type of the image</param>
    /// <exception cref="ArgumentException">Thrown when data is null or empty, or mimeType is invalid</exception>
    public RawImage(byte[] data, string mimeType)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Image data cannot be null or empty", nameof(data));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("MIME type cannot be null or empty", nameof(mimeType));
        }

        if (!mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("MIME type must be an image type (e.g., 'image/png')", nameof(mimeType));
        }

        Data = data;
        MimeType = mimeType;
    }

    /// <summary>
    /// Gets the size of the image data in bytes
    /// </summary>
    public int SizeInBytes => Data.Length;

    /// <summary>
    /// Converts the image to PNG format. If the image is already PNG, returns the same instance.
    /// </summary>
    /// <returns>A new RawImage instance with PNG data, or the same instance if already PNG</returns>
    /// <exception cref="InvalidOperationException">Thrown when the image cannot be decoded or encoded</exception>
    public RawImage ConvertToPng()
    {
        // If already PNG, return this instance
        if (MimeType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        // Use ImageSharp to decode and re-encode as PNG
        try
        {
            using var inputStream = new MemoryStream(Data);
            using var image = Image.Load(inputStream);
            
            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            
            return new RawImage(outputStream.ToArray(), "image/png");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert image with MIME type {MimeType} to PNG", ex);
        }
    }
}

