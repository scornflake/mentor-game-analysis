using System.Text;
using Mentor.Core.Models;
using Mentor.Core.Tests.RuleEvaluation.Models;

namespace Mentor.Core.Tests.RuleEvaluation.Services;

/// <summary>
/// Generates markdown reports from comparison metrics
/// </summary>
public class ComparisonReportGenerator
{
    /// <summary>
    /// Generates a full markdown report from a comparison report
    /// </summary>
    public string GenerateReport(ComparisonReport report)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Rule Augmentation Impact Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Provider:** {report.ProviderName}");
        sb.AppendLine($"**Total Comparisons:** {report.TotalComparisons}");
        sb.AppendLine();

        // Executive Summary
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Comparisons with Improvement:** {report.ComparisonsWithImprovement} ({PercentageOf(report.ComparisonsWithImprovement, report.TotalComparisons)})");
        sb.AppendLine($"- **Comparisons with Degradation:** {report.ComparisonsWithDegradation} ({PercentageOf(report.ComparisonsWithDegradation, report.TotalComparisons)})");
        sb.AppendLine($"- **Neutral Comparisons:** {report.ComparisonsNeutral} ({PercentageOf(report.ComparisonsNeutral, report.TotalComparisons)})");
        sb.AppendLine();
        sb.AppendLine($"**Overall Improvement Score:** {FormatScore(report.AverageOverallImprovement)} {ImprovementEmoji(report.AverageOverallImprovement)}");
        sb.AppendLine();

        // Aggregate Metrics
        sb.AppendLine("## Aggregate Metrics");
        sb.AppendLine();
        
        if (report.ComparisonsWithSubjectiveEvaluation > 0)
        {
            sb.AppendLine($"*Subjective evaluations performed on {report.ComparisonsWithSubjectiveEvaluation} of {report.TotalComparisons} comparisons*");
            sb.AppendLine();
        }
        
        sb.AppendLine("| Metric | Mean Delta | Median Delta | Interpretation |");
        sb.AppendLine("|--------|------------|--------------|----------------|");
        sb.AppendLine($"| Specificity | {FormatScore(report.AverageSpecificityDelta)} | {FormatScore(report.MedianSpecificityDelta)} | {InterpretDelta(report.AverageSpecificityDelta)} |");
        sb.AppendLine($"| Terminology | {FormatScore(report.AverageTerminologyDelta)} | {FormatScore(report.MedianTerminologyDelta)} | {InterpretDelta(report.AverageTerminologyDelta)} |");
        sb.AppendLine($"| Actionability | {FormatScore(report.AverageActionabilityDelta)} | {FormatScore(report.MedianActionabilityDelta)} | {InterpretDelta(report.AverageActionabilityDelta)} |");
        
        // Add subjective evaluation row if available
        if (report.AverageSubjectiveDelta.HasValue && report.MedianSubjectiveDelta.HasValue)
        {
            sb.AppendLine($"| **Subjective (0-10)** | **{report.AverageSubjectiveDelta.Value:+0.00;-0.00;0.00}** | **{report.MedianSubjectiveDelta.Value:+0.00;-0.00;0.00}** | **{InterpretSubjectiveDelta(report.AverageSubjectiveDelta.Value)}** |");
        }
        
        sb.AppendLine();

        // Per-Screenshot Breakdown
        sb.AppendLine("## Per-Screenshot Results");
        sb.AppendLine();

