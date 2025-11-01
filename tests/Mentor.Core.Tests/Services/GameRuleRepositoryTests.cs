using System.Text.Json;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mentor.Core.Tests.Services;

public class GameRuleRepositoryTests : IDisposable
{
    private readonly Mock<ILogger<GameRuleRepository>> _mockLogger;
    private readonly Mock<IUserDataPathService> _mockUserDataPathService;
    private readonly GameRuleRepository _repository;
    private readonly string _testRulesDirectory;
    private readonly string _testRulesFilePath;

    public GameRuleRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<GameRuleRepository>>();
        _mockUserDataPathService = new Mock<IUserDataPathService>();
        
        // Set up a temporary test directory
        _testRulesDirectory = Path.Combine(Path.GetTempPath(), "MentorTest_" + Guid.NewGuid());
        _testRulesFilePath = Path.Combine(_testRulesDirectory, "WarframeRules.json");
        
        // Create the test directory and sample rules file
        Directory.CreateDirectory(_testRulesDirectory);
        CreateTestRulesFile();
        
        // Mock the path service to return our test directory (case-insensitive)
        _mockUserDataPathService
            .Setup(x => x.GetRulesPath(It.IsAny<string>()))
            .Returns(_testRulesDirectory);
        
        // Mock EnsureDirectoryExists to actually create the directory
        _mockUserDataPathService
            .Setup(x => x.EnsureDirectoryExists(It.IsAny<string>()))
            .Callback<string>(path => Directory.CreateDirectory(path));
        
