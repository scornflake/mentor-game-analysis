namespace Mentor.Core.Models;

public class AnalysisRequest
{
    public byte[] ImageData { get; set; } = [];
    public string Prompt { get; set; } = string.Empty;
}

