using System.Text.Json;
using Mentor.Core.Models;

namespace Mentor.Core.Tests.Models;

public class GameRuleTests
{
    [Fact]
    public void GameRule_CanBeCreated_WithAllProperties()
    {
        // Arrange & Act
        var rule = new GameRule
        {
            RuleId = Guid.NewGuid().ToString(),
            RuleText = "Cedo Prime has 100% status chance - prioritize status mods",
            Category = "StatusMechanics",
            Confidence = 0.95
        };

        // Assert
        Assert.NotNull(rule.RuleId);
        Assert.Equal("Cedo Prime has 100% status chance - prioritize status mods", rule.RuleText);
        Assert.Equal("StatusMechanics", rule.Category);
        Assert.Equal(0.95, rule.Confidence);
    }

    [Fact]
    public void GameRule_CanBeSerialized_ToJson()
    {
        // Arrange
        var rule = new GameRule
        {
            RuleId = "test-rule-123",
            RuleText = "Hunter Munitions synergizes with high crit weapons",
            Category = "Synergies",
            Confidence = 0.85
        };

        // Act
        var json = JsonSerializer.Serialize(rule);

        // Assert
        Assert.Contains("test-rule-123", json);
        Assert.Contains("Hunter Munitions", json);
        Assert.Contains("Synergies", json);
    }

    [Fact]
    public void GameRule_CanBeDeserialized_FromJson()
    {
        // Arrange
        var json = """
        {
            "RuleId": "test-rule-456",
            "RuleText": "Corrosive damage is effective against Grineer armor",
            "Category": "DamageTypes",
            "Confidence": 0.9
        }
        """;

        // Act
        var rule = JsonSerializer.Deserialize<GameRule>(json);

        // Assert
        Assert.NotNull(rule);
        Assert.Equal("test-rule-456", rule.RuleId);
        Assert.Equal("Corrosive damage is effective against Grineer armor", rule.RuleText);
        Assert.Equal("DamageTypes", rule.Category);
        Assert.Equal(0.9, rule.Confidence);
    }

    [Fact]
    public void GameRule_DefaultConfidence_IsZero()
    {
        // Arrange & Act
        var rule = new GameRule();

        // Assert
        Assert.Equal(0.0, rule.Confidence);
    }
}

