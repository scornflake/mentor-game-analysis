using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Mentor.Core.Data;
using Mentor.Core.Services;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class ArticleReader : IArticleReader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArticleReader> _logger;
    private ToolConfigurationEntity _config = new ToolConfigurationEntity();

    public ArticleReader(
        IHttpClientFactory httpClientFactory,
        ILogger<ArticleReader> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    public void Configure(ToolConfigurationEntity configuration)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> ReadArticleAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        _logger.LogInformation("Fetching article content from URL: {Url}", url);

        // Fetch HTML content
        var htmlContent = await FetchHtmlContentAsync(url, cancellationToken);

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning("No content retrieved from URL: {Url}", url);
            return "Unable to retrieve article content from the provided URL.";
        }

        // Parse HTML and extract main content
        var mainContent = await ExtractMainContentAsync(htmlContent, cancellationToken);

        if (string.IsNullOrWhiteSpace(mainContent))
        {
            _logger.LogWarning("No main content extracted from URL: {Url}", url);
            return "Unable to extract main content from the article.";
        }

        _logger.LogInformation("Successfully extracted article content from URL: {Url}", url);

        return mainContent;
    }

    private async Task<string> FetchHtmlContentAsync(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);

        // Set a user agent to avoid being blocked
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<string> ExtractMainContentAsync(string htmlContent, CancellationToken cancellationToken)
    {
        var config = AngleSharp.Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(htmlContent), cancellationToken);

        // Remove unwanted elements
        RemoveUnwantedElements(document);

        // Try to find main content using semantic HTML5 elements first
        var mainContent = document.QuerySelector("article")
                          ?? document.QuerySelector("main")
                          ?? document.QuerySelector("[role='main']");

        if (mainContent != null)
        {
            return mainContent.InnerHtml;
        }

        // Fallback: Try common content container classes/ids
        var contentSelectors = new[]
        {
            ".content",
            ".article-content",
            ".post-content",
            ".entry-content",
            "#content",
            "#main-content",
            "#article-content",
            ".article-body",
            ".post-body"
        };

        foreach (var selector in contentSelectors)
        {
            var element = document.QuerySelector(selector);
            if (element != null)
            {
                return element.InnerHtml;
            }
        }

        // Last resort: Use body content but remove navigation, footer, etc.
        var body = document.Body;
        if (body != null)
        {
            // Remove common non-content elements
            body.QuerySelectorAll("nav, header, footer, aside, .sidebar, .navigation, .menu, .footer, .header").ToList()
                .ForEach(el => el.Remove());

            return body.InnerHtml;
        }

        return string.Empty;
    }

    private void RemoveUnwantedElements(IDocument document)
    {
        // Remove scripts, styles, and other non-content elements
        var elementsToRemove = document.QuerySelectorAll(
            "script, style, noscript, iframe, embed, object, " +
            "nav, header, footer, aside, .sidebar, .navigation, .menu, " +
            ".advertisement, .ad, .ads, .sponsored, .promo, " +
            ".social-share, .share-buttons, .comments, .comment-section, " +
            ".related-articles, .related-posts, .newsletter, .subscribe"
        );

        foreach (var element in elementsToRemove)
        {
            element.Remove();
        }
    }
}