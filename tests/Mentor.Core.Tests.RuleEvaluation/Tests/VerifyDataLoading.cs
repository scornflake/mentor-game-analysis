using Mentor.Core.Models;
using Mentor.Core.Tests.RuleEvaluation.Services;

namespace Mentor.Core.Tests.RuleEvaluation.Tests;

/// <summary>
/// Manual verification harness to check that data files are being loaded
/// Run this to verify MetricsCalculator loads terms from data/*.txt files
/// </summary>
public class VerifyDataLoading
{
    public static void Run()
    {
        Console.WriteLine("=== Verifying MetricsCalculator Data Loading ===\n");
        
        // Create a calculator instance - this will trigger the static constructor
        var calculator = new MetricsCalculator();
        
        Console.WriteLine("\nMetricsCalculator instantiated successfully.");
        Console.WriteLine("Check above for loading messages showing terms loaded from data/*.txt files.\n");
        
        // Create a dummy recommendation to test the calculator
        var testRec = new Recommendation
        {
            Analysis = "Testing with Acceltra Prime and Corrosive damage",
            Summary = "Use status chance mods",
            Recommendations = new List<RecommendationItem>
            {
                new RecommendationItem
                {
                    Action = "Equip Acceltra Prime",
                    Reasoning = "High status chance",
                    Context = "For Grineer enemies",
                    Priority = Priority.High
                }
            }
        };
        
        var metrics = calculator.CalculateAllMetrics(testRec);
        
        Console.WriteLine($"Test Metrics:");
        Console.WriteLine($"  Specificity: {metrics.specificity:F3}");
        Console.WriteLine($"  Terminology: {metrics.terminology:F3}");
        Console.WriteLine($"  Actionability: {metrics.actionability:F3}");
        
        Console.WriteLine("\n✓ Verification complete!");
    }
}

