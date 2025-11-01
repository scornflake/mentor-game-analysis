using Mentor.Core.Models;

namespace Mentor.Core.Interfaces;

public class ExportRequest
{
    public Recommendation Recommendation { get; set; } = null!;
    public RawImage ImageData { get; set; } = null!;
    public string Prompt { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
}

public interface IAnalysisExportService
{
    /// <summary>
    /// Exports an analysis to HTML with associated image file.
    /// </summary>
    /// <param name="request">The export request containing analysis data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The full path to the exported HTML file</returns>
    Task<string> ExportAnalysisAsync(ExportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Opens the saved analysis folder in the system file explorer.
    /// </summary>
    void OpenSaveFolder();
}

