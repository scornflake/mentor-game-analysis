using Microsoft.Extensions.AI;

namespace Mentor.Core.Models;

public class AnalysisRequest
{
    public RawImage ImageData { get; set; } = null!;
    public string Prompt { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;

    public void ValidateRequest()
    {
        if (ImageData == null || ImageData.Data == null || ImageData.Data.Length == 0)
        {
            throw new ArgumentException("Image data is required", nameof(AnalysisRequest));
        }
    }

    
    public DataContent GetImageAsReadOnlyMemory()
    {
        ValidateRequest();
        var memoryStream = new ReadOnlyMemory<byte>(ImageData.Data);
        var userImage = new DataContent(memoryStream, ImageData.MimeType);
        return userImage;
    }
}

