using System.Text.Json;
using Mentor.Core.Tests.RuleEvaluation.Models;
using Mentor.Core.Tests.RuleEvaluation.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentor.Core.Tests.RuleEvaluation.Tests;

/// <summary>
/// Tests for WikiContentExtractorService - critical for accurate rule extraction
/// </summary>
public class WikiContentExtractorServiceTests
{
    private readonly WikiContentExtractorService _service;
    private readonly Mock<ILogger<WikiContentExtractorService>> _loggerMock;
    private const string WikiTestDataPath = "Tests/Data/Wiki";

    public WikiContentExtractorServiceTests()
    {
        _loggerMock = new Mock<ILogger<WikiContentExtractorService>>();
        var httpClient = new HttpClient();
        _service = new WikiContentExtractorService(httpClient, _loggerMock.Object);
    }

    /// <summary>
    /// Model for expected characteristics from JSON files
    /// </summary>
    public class WikiTestExpectations
    {
        public List<ExpectedCharacteristic> Expected { get; set; } = new();
        public string[] Unexpected { get; set; } = Array.Empty<string>();
    }

    public class ExpectedCharacteristic
    {
        public string Text { get; set; } = string.Empty;
        public List<ExpectedCharacteristic>? Children { get; set; }
    }

    /// <summary>
    /// Discovers and loads wiki test files from the test data directory
    /// </summary>
    public static IEnumerable<object[]> GetWikiTestFiles()
    {
        var testDataPath = Path.Combine(Directory.GetCurrentDirectory(), WikiTestDataPath);
        
        if (!Directory.Exists(testDataPath))
        {
            yield break;
        }

        var txtFiles = Directory.GetFiles(testDataPath, "*.txt");
        
        foreach (var txtFile in txtFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(txtFile);
            var expectedFile = Path.Combine(testDataPath, $"{fileName}.expected.json");
            
            if (!File.Exists(expectedFile))
            {
                continue; // Skip if no expected file
            }

            var wikitext = File.ReadAllText(txtFile);
            var expectedJson = File.ReadAllText(expectedFile);
            var expectations = JsonSerializer.Deserialize<WikiTestExpectations>(expectedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (expectations?.Expected != null)
            {
                yield return new object[] { fileName, wikitext, expectations.Expected, expectations.Unexpected ?? Array.Empty<string>() };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetWikiTestFiles))]
    public void ExtractCharacteristics_WithRealWikiContent_ExtractsExpectedCharacteristics(
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        string fileName,
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        string wikitext, 
        List<ExpectedCharacteristic> expectedCharacteristics,
        string[] unexpectedCharacteristics)
    {
        // Act
        var result = _service.ExtractCharacteristics(wikitext);

        // Assert - Structure matches expected
        Assert.Equal(expectedCharacteristics.Count, result.Count);
        
        for (int i = 0; i < expectedCharacteristics.Count; i++)
        {
            AssertCharacteristicMatches(expectedCharacteristics[i], result[i]);
        }

        // Assert - Unexpected text should NOT appear anywhere in the tree
        var allExtractedText = FlattenAllText(result);
        foreach (var unexpected in unexpectedCharacteristics)
        {
            Assert.DoesNotContain(allExtractedText, text => text.Contains(unexpected));
        }
    }

    /// <summary>
    /// Recursively verify that a characteristic matches the expected structure
    /// </summary>
    private void AssertCharacteristicMatches(ExpectedCharacteristic expected, WikiContent actual)
    {
        Assert.Equal(expected.Text, actual.Text);
        
        var expectedChildCount = expected.Children?.Count ?? 0;
        Assert.Equal(expectedChildCount, actual.Children.Count);

        if (expected.Children != null)
        {
            for (int i = 0; i < expected.Children.Count; i++)
            {
                AssertCharacteristicMatches(expected.Children[i], actual.Children[i]);
            }
        }
    }

    /// <summary>
    /// Flatten all text from hierarchical structure for unexpected text checks
    /// </summary>
    private List<string> FlattenAllText(List<WikiContent> characteristics)
    {
        var allText = new List<string>();
        
        void Flatten(WikiContent characteristic)
        {
            allText.Add(characteristic.Text);
            foreach (var child in characteristic.Children)
            {
                Flatten(child);
            }
        }

        foreach (var characteristic in characteristics)
        {
            Flatten(characteristic);
        }

        return allText;
    }


    [Theory]
    [InlineData("https://wiki.warframe.com/w/Cedo_Prime", "Cedo_Prime")]
    [InlineData("https://wiki.warframe.com/wiki/Acceltra_Prime", "Acceltra_Prime")]
    [InlineData("https://wiki.warframe.com/w/Phantasma", "Phantasma")]
    [InlineData("https://wiki.warframe.com/w/Weapon_Name?query=param", "Weapon_Name")]
    [InlineData("https://wiki.warframe.com/wiki/Item#section", "Item")]
    public void ExtractPageNameFromUrl_WithValidUrls_ExtractsCorrectly(string url, string expected)
    {
        // Act
        var result = _service.ExtractPageNameFromUrl(url);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractPageNameFromUrl_WithInvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidUrl = "https://invalid.com/page";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ExtractPageNameFromUrl(invalidUrl));
    }
}

