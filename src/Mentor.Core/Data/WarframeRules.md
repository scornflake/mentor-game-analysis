# Game Rules Reference

## Purpose

The `WarframeRules.json` file contains a curated set of domain-specific game knowledge rules that are injected into the LLM's system prompt when analyzing game screenshots. These rules provide the AI with expert-level game knowledge to generate more accurate and actionable recommendations.

## File Structure

The JSON file contains an array of `GameRule` objects with the following schema:

```json
{
  "RuleId": "unique-identifier",
  "GameName": "Warframe",
  "RuleText": "Actionable guidance or factual statement",
  "Category": "CategoryName",
  "Confidence": 0.95
}
```

### Field Descriptions

- **RuleId**: Unique identifier using format `{game-abbreviation}-{category-abbreviation}-{number}`
  - Example: `wf-status-001`, `wf-damage-002`
  - Use consistent prefixes for easy rule management
  
- **GameName**: The game this rule applies to
  - Must match exactly (case-insensitive comparison in code)
  - Example: `"Warframe"`
  
- **RuleText**: The actual rule content
  - Should be clear, concise, and actionable
  - Focus on "what" and "why", not just "how"
  - Include specific values and thresholds when relevant
  
- **Category**: Classification for grouping related rules
  - Rules are grouped and sorted by category in the system prompt
  - See [Categories](#categories) section below
  
- **Confidence**: Numeric value between 0.0 and 1.0
  - Indicates reliability/certainty of the rule
  - Higher confidence rules appear first within their category
  - See [Confidence Scoring](#confidence-scoring) below

## What Makes an Effective Rule

### 1. **Specificity with Context**
✅ Good: "Hunter Munitions synergizes with high critical chance weapons (>25%) to create frequent slash procs that bypass armor"

❌ Too vague: "Hunter Munitions is good"

❌ Too narrow: "Use Hunter Munitions on the Kuva Kohm with exactly 3 forma"

### 2. **Actionable Guidance**
Rules should tell the user what to do or what to prioritize, not just state facts.

✅ Good: "Multishot mods (Split Chamber, Barrel Diffusion, Hell's Chamber) are universally valuable - they effectively multiply damage output"

❌ Just facts: "Split Chamber adds multishot"

### 3. **Include Reasoning**
Explain WHY something matters, not just WHAT to do.

✅ Good: "Viral damage (Toxin + Cold) reduces enemy health by up to 325% when fully stacked - excellent for all factions"

❌ Missing context: "Use Viral damage"

### 4. **Thresholds and Numbers**
Include specific values that help with decision-making.

✅ Good: "Critical mods (Point Strike, Vital Sense) are valuable on weapons with 20%+ base crit chance - creates multiplicative scaling"

❌ Vague: "Critical mods work well on high crit weapons"

## Categories

Categories should group related rules logically. The system prompt displays rules grouped by category.

### Recommended Category Types

1. **StatusMechanics** - Rules about status effects and proc mechanics
2. **DamageTypes** - Elemental combinations, damage effectiveness
3. **Synergies** - Mod combinations and weapon/frame interactions
4. **ModPriority** - What mods to prioritize and why
5. **EnemyWeakness** - Faction-specific vulnerabilities
6. **BuildStrategy** - Overall build philosophy and approaches
7. **WeaponSpecific** - Rules for specific weapons or weapon archetypes
8. **FrameSpecific** - Rules for specific warframes or abilities
9. **GameMechanics** - Core game systems and mechanics
10. **ContentSpecific** - Rules for specific mission types or game modes

### Category Naming Conventions
- Use PascalCase (no spaces)
- Be descriptive but concise
- Remain consistent within a game's ruleset

## Confidence Scoring

Confidence values (0.0 to 1.0) indicate how reliable and universally applicable a rule is.

### Confidence Guidelines

**0.95 - 1.0**: Universal truth, unlikely to change
- Example: "Multishot effectively multiplies damage output"
- Game mechanics that are core and stable

**0.85 - 0.94**: Strong evidence, widely accepted by community
- Example: "Hunter Munitions synergizes with high crit weapons"
- Meta strategies with mathematical backing

**0.75 - 0.84**: Generally effective, some exceptions
- Example: "Hybrid builds work well on 20%+ crit and status weapons"
- Context-dependent strategies

**0.65 - 0.74**: Situational or opinion-based
- Example: "Ember is strong for clearing low-level content"
- Player preference or situational effectiveness

**Below 0.65**: Experimental or niche
- Avoid including rules with confidence this low unless testing

### Factors That Lower Confidence
- Depends on player skill level
- Meta shifts frequently
- Community has divided opinions
- Only works in specific scenarios
- Based on incomplete information

## Effective Rule Mix

A well-balanced ruleset should contain:

### Distribution by Category (Example for 50 rules)

```
30-40%  Core Mechanics (Damage, Status, Mods)
20-30%  Synergies and Combos  
15-25%  Build Strategy
10-15%  Enemy/Faction Knowledge
5-10%   Weapon/Frame Specific
5-10%   Content Specific
```

### Distribution by Confidence

```
40-50%  High confidence (0.85+)
30-40%  Medium-high confidence (0.75-0.84)
10-20%  Medium confidence (0.65-0.74)
0-10%   Lower confidence (experimental)
```

### Quality Over Quantity

**Target Size**: 50-150 rules per game
- Too few (<30): Insufficient context for AI
- Too many (>200): Clutters system prompt, increases latency
- Sweet spot: 75-125 rules

## Writing Best Practices

### DO ✅
- Focus on rules that affect decision-making
- Include mathematical relationships when relevant
- Mention specific mod/weapon/ability names
- Explain trade-offs and edge cases
- Update rules when game patches change mechanics
- Remove outdated rules promptly

### DON'T ❌
- Include basic tutorial information
- Write rules that are too obvious ("Pressing fire shoots weapon")
- Create rules that duplicate others
- Use jargon without explanation
- Include rules that are patch-specific without noting it
- Add rules based on single anecdotal experiences

## Maintenance

### Regular Review Cycle
1. **Monthly**: Check for game patches that invalidate rules
2. **Quarterly**: Evaluate rule effectiveness based on user feedback
3. **Bi-annually**: Rebalance category distribution

### Adding New Rules
1. Verify information accuracy against game wikis/patch notes
2. Check for duplicates or similar existing rules
3. Assign appropriate confidence based on evidence
4. Use consistent formatting and style
5. Test with sample prompts to ensure rule is used effectively

### Removing Rules
Remove rules when:
- Game mechanics have changed
- Rule is consistently ignored by AI
- Rule provides no unique value
- Information is outdated or incorrect

## Usage in Code

Rules are loaded by `GameRuleRepository` and injected into the system prompt when:
1. `UseGameRules` configuration is enabled
2. `GameName` is specified in the `AnalysisRequest`
3. Rules exist for the specified game

The formatted output groups rules by category and sorts by confidence within each category:

```
=== GAME KNOWLEDGE RULES ===
These rules provide specific guidance for Warframe. Apply them when relevant to the user's query.

## StatusMechanics
- Cedo Prime has 100% base status chance - prioritize status mods...
- Phantasma benefits greatly from status mods due to beam-based...

## DamageTypes
- Viral damage (Toxin + Cold) reduces enemy health by up to 325%...
- Corrosive damage (Toxin + Electric) is highly effective...
```

## Future Enhancements

Potential improvements to the rule system:
1. **Version tagging**: Track which game version a rule applies to
2. **Usage tracking**: Monitor which rules influence AI recommendations
3. **User feedback**: Allow users to rate rule effectiveness
4. **Dynamic loading**: Pull rules from external database/API
5. **Rule dependencies**: Link related rules for better context
6. **Multi-language support**: Provide rules in different languages

## Example: Complete Rule Entry

```json
{
  "RuleId": "wf-synergy-001",
  "GameName": "Warframe",
  "RuleText": "Hunter Munitions synergizes with high critical chance weapons (>25%) to create frequent slash procs that bypass armor",
  "Category": "Synergies",
  "Confidence": 0.9
}
```

**Why this rule is effective:**
- ✅ Specific threshold (>25% crit chance)
- ✅ Names the mod explicitly (Hunter Munitions)
- ✅ Explains the mechanism (slash procs bypass armor)
- ✅ Implies when to use it (against armored enemies)
- ✅ High confidence based on game mechanics
- ✅ Actionable for build optimization

---

## Quick Reference

**File Location**: `src/Mentor.Core/Data/WarframeRules.json`

**Code References**:
- Model: `src/Mentor.Core/Models/GameRule.cs`
- Repository: `src/Mentor.Core/Services/GameRuleRepository.cs`
- Usage: `src/Mentor.Core/Tools/AnalysisService.cs` (line 42-60)

**Testing**:
- Model tests: `tests/Mentor.Core.Tests/Models/GameRuleTests.cs`
- Repository tests: `tests/Mentor.Core.Tests/Services/GameRuleRepositoryTests.cs`

