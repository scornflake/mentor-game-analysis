using Mentor.Core.Tools;

namespace Mentor.Core.Services;

public class SimpleHtmlToMarkdownConverter : IHtmlToMarkdownConverter
{
    private readonly IHtmlTextExtractor _extractor;

    public SimpleHtmlToMarkdownConverter(IHtmlTextExtractor extractor)
    {
        _extractor = extractor;
    }

    public async Task<string> ConvertAsync(string htmlContent, CancellationToken cancellationToken = default)
    {
        return await _extractor.ExtractTextAsync(htmlContent, cancellationToken);
    }
}