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
            Category = "StatusMechanics"
        };

        // Assert
        Assert.NotNull(rule.RuleId);
        Assert.Equal("Cedo Prime has 100% status chance - prioritize status mods", rule.RuleText);
        Assert.Equal("StatusMechanics", rule.Category);
        Assert.NotNull(rule.Children);
        Assert.Empty(rule.Children);
    }

    [Fact]
    public void GameRule_CanBeSerialized_ToJson()
    {
        // Arrange
        var rule = new GameRule
        {
            RuleId = "test-rule-123",
            RuleText = "Hunter Munitions synergizes with high crit weapons",
            Category = "Synergies"
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
            "Category": "DamageTypes"
        }
        """;

        // Act
        var rule = JsonSerializer.Deserialize<GameRule>(json);

        // Assert
        Assert.NotNull(rule);
        Assert.Equal("test-rule-456", rule.RuleId);
        Assert.Equal("Corrosive damage is effective against Grineer armor", rule.RuleText);
        Assert.Equal("DamageTypes", rule.Category);
    }

    [Fact]
    public void GameRule_CanHaveChildren()
    {
        // Arrange & Act
        var parent = new GameRule
        {
            RuleId = "parent-rule",
            RuleText = "Primary fire shoots beams",
            Category = "WeaponSpecific"
        };

        var child1 = new GameRule
        {
            RuleId = "child-rule-1",
            RuleText = "Pinpoint accuracy",
            Category = "WeaponSpecific"
        };

        var child2 = new GameRule
        {
            RuleId = "child-rule-2",
            RuleText = "Innate multishot",
            Category = "WeaponSpecific"
        };

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        // Assert
        Assert.Equal(2, parent.Children.Count);
        Assert.Equal("child-rule-1", parent.Children[0].RuleId);
        Assert.Equal("child-rule-2", parent.Children[1].RuleId);
    }

    [Fact]
    public void GameRule_HierarchicalStructure_CanBeSerialized()
    {
        // Arrange
        var parent = new GameRule
        {
            RuleId = "wf-weapon-001",
            RuleText = "Primary fire shoots continuous beams",
            Category = "WeaponSpecific",
            Children = new List<GameRule>
            {
                new GameRule
                {
                    RuleId = "wf-weapon-001-a",
                    RuleText = "Pinpoint accuracy",
                    Category = "WeaponSpecific"
                },
                new GameRule
                {
                    RuleId = "wf-weapon-001-b",
                    RuleText = "Innate multishot of 6 beams",
                    Category = "WeaponSpecific"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(parent, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<GameRule>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("wf-weapon-001", deserialized.RuleId);
        Assert.Equal(2, deserialized.Children.Count);
        Assert.Equal("wf-weapon-001-a", deserialized.Children[0].RuleId);
        Assert.Equal("Pinpoint accuracy", deserialized.Children[0].RuleText);
    }
}

