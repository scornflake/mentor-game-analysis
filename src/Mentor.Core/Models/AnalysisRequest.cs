using Microsoft.Extensions.AI;

namespace Mentor.Core.Models;

public class AnalysisRequest
{
    public byte[] ImageData { get; set; } = [];
    public string Prompt { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;

    public void ValidateRequest()
    {
        if (ImageData == null || ImageData.Length == 0)
        {
            throw new ArgumentException("Image data is required", nameof(AnalysisRequest));
        }
    }

    
    public DataContent GetImageAsReadOnlyMemory()
    {
        ValidateRequest();
        var memoryStream = new ReadOnlyMemory<byte>(ImageData);
        var userImage = new DataContent(memoryStream, "image/png");
        return userImage;
    }
}

