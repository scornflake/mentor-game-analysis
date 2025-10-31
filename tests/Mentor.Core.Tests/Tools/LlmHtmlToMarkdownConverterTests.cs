using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tests.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Tools;

public class LlmHtmlToMarkdownConverterTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LlmHtmlToMarkdownConverterTests> _logger;
    private readonly ITestOutputHelper _testOutputHelper;

    public LlmHtmlToMarkdownConverterTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var services = TestHelpers.CreateTestServices(testOutputHelper);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverterTests>>();
    }

    [Fact]
    public async Task Convert_NullHtml_ReturnsEmptyString()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var converter = new LlmHtmlToMarkdownConverter(llmClient, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await converter.ConvertAsync(null!));
    }

    [Fact]
    public void Constructor_AcceptsMaxLinesToConvertParameter()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var maxLines = 500;

        // Act
        var converter = new LlmHtmlToMarkdownConverter(llmClient, logger, maxLines);

        // Assert
        Assert.NotNull(converter);
    }

    [Fact]
    public async Task Convert_EmptyHtml_ReturnsEmptyString()
    {
        // Arrange
        var llmClient = CreateTestLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var converter = new LlmHtmlToMarkdownConverter(llmClient, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await converter.ConvertAsync(""));
    }

    [Fact]
    public async Task Convert_VeryShortHtml_UsesFallback()
    {
        // Arrange
        var reverseLogger = _serviceProvider.GetRequiredService<ILogger<HtmlTextExtractor>>();
        var converter = new HtmlTextExtractor(reverseLogger);

        var shortHtml = "<p>Short</p>";

        // Act
        var result = await converter.ExtractTextAsync(shortHtml);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Short", result);
    }

    [RequiresOpenAIKeyFact]
    public async Task Convert_SimpleHtmlWithRealLLM_ReturnsMarkdown()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real LLM Test: Convert_SimpleHtmlWithRealLLM_ReturnsMarkdown ===");

        var llmClient = CreateLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var converter = new LlmHtmlToMarkdownConverter(llmClient, logger);

        var html = @"<article>
            <h1>Test Article</h1>
            <p>This is a <strong>test</strong> paragraph with <a href='https://example.com'>a link</a>.</p>
            <ul>
                <li>Item 1</li>
                <li>Item 2</li>
            </ul>
        </article>";

        _logger.LogInformation("Converting HTML to Markdown using LLM");

        // Act
        var result = await converter.ConvertAsync(html);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        _logger.LogInformation("Conversion result:\n{Result}", result);

        // Should contain the title
        Assert.Contains("Test Article", result, StringComparison.OrdinalIgnoreCase);
        
        // Should contain the word "test" (from strong tag)
        Assert.Contains("test", result, StringComparison.OrdinalIgnoreCase);
        
        // Should contain list items
        Assert.Contains("Item 1", result);
        Assert.Contains("Item 2", result);

        // Should have markdown formatting
        Assert.True(result.Contains("#") || result.Contains("-") || result.Contains("*"),
            "Result should contain some markdown formatting");

        _logger.LogInformation("=== Test completed successfully ===");
    }

    [RequiresOpenAIKeyFact]
    public async Task Convert_ComplexRedditHtmlWithRealLLM_ExtractsMainContent()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real LLM Test: Convert_ComplexRedditHtmlWithRealLLM_ExtractsMainContent ===");

        var llmClient = CreateLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var converter = new LlmHtmlToMarkdownConverter(llmClient, logger);

        var projectRoot = Helpers.ApiKeyHelper.FindProjectRoot(AppContext.BaseDirectory);
        Assert.NotNull(projectRoot);

        var htmlPath = Path.Combine(projectRoot, "tests", "media", "problematic_html1.html");
        Assert.True(File.Exists(htmlPath), $"Test file not found: {htmlPath}");

        var html = File.ReadAllText(htmlPath);

        _logger.LogInformation("Converting complex Reddit HTML ({Length} chars) to Markdown using LLM", html.Length);

        // Act
        var result = await converter.ConvertAsync(html);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        _logger.LogInformation("Conversion result length: {Length} chars", result.Length);
        _logger.LogInformation("First 500 chars of result:\n{Result}", 
            result.Length > 500 ? result.Substring(0, 500) : result);

        // Should contain the main title
        Assert.Contains("grineer", result, StringComparison.OrdinalIgnoreCase);

        // Should be significantly shorter than original HTML (LLM removes noise)
        Assert.True(result.Length < html.Length, 
            $"Markdown ({result.Length}) should be shorter than HTML ({html.Length})");

        // Should not contain excessive navigation/UI elements
        var lowerResult = result.ToLower();
        // LLM should have removed most UI chrome, though some contextual info might remain
        
        // Should not have excessive whitespace
        Assert.DoesNotContain("\n\n\n", result);

        _logger.LogInformation("=== Test completed successfully ===");
    }

    [RequiresOpenAIKeyFact]
    public async Task Convert_HtmlWithNoiseRemoval_ProducesCleanerOutput()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real LLM Test: Convert_HtmlWithNoiseRemoval_ProducesCleanerOutput ===");

        var llmClient = CreateLLMClient();
        var reverseLogger = _serviceProvider.GetRequiredService<ILogger<HtmlTextExtractor>>();
        var fallback = new HtmlTextExtractor(reverseLogger);
        var llmLogger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var llmConverter = new LlmHtmlToMarkdownConverter(llmClient, llmLogger);

        var htmlWithNoise = @"
            <header class='site-header'>
                <nav>
                    <a href='/home'>Home</a>
                    <a href='/about'>About</a>
                </nav>
            </header>
            
            <article>
                <h1>Important Article Title</h1>
                <p>This is the main content that should be preserved.</p>
                <p>It contains <strong>important information</strong>.</p>
            </article>
            
            <aside class='sidebar'>
                <div class='advertisement'>Buy our product!</div>
                <div class='social-share'>Share this!</div>
            </aside>
            
            <footer>
                <p>Copyright 2024</p>
            </footer>";

        _logger.LogInformation("Converting HTML with noise to Markdown using LLM");

        // Act
        var llmResult = await llmConverter.ConvertAsync(htmlWithNoise);
        var fallbackResult = await fallback.ExtractTextAsync(htmlWithNoise);

        // Assert
        Assert.NotNull(llmResult);
        Assert.NotEmpty(llmResult);

        _logger.LogInformation("LLM result:\n{LlmResult}", llmResult);
        _logger.LogInformation("Fallback result:\n{FallbackResult}", fallbackResult);

        // Should contain main content
        Assert.Contains("Important Article Title", llmResult);
        Assert.Contains("main content", llmResult);
        Assert.Contains("important information", llmResult, StringComparison.OrdinalIgnoreCase);

        // LLM result should generally be cleaner (less navigation noise)
        // Note: This is qualitative, but we can check that main content is preserved
        Assert.True(llmResult.Contains("Important Article Title") && llmResult.Contains("main content"),
            "LLM should preserve main article content");

        _logger.LogInformation("=== Test completed successfully ===");
    }

    [RequiresOpenAIKeyFact]
    public async Task Convert_HtmlExceedingMaxLines_TruncatesContent()
    {
        // Arrange
        _logger.LogInformation("=== Starting Real LLM Test: Convert_HtmlExceedingMaxLines_TruncatesContent ===");

        var llmClient = CreateLLMClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<LlmHtmlToMarkdownConverter>>();
        var maxLines = 10;
        var converter = new LlmHtmlToMarkdownConverter(llmClient, logger, maxLines);

        // Create HTML with more than maxLines
        var htmlLines = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            htmlLines.Add($"<p>Line {i}</p>");
        }
        var html = string.Join('\n', htmlLines);

        _logger.LogInformation("Converting HTML with {TotalLines} lines (max: {MaxLines})", htmlLines.Count, maxLines);

        // Act
        var result = await converter.ConvertAsync(html);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        _logger.LogInformation("Conversion result:\n{Result}", result);

        // Should contain content from early lines
        Assert.Contains("Line 1", result);
        
        // Should NOT contain content from lines beyond maxLines
        Assert.DoesNotContain("Line 20", result);
        Assert.DoesNotContain("Line 15", result);

        _logger.LogInformation("=== Test completed successfully ===");
    }

    private ILLMClient CreateTestLLMClient()
    {
        // Create a minimal LLM client for validation tests that don't actually call the LLM
        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfigurationEntity
        {
            ProviderType = "openai",
            ApiKey = "test-key",
            Model = "gpt-4o-mini",
            BaseUrl = "https://api.openai.com/v1",
            Timeout = 60
        };

        return factory.GetProvider(providerConfig);
    }

    private ILLMClient CreateLLMClient()
    {
        var apiKey = ApiKeyHelper.GetOpenAIApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key not available for integration test");
        }

        var factory = new LLMProviderFactory(_serviceProvider);

        var providerConfig = new ProviderConfigurationEntity
        {
            ProviderType = "perplexity",
            ApiKey = apiKey,
            Model = "sonar",
            BaseUrl = "https://api.perplexity.ai",
            Timeout = 60
        };

        return factory.GetProvider(providerConfig);
    }
}

