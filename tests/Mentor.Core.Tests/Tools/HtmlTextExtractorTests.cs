using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tests.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Tools;

public class HtmlTextExtractorTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILogger<HtmlTextExtractor> _logger;

    public HtmlTextExtractorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        var serviceProvider = services.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<HtmlTextExtractor>>();
    }

    private string LoadTestHtml(string filename)
    {
        var projectRoot = ApiKeyHelper.FindProjectRoot(AppContext.BaseDirectory);
        if (projectRoot == null)
        {
            throw new InvalidOperationException("Could not find project root");
        }

        var htmlPath = Path.Combine(projectRoot, "tests", "media", filename);
        if (!File.Exists(htmlPath))
        {
            throw new FileNotFoundException($"Test HTML file not found: {htmlPath}");
        }

        return File.ReadAllText(htmlPath);
    }

    #region Property-Based Tests

    [Fact]
    public async Task ExtractTextAsync_ShouldRemoveScriptTags()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"
            <html>
                <head><script>alert('test');</script></head>
                <body>
                    <p>Main content here</p>
                    <script>console.log('another');</script>
                </body>
            </html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.DoesNotContain("alert", result);
        Assert.DoesNotContain("console.log", result);
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("Main content here", result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldRemoveStyleTags()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"
            <html>
                <head><style>.class { color: red; }</style></head>
                <body>
                    <p>Main content here</p>
                    <style>.another { display: none; }</style>
                </body>
            </html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.DoesNotContain("color: red", result);
        Assert.DoesNotContain("display: none", result);
        Assert.DoesNotContain("<style>", result);
        Assert.Contains("Main content here", result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldRemoveHtmlTags()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"<html><body><p>Hello <strong>world</strong>!</p></body></html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.DoesNotContain("<p>", result);
        Assert.DoesNotContain("<strong>", result);
        Assert.DoesNotContain("</p>", result);
        Assert.DoesNotContain("</strong>", result);
        Assert.Contains("Hello", result);
        Assert.Contains("world", result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldNotReturnEmptyForValidContent()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"<html><body><p>Some content</p></body></html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldRemoveNavigationElements()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"
            <html>
                <body>
                    <nav><a href='/home'>Home</a><a href='/about'>About</a></nav>
                    <header><h1>Site Header</h1></header>
                    <main><p>Main content here</p></main>
                    <footer>Footer content</footer>
                    <aside>Sidebar content</aside>
                </body>
            </html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.DoesNotContain("Home", result);
        Assert.DoesNotContain("About", result);
        Assert.DoesNotContain("Site Header", result);
        Assert.DoesNotContain("Footer content", result);
        Assert.DoesNotContain("Sidebar content", result);
        Assert.Contains("Main content here", result);
    }

    #endregion

    #region Manual Assertion Tests

    [Fact]
    public async Task ExtractTextAsync_WithProblematicHtml2_ShouldExtractMainContent()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = LoadTestHtml("reddit_post_1.html");

        // Act
        var result = await extractor.ExtractTextAsync(html);
        
        // Assert
        // Assert.Contains("Whats more effective against grineer", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Go to Warframe", result);
        Assert.DoesNotContain("Share", result);
        Assert.DoesNotContain("alert(", result);
        Assert.DoesNotContain("console.log", result);
        Assert.DoesNotContain("<shreddit", result);

        _testOutputHelper.WriteLine("Extracted text length: " + result.Length);
        _testOutputHelper.WriteLine("First 500 chars: " + (result.Length > 500 ? result.Substring(0, 500) : result));
    }

    [Fact]
    public async Task ExtractTextAsync_WithProblematicHtml1_ShouldExtractMainContent()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = LoadTestHtml("problematic_html1.html");

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert - Should contain the main post title
        Assert.Contains("Whats more effective against grineer", result, StringComparison.OrdinalIgnoreCase);
        
        // Assert - Should NOT contain navigation/UI elements
        Assert.DoesNotContain("Go to Warframe", result);
        Assert.DoesNotContain("Share", result);
        
        // Assert - Should NOT contain scripts or style content
        Assert.DoesNotContain("alert(", result);
        Assert.DoesNotContain("console.log", result);
        Assert.DoesNotContain("<shreddit", result);
        
        _testOutputHelper.WriteLine("Extracted text length: " + result.Length);
        _testOutputHelper.WriteLine("First 500 chars: " + (result.Length > 500 ? result.Substring(0, 500) : result));
    }

    [Fact]
    public async Task ExtractTextAsync_WithSimpleHtml_ShouldExtractTitle()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"
            <html>
                <head><title>Test Title</title></head>
                <body>
                    <h1>Main Heading</h1>
                    <p>This is the main content of the article.</p>
                </body>
            </html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.Contains("Main Heading", result);
        Assert.Contains("main content of the article", result);
    }

    [Fact]
    public async Task ExtractTextAsync_WithAdsAndComments_ShouldRemoveThem()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"
            <html>
                <body>
                    <div class='advertisement'>Buy our product!</div>
                    <div id='sidebar-ad'>Click here!</div>
                    <article>Real content here</article>
                    <div class='comments-section'>User comment</div>
                    <div class='social-share'>Share on Facebook</div>
                </body>
            </html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.DoesNotContain("Buy our product", result);
        Assert.DoesNotContain("Click here", result);
        Assert.DoesNotContain("User comment", result);
        Assert.DoesNotContain("Share on Facebook", result);
        Assert.Contains("Real content here", result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExtractTextAsync_WithNullInput_ShouldThrowArgumentException()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            extractor.ExtractTextAsync(null!));
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            extractor.ExtractTextAsync(string.Empty));
    }

    [Fact]
    public async Task ExtractTextAsync_WithWhitespaceOnly_ShouldThrowArgumentException()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            extractor.ExtractTextAsync("   "));
    }

    [Fact]
    public async Task ExtractTextAsync_WithMalformedHtml_ShouldStillExtractText()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"<html><body><p>Unclosed paragraph<div>Content</p></div></body>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Unclosed paragraph", result);
        Assert.Contains("Content", result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldCollapseMultipleWhitespaces()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"<html><body><p>Text   with    multiple     spaces</p></body></html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.DoesNotContain("   ", result); // Should not have triple spaces
        Assert.Contains("Text with multiple spaces", result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldHandleNestedElements()
    {
        // Arrange
        var extractor = new HtmlTextExtractor(_logger);
        var html = @"
            <html>
                <body>
                    <div>
                        <div>
                            <div>
                                <p>Deeply nested content</p>
                            </div>
                        </div>
                    </div>
                </body>
            </html>";

        // Act
        var result = await extractor.ExtractTextAsync(html);

        // Assert
        Assert.Contains("Deeply nested content", result);
    }

    #endregion
}

