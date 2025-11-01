using Mentor.Core.Models;
using Mentor.Core.Tools;

namespace Mentor.Core.Tests.Tools;

public class SearchResultFormatterTests
{
    [Fact]
    public void FormatAsText_WithEmptyList_ReturnsEmptyString()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>();

        // Act
        var formatted = formatter.FormatAsText(results);

        // Assert
        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatAsText_WithNullList_ReturnsEmptyString()
    {
        // Arrange
        var formatter = new SearchResultFormatter();

        // Act
        var formatted = formatter.FormatAsText(null!);

        // Assert
        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatAsText_WithSingleResult_ReturnsConcatenatedContent()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult
            {
                Title = "Test Title",
                Url = "https://example.com",
                Content = "This is test content."
            }
        };

        // Act
        var formatted = formatter.FormatAsText(results);

        // Assert
        Assert.Equal("This is test content.", formatted);
    }

    [Fact]
    public void FormatAsText_WithMultipleResults_ReturnsConcatenatedContent()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult { Content = "First content." },
            new SearchResult { Content = "Second content." },
            new SearchResult { Content = "Third content." }
        };

        // Act
        var formatted = formatter.FormatAsText(results);

        // Assert
        Assert.Contains("First content.", formatted);
        Assert.Contains("Second content.", formatted);
        Assert.Contains("Third content.", formatted);
    }

    [Fact]
    public void FormatAsText_SkipsEmptyContent()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult { Content = "First content." },
            new SearchResult { Content = "" },
            new SearchResult { Content = "Third content." }
        };

        // Act
        var formatted = formatter.FormatAsText(results);

        // Assert
        Assert.Contains("First content.", formatted);
        Assert.Contains("Third content.", formatted);
        Assert.DoesNotContain("\n\n", formatted); // No double newlines from empty content
    }

    [Fact]
    public void FormatAsSummary_WithEmptyList_ReturnsEmptyString()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>();

        // Act
        var formatted = formatter.FormatAsSummary(results);

        // Assert
        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatAsSummary_WithNullList_ReturnsEmptyString()
    {
        // Arrange
        var formatter = new SearchResultFormatter();

        // Act
        var formatted = formatter.FormatAsSummary(null!);

        // Assert
        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormatAsSummary_WithResults_IncludesResultCount()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult { Content = "First content." },
            new SearchResult { Content = "Second content." }
        };

        // Act
        var formatted = formatter.FormatAsSummary(results);

        // Assert
        Assert.Contains("Found 2 result(s):", formatted);
    }

    [Fact]
    public void FormatAsSummary_WithAdditionalContext_IncludesContextFirst()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult { Content = "Result content." }
        };
        var additionalContext = "This is additional context from the search provider.";

        // Act
        var formatted = formatter.FormatAsSummary(results, additionalContext);

        // Assert
        Assert.StartsWith(additionalContext, formatted);
        Assert.Contains("Result content.", formatted);
    }

    [Fact]
    public void FormatAsSummary_WithNoAdditionalContext_DoesNotIncludeEmptyContext()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult { Content = "Result content." }
        };

        // Act
        var formatted = formatter.FormatAsSummary(results);

        // Assert
        Assert.DoesNotContain("\n\n\n", formatted); // No triple newlines
        Assert.StartsWith("Found", formatted);
    }

    [Fact]
    public void FormatAsSummary_DoesNotTruncateShortContent()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var shortContent = "This is short content.";
        var results = new List<SearchResult>
        {
            new SearchResult { Content = shortContent }
        };

        // Act
        var formatted = formatter.FormatAsSummary(results);

        // Assert
        Assert.Contains(shortContent, formatted);
        Assert.DoesNotContain("...", formatted); // Should not have ellipsis for short content
    }

    [Fact]
    public void FormatAsSummary_SkipsEmptyContent()
    {
        // Arrange
        var formatter = new SearchResultFormatter();
        var results = new List<SearchResult>
        {
            new SearchResult { Content = "First content." },
            new SearchResult { Content = "" },
            new SearchResult { Content = "Third content." }
        };

        // Act
        var formatted = formatter.FormatAsSummary(results);

        // Assert
        Assert.Contains("Found 3 result(s):", formatted); // Count includes all results
        Assert.Contains("First content.", formatted);
        Assert.Contains("Third content.", formatted);
    }
}

