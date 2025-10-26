using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mentor.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Set up DI container
        var serviceProvider = ConfigureServices();

        // Basic argument parsing for validation
        string? imagePath = null;
        string prompt = "What should I do next?";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--image" && i + 1 < args.Length)
            {
                imagePath = args[i + 1];
                i++;
            }
            else if (args[i] == "--prompt" && i + 1 < args.Length)
            {
                prompt = args[i + 1];
                i++;
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                ShowHelp();
                return 0;
            }
        }

        if (string.IsNullOrEmpty(imagePath))
        {
            Console.WriteLine("Error: --image parameter is required");
            Console.WriteLine();
            ShowHelp();
            return 1;
        }

        await AnalyzeScreenshotAsync(serviceProvider, imagePath, prompt);
        return 0;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Mentor - Game Screenshot Analysis Tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  mentor --image <path> [--prompt <text>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --image <path>   Path to the screenshot image file (required)");
        Console.WriteLine("  --prompt <text>  Analysis prompt/question (default: 'What should I do next?')");
        Console.WriteLine("  --help, -h       Show this help message");
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Register core services
        services.AddSingleton<IAnalysisService, AnalysisService>();
        
        return services.BuildServiceProvider();
    }

    private static async Task AnalyzeScreenshotAsync(
        ServiceProvider serviceProvider, 
        string imagePath, 
        string prompt)
    {
        try
        {
            Console.WriteLine($"Analyzing screenshot: {imagePath}");
            Console.WriteLine($"Prompt: {prompt}");
            Console.WriteLine();

            // Get the analysis service
            var analysisService = serviceProvider.GetRequiredService<IAnalysisService>();

            // For now, just create a stub request (not actually reading the image file)
            var request = new AnalysisRequest
            {
                ImageData = Array.Empty<byte>(), // Stub: not actually reading file yet
                Prompt = prompt
            };

            // Perform analysis
            var result = await analysisService.AnalyzeAsync(request);

            // Display results
            Console.WriteLine("=== Analysis Complete ===");
            Console.WriteLine();
            Console.WriteLine($"Provider: {result.ProviderUsed}");
            Console.WriteLine($"Confidence: {result.Confidence:P0}");
            Console.WriteLine($"Generated: {result.GeneratedAt:u}");
            Console.WriteLine();
            Console.WriteLine($"Summary: {result.Summary}");
            Console.WriteLine();
            Console.WriteLine($"Analysis: {result.Analysis}");
            Console.WriteLine();
            Console.WriteLine("Recommendations:");
            
            foreach (var rec in result.Recommendations)
            {
                Console.WriteLine($"  [{rec.Priority}] {rec.Action}");
                Console.WriteLine($"      Reasoning: {rec.Reasoning}");
                Console.WriteLine($"      Context: {rec.Context}");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
