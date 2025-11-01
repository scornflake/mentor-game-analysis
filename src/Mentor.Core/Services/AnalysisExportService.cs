using System.Text;
using System.Web;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Services;

public class AnalysisExportService : IAnalysisExportService
{
    private readonly ILogger<AnalysisExportService> _logger;

    public AnalysisExportService(ILogger<AnalysisExportService> logger)
    {
        _logger = logger;
    }

    private string GetBaseSavePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mentor",
            "Saved Analysis"
        );
    }

    private void EnsureBaseSavePathExists()
    {
        var basePath = GetBaseSavePath();
        Directory.CreateDirectory(basePath);
        _logger.LogInformation("Ensured base save path exists: {BasePath}", basePath);
    }

    public void OpenSaveFolder()
    {
        try
        {
            // Ensure the folder exists
            EnsureBaseSavePathExists();
            
            // Get the path
            var path = GetBaseSavePath();
            
            _logger.LogInformation("Opening save folder: {Path}", path);
            
            // Open the folder using shell execute
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening save folder");
            throw;
        }
    }

    public async Task<string> ExportAnalysisAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (request?.Recommendation == null)
                throw new ArgumentNullException(nameof(request), "Export request and recommendation are required");
            
            if (request.ImageData == null || request.ImageData.Data == null || request.ImageData.Data.Length == 0)
                throw new ArgumentException("Image data is required", nameof(request));

            // Get base path
            var basePath = GetBaseSavePath();

            // Create folder name with game name and timestamp
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var sanitizedGameName = SanitizeFileName(request.GameName);
            var folderName = string.IsNullOrWhiteSpace(sanitizedGameName) 
                ? $"Analysis_{timestamp}" 
                : $"{sanitizedGameName}_{timestamp}";
            
            var exportPath = Path.Combine(basePath, folderName);
            Directory.CreateDirectory(exportPath);

            // Create assets subfolder
            var assetsPath = Path.Combine(exportPath, "assets");
            Directory.CreateDirectory(assetsPath);

            _logger.LogInformation("Exporting analysis to: {ExportPath}", exportPath);

            // Save screenshot image in assets folder with timestamp
            var imageExtension = GetImageExtension(request.ImageData.MimeType);
            var screenshotFileName = $"screenshot_{timestamp}{imageExtension}";
            var screenshotPath = Path.Combine(assetsPath, screenshotFileName);
            await File.WriteAllBytesAsync(screenshotPath, request.ImageData.Data, cancellationToken);

            // Generate and save HTML (pass relative path to screenshot)
            var screenshotRelativePath = $"./assets/{screenshotFileName}";
            var html = GenerateHtml(request, screenshotRelativePath);
            var htmlPath = Path.Combine(exportPath, "index.html");
            await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("Analysis exported successfully to: {HtmlPath}", htmlPath);
            return htmlPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analysis");
            throw;
        }
    }

    private string GenerateHtml(ExportRequest request, string screenshotFileName)
    {
        var recommendation = request.Recommendation;
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>Analysis Results - {HtmlEncode(request.GameName)}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        
        // Header
        sb.AppendLine("        <h1>Analysis Results</h1>");
        
        // Metadata section
        sb.AppendLine("        <div class=\"metadata\">");
        sb.AppendLine($"            <div class=\"metadata-item\"><span class=\"label\">Provider:</span> {HtmlEncode(recommendation.ProviderUsed)}</div>");
        sb.AppendLine($"            <div class=\"metadata-item\"><span class=\"label\">Confidence:</span> {recommendation.Confidence:F2}</div>");
        sb.AppendLine($"            <div class=\"metadata-item\"><span class=\"label\">Generated:</span> {HtmlEncode(recommendation.GeneratedAt.ToLocalTime().ToString("g"))}</div>");
        sb.AppendLine("        </div>");

        // Prompt section
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            sb.AppendLine("        <div class=\"section\">");
            sb.AppendLine("            <h2>Prompt</h2>");
            sb.AppendLine("            <div class=\"card\">");
            sb.AppendLine($"                <p>{HtmlEncode(request.Prompt)}</p>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
        }

        // Screenshot section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Screenshot</h2>");
        sb.AppendLine("            <div class=\"screenshot-container\">");
        sb.AppendLine($"                <img src=\"./{HtmlEncode(screenshotFileName)}\" alt=\"Game Screenshot\" class=\"screenshot\" />");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        // Summary section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Summary</h2>");
        sb.AppendLine("            <div class=\"card\">");
        sb.AppendLine($"                <p>{HtmlEncode(recommendation.Summary)}</p>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        // Detailed Analysis section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Detailed Analysis</h2>");
        sb.AppendLine("            <div class=\"card\">");
        sb.AppendLine($"                <p class=\"analysis-text\">{HtmlEncode(recommendation.Analysis)}</p>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        // Recommendations section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Recommendations</h2>");
        
        if (recommendation.Recommendations != null && recommendation.Recommendations.Count > 0)
        {
            foreach (var rec in recommendation.Recommendations.OrderBy(r => r.Priority))
            {
                var priorityClass = rec.Priority.ToString().ToLower();
                sb.AppendLine("            <div class=\"card recommendation-card\">");
                sb.AppendLine("                <div class=\"recommendation-header\">");
                sb.AppendLine($"                    <span class=\"priority-badge priority-{priorityClass}\">{HtmlEncode(rec.Priority.ToString())}</span>");
                sb.AppendLine($"                    <span class=\"recommendation-action\">{HtmlEncode(rec.Action)}</span>");
                sb.AppendLine("                </div>");
                sb.AppendLine("                <div class=\"recommendation-content\">");
                sb.AppendLine($"                    <p><span class=\"label\">Reasoning:</span> {HtmlEncode(rec.Reasoning)}</p>");
                if (!string.IsNullOrWhiteSpace(rec.ReferenceLink) && rec.ReferenceLink.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"                    <p class=\"reference-link\"><a href=\"{HtmlEncode(rec.ReferenceLink)}\" target=\"_blank\">{HtmlEncode(rec.ReferenceLink)}</a></p>");
                }
                sb.AppendLine($"                    <p><span class=\"label\">Context:</span> {HtmlEncode(rec.Context)}</p>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </div>");
            }
        }
        else
        {
            sb.AppendLine("            <div class=\"card\">");
            sb.AppendLine("                <p>No recommendations available.</p>");
            sb.AppendLine("            </div>");
        }
        
        sb.AppendLine("        </div>");

        // Search Results section (at bottom, secondary styling)
        sb.AppendLine("        <div class=\"section search-results-section\">");
        sb.AppendLine("            <h2>Search Results</h2>");
        sb.AppendLine("            <div class=\"search-results\">");
        
        if (recommendation.SearchResults != null && recommendation.SearchResults.Count > 0)
        {
            foreach (var result in recommendation.SearchResults)
            {
                sb.AppendLine("                <div class=\"search-result-item\">");
                sb.AppendLine($"                    <h3><a href=\"{HtmlEncode(result.Url)}\" target=\"_blank\">{HtmlEncode(result.Title)}</a></h3>");
                if (!string.IsNullOrWhiteSpace(result.Content))
                {
                    sb.AppendLine($"                    <p class=\"search-snippet\">{HtmlEncode(result.Content)}</p>");
                }
                sb.AppendLine($"                    <p class=\"search-url\">{HtmlEncode(result.Url)}</p>");
                sb.AppendLine("                </div>");
            }
        }
        else
        {
            sb.AppendLine("                <p>No search results available.</p>");
        }
        
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GetCss()
    {
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f5f5;
            padding: 20px;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
            background-color: white;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        h1 {
            color: #2c3e50;
            margin-bottom: 30px;
            font-size: 2.5em;
            border-bottom: 3px solid #3498db;
            padding-bottom: 10px;
        }

        h2 {
            color: #34495e;
            margin-bottom: 15px;
            font-size: 1.5em;
        }

        h3 {
            color: #2980b9;
            font-size: 1.1em;
            margin-bottom: 8px;
        }

        .metadata {
            background-color: #ecf0f1;
            padding: 20px;
            border-radius: 6px;
            margin-bottom: 30px;
            display: flex;
            gap: 30px;
            flex-wrap: wrap;
        }

        .metadata-item {
            font-size: 0.95em;
        }

        .label {
            font-weight: 600;
            color: #555;
        }

        .section {
            margin-bottom: 40px;
        }

        .card {
            background-color: #fff;
            border: 1px solid #ddd;
            border-radius: 6px;
            padding: 20px;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
        }

        .card p {
            margin-bottom: 12px;
        }

        .card p:last-child {
            margin-bottom: 0;
        }

        .analysis-text {
            white-space: pre-wrap;
        }

        .screenshot-container {
            text-align: center;
            margin: 20px 0;
        }

        .screenshot {
            max-width: 100%;
            height: auto;
            border: 1px solid #ddd;
            border-radius: 6px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        .recommendation-card {
            margin-bottom: 20px;
        }

        .recommendation-card:last-child {
            margin-bottom: 0;
        }

        .recommendation-header {
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 15px;
        }

        .priority-badge {
            display: inline-block;
            padding: 6px 12px;
            border-radius: 4px;
            font-size: 0.85em;
            font-weight: 600;
            text-transform: uppercase;
            color: white;
        }

        .priority-high {
            background-color: #e74c3c;
        }

        .priority-medium {
            background-color: #f39c12;
        }

        .priority-low {
            background-color: #27ae60;
        }

        .recommendation-action {
            font-weight: 600;
            font-size: 1.1em;
            color: #2c3e50;
        }

        .recommendation-content p {
            margin-bottom: 10px;
        }

        .reference-link {
            font-size: 0.9em;
        }

        .reference-link a {
            color: #3498db;
            text-decoration: none;
        }

        .reference-link a:hover {
            text-decoration: underline;
        }

        .search-results-section {
            border-top: 2px solid #ecf0f1;
            padding-top: 30px;
            margin-top: 50px;
        }

        .search-results-section h2 {
            color: #7f8c8d;
            font-size: 1.3em;
        }

        .search-results {
            background-color: #fafafa;
            padding: 20px;
            border-radius: 6px;
        }

        .search-result-item {
            margin-bottom: 25px;
            padding-bottom: 20px;
            border-bottom: 1px solid #e0e0e0;
        }

        .search-result-item:last-child {
            border-bottom: none;
            margin-bottom: 0;
            padding-bottom: 0;
        }

        .search-result-item h3 a {
            color: #2980b9;
            text-decoration: none;
        }

        .search-result-item h3 a:hover {
            text-decoration: underline;
        }

        .search-snippet {
            color: #555;
            font-size: 0.95em;
            margin: 8px 0;
        }

        .search-url {
            color: #27ae60;
            font-size: 0.85em;
            word-break: break-all;
        }

        @media (max-width: 768px) {
            .container {
                padding: 20px;
            }

            h1 {
                font-size: 2em;
            }

            .metadata {
                flex-direction: column;
                gap: 10px;
            }
        }";
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        // Remove invalid file name characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        
        // Trim and limit length
        sanitized = sanitized.Trim();
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);
        
        return sanitized;
    }

    private string GetImageExtension(string mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".png" // Default to PNG
        };
    }

    private string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        
        return HttpUtility.HtmlEncode(text);
    }
}