        foreach (var comparison in report.IndividualComparisons.OrderByDescending(c => c.OverallImprovementScore))
        {
            sb.AppendLine($"### {Path.GetFileName(comparison.ScreenshotPath)}");
            sb.AppendLine();
            sb.AppendLine($"**Overall Improvement:** {FormatScore(comparison.OverallImprovementScore)} {ImprovementEmoji(comparison.OverallImprovementScore)}");
            sb.AppendLine();
            sb.AppendLine("| Metric | Baseline | Rule-Augmented | Delta |");
            sb.AppendLine("|--------|----------|----------------|-------|");
            sb.AppendLine($"| Specificity | {FormatScore(comparison.BaselineSpecificityScore)} | {FormatScore(comparison.RuleAugmentedSpecificityScore)} | {FormatDelta(comparison.SpecificityDelta)} |");
            sb.AppendLine($"| Terminology | {FormatScore(comparison.BaselineTerminologyScore)} | {FormatScore(comparison.RuleAugmentedTerminologyScore)} | {FormatDelta(comparison.TerminologyDelta)} |");
            sb.AppendLine($"| Actionability | {FormatScore(comparison.BaselineActionabilityScore)} | {FormatScore(comparison.RuleAugmentedActionabilityScore)} | {FormatDelta(comparison.ActionabilityDelta)} |");
            sb.AppendLine($"| Confidence | {FormatScore(comparison.BaselineConfidence)} | {FormatScore(comparison.RuleAugmentedConfidence)} | {FormatDelta(comparison.ConfidenceDelta)} |");
            sb.AppendLine($"| Rec. Count | {comparison.BaselineRecommendationCount} | {comparison.RuleAugmentedRecommendationCount} | {comparison.RecommendationCountDelta:+0;-0} |");
            sb.AppendLine($"| Duration | {FormatDuration(comparison.BaselineDuration)} | {FormatDuration(comparison.RuleAugmentedDuration)} | {FormatDuration(comparison.DurationDelta)} |");
            sb.AppendLine();
            
            // Add subjective evaluation if present
            if (comparison.BaselineSubjectiveEvaluation != null && comparison.RuleAugmentedSubjectiveEvaluation != null)
            {
                AppendSubjectiveEvaluation(sb, comparison);
            }
            
            // Add detailed recommendations
            AppendDetailedRecommendations(sb, comparison);
        }

        // Methodology
        sb.AppendLine("## Methodology");
        sb.AppendLine();
        sb.AppendLine("### Objective Metrics");
        sb.AppendLine();
        sb.AppendLine("**Specificity Score:** Unique game entities mentioned / recommendation count");
        sb.AppendLine("- Measures how concrete and specific recommendations are");
        sb.AppendLine("- Higher = mentions specific weapons, mods, frames by name");
        sb.AppendLine("- Range: 0-5+ (unbounded)");
        sb.AppendLine();
        sb.AppendLine("**Terminology Score:** Game-specific mechanic terms used (normalized to 0-1)");
        sb.AppendLine("- Measures technical depth and game knowledge");
        sb.AppendLine("- Higher = uses terms like 'status chance', 'corrosive', 'slash proc'");
        sb.AppendLine("- Range: 0-1");
        sb.AppendLine();
        sb.AppendLine("**Actionability Score:** Recommendations with clear actions (0-1)");
        sb.AppendLine("- Measures how actionable and concrete recommendations are");
        sb.AppendLine("- Higher = specific verbs (equip, replace) + reasoning");
        sb.AppendLine("- Range: 0-1");
        sb.AppendLine();
        sb.AppendLine("**Overall Improvement:** Average of the three objective metric deltas");
        sb.AppendLine("- Positive = rules improved recommendations");
        sb.AppendLine("- Negative = rules degraded recommendations");
        sb.AppendLine();
        
        if (report.ComparisonsWithSubjectiveEvaluation > 0)
        {
            sb.AppendLine("### Subjective Evaluation");
            sb.AppendLine();
            sb.AppendLine("**Subjective Score:** LLM-based evaluation against custom criteria (0-10 scale)");
            sb.AppendLine("- Uses a separate evaluator LLM to score recommendations against predefined expectations");
            sb.AppendLine("- Each criterion can specify positive expectations (should include X) or negative expectations (should avoid Y)");
            sb.AppendLine("- Criteria are weighted and averaged to produce an overall subjective score");
            sb.AppendLine("- Score of 10 = perfectly meets criterion, 0 = completely fails criterion");
            sb.AppendLine("- More sensitive than objective metrics to domain-specific quality nuances");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Saves the report to a file
    /// </summary>
    public async Task SaveReportAsync(ComparisonReport report, string outputPath)
    {
        var reportText = GenerateReport(report);
        
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, reportText);
    }

