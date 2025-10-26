using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Mentor.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Basic argument parsing
        string? imagePath = null;
        string prompt = "What should I do next?";
        string provider = "perplexity";

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
            else if (args[i] == "--provider" && i + 1 < args.Length)
            {
                provider = args[i + 1];
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

        // Set up DI container with configuration
        var serviceProvider = ConfigureServices(provider);

        await AnalyzeScreenshotAsync(serviceProvider, imagePath, prompt, provider);
        return 0;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Mentor - Game Screenshot Analysis Tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  mentor --image <path> [--prompt <text>] [--provider <name>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --image <path>      Path to the screenshot image file (required)");
        Console.WriteLine("  --prompt <text>     Analysis prompt/question (default: 'What should I do next?')");
        Console.WriteLine("  --provider <name>   LLM provider to use (default: 'openai')");
        Console.WriteLine("  --help, -h          Show this help message");
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine("  API keys can be set in appsettings.Development.json or via environment variables:");
        Console.WriteLine("  LLM__Providers__perplexity__ApiKey=your-api-key");
        Console.WriteLine("  LLM__Providers__openai__ApiKey=your-api-key");
        Console.WriteLine("  LLM__Providers__local__ApiKey=your-api-key (not needed for local LLMs)");
    }

    private static ServiceProvider ConfigureServices(string provider)
    {
        // Determine base path for configuration files
        var currentDir = Directory.GetCurrentDirectory();
        var executableDir = AppContext.BaseDirectory;
        
        // Check if appsettings.json exists in current directory, otherwise fallback to executable directory
        var basePath = File.Exists(Path.Combine(currentDir, "appsettings.json"))
            ? currentDir
            : executableDir;

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();
        
        // Register configuration
        services.Configure<LLMConfiguration>(configuration.GetSection("LLM"));
        services.Configure<BraveSearchConfiguration>(configuration.GetSection("BraveSearch"));
        
        // Register core services
        services.AddHttpClient();
        services.AddLogging(sp =>
        {
            sp.AddSerilog();
        });
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
        services.AddTransient<IAnalysisService, AnalysisService>();
        services.AddTransient<IWebsearch, Websearch>();
        services.AddTransient<ILLMClient>(sp =>
        {
            var factory = sp.GetRequiredService<ILLMProviderFactory>();
            return factory.GetProvider(provider);
        });
        
        return services.BuildServiceProvider();
    }

    private static async Task AnalyzeScreenshotAsync(
        ServiceProvider serviceProvider, 
        string imagePath, 
        string prompt,
        string provider)
    {
        try
        {
            Console.WriteLine($"Analyzing screenshot: {imagePath}");
            Console.WriteLine($"Provider: {provider}");
            Console.WriteLine($"Prompt: {prompt}");
            Console.WriteLine();

            // Get the provider factory and create the chat client
            var analysisService = serviceProvider.GetRequiredService<IAnalysisService>();

            // Read the image file
            byte[] imageData;
            try
            {
                imageData = await File.ReadAllBytesAsync(imagePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading image file: {ex.Message}");
                return;
            }

            // Create analysis request
            var request = new AnalysisRequest
            {
                ImageData = imageData,
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
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Configuration Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
