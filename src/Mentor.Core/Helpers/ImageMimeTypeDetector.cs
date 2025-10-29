namespace Mentor.Core.Helpers;

/// <summary>
/// Utility for detecting image MIME types from file extensions and byte signatures (magic numbers)
/// </summary>
public static class ImageMimeTypeDetector
{
    /// <summary>
    /// Detects the MIME type of an image from its file path extension or byte content
    /// </summary>
    /// <param name="data">The image data bytes</param>
    /// <param name="filePath">Optional file path to check extension</param>
    /// <returns>The detected MIME type, defaults to "image/png" if detection fails</returns>
    public static string DetectMimeType(byte[] data, string? filePath = null)
    {
        // Try file extension first if available
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var mimeType = GetMimeTypeFromExtension(filePath);
            if (mimeType != null)
            {
                return mimeType;
            }
        }

        // Fall back to magic number detection
        if (data != null && data.Length > 0)
        {
            var mimeType = GetMimeTypeFromMagicNumber(data);
            if (mimeType != null)
            {
                return mimeType;
            }
        }

        // Default to PNG (maintains current behavior)
        return "image/png";
    }

    /// <summary>
    /// Gets MIME type from file extension
    /// </summary>
    private static string? GetMimeTypeFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => null
        };
    }

    /// <summary>
    /// Gets MIME type from magic number (file signature)
    /// </summary>
    private static string? GetMimeTypeFromMagicNumber(byte[] data)
    {
        if (data.Length < 4)
        {
            return null;
        }

        // PNG: 89 50 4E 47 (‰PNG)
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            return "image/png";
        }

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // GIF: 47 49 46 38 (GIF8)
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
        {
            return "image/gif";
        }

        // BMP: 42 4D (BM)
        if (data[0] == 0x42 && data[1] == 0x4D)
        {
            return "image/bmp";
        }

        // WebP: 52 49 46 46 ... 57 45 42 50 (RIFF...WEBP)
        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
        {
            return "image/webp";
        }

        return null;
    }
}