    private static string FormatScore(double value)
    {
        return $"{value:F3}";
    }

    private static string FormatDelta(double delta)
    {
        var formatted = $"{delta:+0.000;-0.000;0.000}";
        if (delta > 0.05)
            return $"**{formatted}** ↑";
        else if (delta < -0.05)
            return $"**{formatted}** ↓";
        else
            return formatted;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        else
            return $"{duration.TotalSeconds:F1}s";
    }

    private static string ImprovementEmoji(double score)
    {
        if (score > 0.1) return "✅ Significant Improvement";
        if (score > 0.05) return "📈 Moderate Improvement";
        if (score > 0.01) return "➡️ Slight Improvement";
        if (score > -0.01) return "⚪ Neutral";
        if (score > -0.05) return "➡️ Slight Degradation";
        if (score > -0.1) return "📉 Moderate Degradation";
        return "❌ Significant Degradation";
    }

    private static string InterpretDelta(double delta)
    {
        if (delta > 0.1) return "Strong positive impact";
        if (delta > 0.05) return "Moderate positive impact";
        if (delta > 0.01) return "Slight positive impact";
        if (delta > -0.01) return "Negligible impact";
        if (delta > -0.05) return "Slight negative impact";
        if (delta > -0.1) return "Moderate negative impact";
        return "Strong negative impact";
    }

    private static string InterpretSubjectiveDelta(double delta)
    {
        // Subjective scores are on 0-10 scale, so thresholds are larger
        if (delta > 2.0) return "Very strong improvement";
        if (delta > 1.0) return "Strong improvement";
        if (delta > 0.5) return "Moderate improvement";
        if (delta > 0.1) return "Slight improvement";
        if (delta > -0.1) return "Negligible change";
        if (delta > -0.5) return "Slight degradation";
        if (delta > -1.0) return "Moderate degradation";
        if (delta > -2.0) return "Strong degradation";
        return "Very strong degradation";
    }

    private static string PercentageOf(int numerator, int denominator)
    {
        if (denominator == 0) return "N/A";
        return $"{(double)numerator / denominator:P0}";
    }

