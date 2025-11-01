# Rule Evaluation Project

This is a research/evaluation project for objectively measuring the impact of game-specific rule augmentation on LLM analysis quality.

## Purpose

Compare analysis quality with and without manually-authored game rules to determine if rule injection improves recommendations.

## Quick Start

### 1. Create an Evaluation Config

Create a JSON file describing your evaluation (see `evaluation-example.json`):

```json
{
  "provider": "perplexity",
  "evaluator": "openai-local",
  "screenshots": [
    {
      "path": "../media/acceltra prime rad build.png",
      "prompt": "Analyze this build and suggest improvements for Steel Path",
      "evaluationCriteria": [
        {
          "criterion": "Should recommend corrosive or viral damage for Grineer armor",
          "expectation": "positive",
          "weight": 1.0
        },
        {
          "criterion": "Should NOT recommend fire or blast as primary damage",
          "expectation": "negative",
          "weight": 0.8
        }
      ]
    }
  ]
}
```

**Configuration Fields:**
- `provider`: LLM provider for analysis (required)
- `evaluator`: LLM provider for subjective evaluation (optional, defaults to same as `provider`)
- `screenshots`: Array of screenshots with prompts
  - `evaluationCriteria`: Optional subjective evaluation criteria
    - `criterion`: What to evaluate (e.g., "Should recommend X")
    - `expectation`: "positive" (should do) or "negative" (should not do)
    - `weight`: Importance multiplier (0-1)

### 2. Run the Evaluation

From this directory:

```bash
dotnet run -- evaluation-example.json
```

Or from the repo root:

```bash
dotnet run --project tests/Mentor.Core.Tests.RuleEvaluation -- tests/Mentor.Core.Tests.RuleEvaluation/evaluation-example.json
```

### 3. Review the Report

The tool generates a markdown report in `./reports/` with:
- Per-screenshot metrics (specificity, terminology, actionability)
- Aggregate statistics (mean/median across all screenshots)
- Deltas showing improvement or degradation with rules enabled
- **Full recommendation text from both analyses** for side-by-side comparison

## Metrics Explained

### Objective Metrics (Automatic)

- **Specificity Score**: Unique game entities mentioned / recommendation count
  - Higher = more concrete recommendations (e.g., "equip Hunter Munitions" vs "use crit mods")

- **Terminology Score**: Game-specific mechanic terms used
  - Higher = more technical depth (e.g., mentions "status chance", "corrosive", "slash proc")

- **Actionability Score**: Recommendations with clear actions
  - Higher = more actionable (e.g., has action verbs, specific items, reasoning)

- **Overall Improvement**: Average of the three metric deltas
  - Positive = rules helped, negative = rules hurt, ~0 = neutral

### Subjective Metrics (Optional, LLM-Based)

When `evaluationCriteria` are provided:

- **Criterion Scores** (0-10 per criterion): LLM judges how well recommendations meet each criterion
  - Positive expectations: score 10 = perfectly fulfills the criterion
  - Negative expectations: score 10 = correctly avoids the thing
  - Each score includes LLM reasoning

- **Overall Subjective Score** (0-10): Weighted average of all criterion scores
  - Complements objective metrics with domain-specific quality assessment
  - Detects semantic improvements that objective metrics might miss

## Provider Configuration

The tool loads provider configurations from the shared Mentor configuration database (`%LOCALAPPDATA%/Mentor/mentor.db`).

You can configure providers through:
- Mentor.CLI: `dotnet run --project src/Mentor.CLI`
- Mentor.Uno: The desktop application settings page

## Example Output

```
[1/2] Processing: acceltra prime rad build.png
    Prompt: Analyze this Acceltra Prime radiation build...
    Subjective evaluation: 4 criteria
    ✓ Complete - Overall improvement: 0.127
    ✓ Subjective delta: +1.8

✓ Report saved to: ./reports/evaluation_report_2024-11-01_153045.md

Summary:
  Total comparisons: 2
  With improvement: 2
  With degradation: 0
  Neutral: 0
  Average improvement: 0.112
```

## Report Contents

Each report includes:

1. **Executive Summary** - High-level statistics and overall improvement score
2. **Aggregate Metrics** - Mean and median deltas across all screenshots
3. **Per-Screenshot Results** - For each screenshot:
   - Metrics comparison table
   - **Subjective Evaluation** (if criteria provided) - Per-criterion scores and reasoning
   - **Baseline Analysis (No Rules)** - Full summary, analysis, and recommendations
   - **Rule-Augmented Analysis** - Full summary, analysis, and recommendations
   - This allows you to directly compare the quality and specificity of recommendations
4. **Methodology** - Explanation of how metrics are calculated

## Tips

- Use specific prompts per screenshot for best results
- Test with 3-5 screenshots for meaningful aggregate stats
- Provider must support vision/image analysis (GPT-4 Vision, Claude, etc.)
- Each evaluation runs TWICE per screenshot (baseline + rule-augmented), so it takes time

## Architecture

- `Program.cs`: CLI entry point
- `Models/EvaluationConfig.cs`: JSON config model
- `Models/EvaluationCriterion.cs`: Subjective evaluation criterion
- `Services/RuleComparisonHarness.cs`: Runs dual analysis
- `Services/MetricsCalculator.cs`: Calculates objective quality metrics
- `Services/SubjectiveEvaluationService.cs`: LLM-based subjective evaluation
- `Services/ComparisonReportGenerator.cs`: Generates markdown reports