        _repository = new GameRuleRepository(_mockLogger.Object, _mockUserDataPathService.Object);
    }

    private void CreateTestRulesFile()
    {
        var testRules = new List<GameRule>
        {
            new GameRule
            {
                RuleId = "wf-test-001",
                RuleText = "Test rule for status mechanics",
                Category = "StatusMechanics"
            },
            new GameRule
            {
                RuleId = "wf-test-002",
                RuleText = "Test rule for damage types",
                Category = "DamageTypes"
            }
        };

        var json = JsonSerializer.Serialize(testRules, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(_testRulesFilePath, json);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testRulesDirectory))
        {
            Directory.Delete(_testRulesDirectory, true);
        }
    }

    [Fact]
    public async Task LoadRulesAsync_ReturnsRules_FromFileSystem()
    {
        // Act
        var rules = await _repository.LoadRulesAsync("warframe", new List<string> { "WarframeRules" });

        // Assert
        Assert.NotNull(rules);
        Assert.NotEmpty(rules);
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public async Task LoadRulesAsync_AllRulesHaveRequiredProperties()
    {
        // Act
        var rules = await _repository.LoadRulesAsync("warframe", new List<string> { "WarframeRules" });

        // Assert
        Assert.All(rules, rule => 
        {
            Assert.NotEmpty(rule.RuleId);
            Assert.NotEmpty(rule.RuleText);
            Assert.NotEmpty(rule.Category);
        });
    }

    [Fact]
    public async Task LoadRulesAsync_ReturnsEmptyList_WhenNoRuleFilesSpecified()
    {
        // Act
        var rules = await _repository.LoadRulesAsync("warframe", new List<string>());

        // Assert - should return empty list when no rule files specified
        Assert.NotNull(rules);
        Assert.Empty(rules);
    }

    [Fact]
    public async Task GetRulesForGameAsync_ReturnsAllRules()
    {
        // Act
        var rules = await _repository.GetRulesForGameAsync("Warframe", new List<string> { "WarframeRules" });

        // Assert
        Assert.NotEmpty(rules);
    }

    [Fact]
    public async Task GetRulesForGameAsync_IgnoresGameNameParameter()
    {
        // Act - GameName parameter is now ignored since the field was removed
        var ruleFiles = new List<string> { "WarframeRules" };
        var warframeRules = await _repository.GetRulesForGameAsync("Warframe", ruleFiles);
        var otherRules = await _repository.GetRulesForGameAsync("SomeOtherGame", ruleFiles);

        // Assert - both calls return the same rules
        Assert.Equal(warframeRules.Count, otherRules.Count);
    }

    [Fact]
    public async Task GetFormattedRulesAsync_ReturnsFormattedString()
    {
        // Act
        var formatted = await _repository.GetFormattedRulesAsync("Warframe", new List<string> { "WarframeRules" });

        // Assert
        Assert.NotNull(formatted);
        Assert.NotEmpty(formatted);
        Assert.Contains("GAME KNOWLEDGE RULES", formatted);
    }

    [Fact]
    public async Task GetFormattedRulesAsync_ContainsRuleContent()
    {
        // Act
        var formatted = await _repository.GetFormattedRulesAsync("Warframe", new List<string> { "WarframeRules" });

        // Assert
        Assert.Contains("status", formatted.ToLower());
        Assert.Contains("damage", formatted.ToLower());
    }

    [Fact]
    public async Task GetFormattedRulesAsync_ReturnsRulesRegardlessOfGameName()
    {
        // Act - GameName parameter is now ignored since the field was removed
        var formatted = await _repository.GetFormattedRulesAsync("UnknownGame", new List<string> { "WarframeRules" });

        // Assert - returns rules anyway
        Assert.NotEmpty(formatted);
        Assert.Contains("GAME KNOWLEDGE RULES", formatted);
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenGameNameIsNull()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-001", RuleText = "Test rule", Category = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync(null!, "weapons", "testweapon", rules)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenGameNameIsEmpty()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-001", RuleText = "Test rule", Category = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync(string.Empty, "weapons", "testweapon", rules)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenWeaponNameIsNull()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-001", RuleText = "Test rule", Category = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync("warframe", "weapons", null!, rules)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenWeaponNameIsEmpty()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-001", RuleText = "Test rule", Category = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync("warframe", "weapons", string.Empty, rules)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenRulesIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync("warframe", "weapons", "testweapon", null!)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenRulesIsEmpty()
    {
        // Arrange
        var rules = new List<GameRule>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync("warframe", "weapons", "testweapon", rules)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_SavesRulesToCorrectLocation()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-003", RuleText = "New test rule", Category = "Test" }
        };

        _mockUserDataPathService
            .Setup(x => x.GetRulesPath("testgame"))
            .Returns(_testRulesDirectory);

        // Act
        await _repository.SaveRulesAsync("testgame", "weapons", "testweapon", rules);

        // Assert
        var savedFilePath = Path.Combine(_testRulesDirectory, "weapons", "testweapon.json");
        Assert.True(File.Exists(savedFilePath));

        var savedJson = await File.ReadAllTextAsync(savedFilePath);
        var savedRules = JsonSerializer.Deserialize<List<GameRule>>(savedJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(savedRules);
        Assert.Single(savedRules);
        Assert.Equal("test-003", savedRules[0].RuleId);
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenTypeIsNull()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-001", RuleText = "Test rule", Category = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync("warframe", null!, "testweapon", rules)
        );
    }

    [Fact]
    public async Task SaveRulesAsync_ThrowsArgumentException_WhenTypeIsEmpty()
    {
        // Arrange
        var rules = new List<GameRule>
        {
            new() { RuleId = "test-001", RuleText = "Test rule", Category = "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.SaveRulesAsync("warframe", string.Empty, "testweapon", rules)
        );
    }

    [Fact]
    public async Task LoadRulesAsync_FindsRulesInSubfolders()
    {
        // Arrange - create a subfolder structure
        var weaponsDir = Path.Combine(_testRulesDirectory, "weapons");
        Directory.CreateDirectory(weaponsDir);
        
        var testRules = new List<GameRule>
        {
            new() { RuleId = "wf-weapon-001", RuleText = "Weapon test rule", Category = "Weapons" }
        };

        var json = JsonSerializer.Serialize(testRules, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(Path.Combine(weaponsDir, "Acceltra.json"), json);

        // Act
        var rules = await _repository.LoadRulesAsync("warframe", new List<string> { "Acceltra" });

        // Assert
        Assert.NotNull(rules);
        Assert.Single(rules);
        Assert.Equal("wf-weapon-001", rules[0].RuleId);
    }
}