    private static void AppendDetailedRecommendations(StringBuilder sb, ComparisonMetrics comparison)
    {
        sb.AppendLine("#### Baseline Analysis (No Rules)");
        sb.AppendLine();
        
        if (comparison.BaselineRecommendation != null)
        {
            AppendRecommendationDetails(sb, comparison.BaselineRecommendation);
        }
        else
        {
            sb.AppendLine("*No baseline recommendation available*");
        }
        
        sb.AppendLine();
        sb.AppendLine("#### Rule-Augmented Analysis");
        sb.AppendLine();
        
        if (comparison.RuleAugmentedRecommendation != null)
        {
            AppendRecommendationDetails(sb, comparison.RuleAugmentedRecommendation);
        }
        else
        {
            sb.AppendLine("*No rule-augmented recommendation available*");
        }
        
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void AppendRecommendationDetails(StringBuilder sb, Recommendation recommendation)
    {
        // Summary
        sb.AppendLine("**Summary:**");
        sb.AppendLine();
        sb.AppendLine(TruncateText(recommendation.Summary, 500));
        sb.AppendLine();
        
        // Analysis
        sb.AppendLine("**Analysis:**");
        sb.AppendLine();
        sb.AppendLine(TruncateText(recommendation.Analysis, 1000));
        sb.AppendLine();
        
        // Recommendations
        sb.AppendLine("**Recommendations:**");
        sb.AppendLine();
        
        if (recommendation.Recommendations.Count == 0)
        {
            sb.AppendLine("*No specific recommendations provided*");
        }
        else
        {
            for (int i = 0; i < recommendation.Recommendations.Count; i++)
            {
                var rec = recommendation.Recommendations[i];
                sb.AppendLine($"{i + 1}. **[{rec.Priority.ToString().ToUpper()}]** {TruncateText(rec.Action, 200)}");
                
                if (!string.IsNullOrWhiteSpace(rec.Reasoning))
                {
                    sb.AppendLine($"   - **Reasoning:** {TruncateText(rec.Reasoning, 300)}");
                }
                
                if (!string.IsNullOrWhiteSpace(rec.Context))
                {
                    sb.AppendLine($"   - **Context:** {TruncateText(rec.Context, 300)}");
                }
                
                if (!string.IsNullOrWhiteSpace(rec.ReferenceLink))
                {
                    sb.AppendLine($"   - **Reference:** {rec.ReferenceLink}");
                }
                
                sb.AppendLine();
            }
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "*No text provided*";
        
        text = text.Trim();
        
        if (text.Length <= maxLength)
            return text;
        
        // Find a good break point (space, period, etc.)
        var truncated = text.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOfAny(new[] { ' ', '.', ',', ';' });
        
        if (lastSpace > maxLength - 50) // Only use the break if it's reasonably close to the end
        {
            truncated = truncated.Substring(0, lastSpace);
        }
        
        return truncated + "...";
    }

    private static void AppendSubjectiveEvaluation(StringBuilder sb, ComparisonMetrics comparison)
    {
        sb.AppendLine("#### Subjective Evaluation");
        sb.AppendLine();
        
        var baselineScore = comparison.BaselineSubjectiveScore ?? 0;
        var ruleScore = comparison.RuleAugmentedSubjectiveScore ?? 0;
        var delta = comparison.SubjectiveScoreDelta ?? 0;
        
        sb.AppendLine($"**Baseline Score:** {baselineScore:F1}/10");
        sb.AppendLine($"**Rule-Augmented Score:** {ruleScore:F1}/10");
        sb.AppendLine($"**Delta:** {FormatSubjectiveDelta(delta)}");
        sb.AppendLine();
        
        if (comparison.BaselineSubjectiveEvaluation != null && comparison.RuleAugmentedSubjectiveEvaluation != null)
        {
            sb.AppendLine("**Evaluation Details:**");
            sb.AppendLine();
            
            for (int i = 0; i < comparison.BaselineSubjectiveEvaluation.Evaluations.Count; i++)
            {
                var baselineEval = comparison.BaselineSubjectiveEvaluation.Evaluations[i];
                var ruleEval = comparison.RuleAugmentedSubjectiveEvaluation.Evaluations.FirstOrDefault(e => e.CriterionIndex == i);
                
                if (ruleEval != null)
                {
                    sb.AppendLine($"{i + 1}. **{baselineEval.Criterion}** (weight: {baselineEval.Weight})");
                    sb.AppendLine($"   - **Baseline:** {baselineEval.Score:F1}/10 - {baselineEval.Reasoning}");
                    sb.AppendLine($"   - **Rule-Aug:** {ruleEval.Score:F1}/10 - {ruleEval.Reasoning}");
                    var criterionDelta = ruleEval.Score - baselineEval.Score;
                    if (Math.Abs(criterionDelta) > 0.5)
                    {
                        sb.AppendLine($"   - **Delta:** {criterionDelta:+0.0;-0.0} {(criterionDelta > 0 ? "✅" : "⬇️")}");
                    }
                    sb.AppendLine();
                }
            }
        }
        
        sb.AppendLine();
    }

    private static string FormatSubjectiveDelta(double delta)
    {
        var formatted = $"{delta:+0.0;-0.0;0.0}";
        if (delta > 1.5)
            return $"**{formatted}** ✅ Significant Improvement";
        else if (delta > 0.5)
            return $"**{formatted}** 📈 Moderate Improvement";
        else if (delta > 0)
            return $"{formatted} ➡️ Slight Improvement";
        else if (delta > -0.5)
            return $"{formatted} ⚪ Neutral";
        else if (delta > -1.5)
            return $"**{formatted}** ⬇️ Moderate Degradation";
        else
            return $"**{formatted}** ❌ Significant Degradation";
    }
}

