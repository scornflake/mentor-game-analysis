using Mentor.Core.Configuration;
using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Services;
using Mentor.Core.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Mentor.CLI;

public class Program
{
    private static ILogger<Program> _logger;
    private static ServiceProvider _serviceProvider;

    public static int Main(string[] args)
    {
        return AsyncContext.Run(async () =>
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                ShowHelp();
                return 0;
            }

            var command = args[0].ToLowerInvariant();
            _serviceProvider = ConfigureServices();
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                switch (command)
                {
                    case "provider":
                        return await HandleProviderCommand(args.Skip(1).ToArray());
                    case "tool":
                        return await HandleToolCommand(args.Skip(1).ToArray());
                    case "analyze":
                        return await HandleAnalyzeCommand(args.Skip(1).ToArray());
                    default:
                        // For backward compatibility, treat as analyze if --image is present
                        if (args.Any(a => a == "--image"))
                        {
                            return await HandleAnalyzeCommand(args);
                        }
                        Console.WriteLine($"Unknown command: {command}");
                        Console.WriteLine();
                        ShowHelp();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Mentor - Game Screenshot Analysis Tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  mentor <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  provider list                          List all LLM providers");
        Console.WriteLine("  provider create <name>                 Create a new provider");
        Console.WriteLine("  provider update <name>                 Update a provider");
        Console.WriteLine("  provider delete <name>                 Delete a provider");
        Console.WriteLine("  provider activate <name>               Set a provider as active");
        Console.WriteLine();
        Console.WriteLine("  tool list                              List all search tools");
        Console.WriteLine("  tool create <name>                     Create a new tool");
        Console.WriteLine("  tool update <name>                     Update a tool");
        Console.WriteLine("  tool delete <name>                     Delete a tool");
        Console.WriteLine();
        Console.WriteLine("  analyze --image <path>                 Analyze a screenshot");
        Console.WriteLine();
        Console.WriteLine("Provider Options:");
        Console.WriteLine("  --provider-type <type>    Provider type (openai, perplexity, etc.)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  mentor provider list");
        Console.WriteLine("  mentor provider create OpenAI --provider-type openai --apikey sk-xxx --model gpt-4o");
        Console.WriteLine("  mentor provider update OpenAI --apikey sk-new-key");
        Console.WriteLine("  mentor provider activate OpenAI");
        Console.WriteLine("  mentor tool list");
        Console.WriteLine("  mentor tool create Brave --apikey brave-xxx --url https://api.search.brave.com");
        Console.WriteLine("  mentor tool update Brave --apikey brave-new-key");
    }

    private static async Task<int> HandleProviderCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Provider command requires an action");
            return 1;
        }

        var action = args[0].ToLowerInvariant();
        var repository = _serviceProvider.GetRequiredService<IConfigurationRepository>();

        switch (action)
        {
            case "list":
                return await ListProviders(repository);
            case "create":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Provider create requires a name");
                    return 1;
                }
                return await CreateProvider(repository, args[1], args.Skip(2).ToArray());
            case "update":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Provider update requires a name");
                    return 1;
                }
                return await UpdateProvider(repository, args[1], args.Skip(2).ToArray());
            case "delete":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Provider delete requires a name");
                    return 1;
                }
                return await DeleteProvider(repository, args[1]);
            case "activate":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Provider activate requires a name");
                    return 1;
                }
                return await ActivateProvider(repository, args[1]);
            default:
                Console.WriteLine($"Unknown provider action: {action}");
                return 1;
        }
    }

    private static async Task<int> HandleToolCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Tool command requires an action");
            return 1;
        }

        var repository = _serviceProvider.GetRequiredService<IConfigurationRepository>();
        var action = args[0].ToLowerInvariant();

        switch (action)
        {
            case "list":
                return await ListTools(repository);
            case "create":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Tool create requires a name");
                    return 1;
                }
                return await CreateTool(repository, args[1], args.Skip(2).ToArray());
            case "update":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Tool update requires a name");
                    return 1;
                }
                return await UpdateTool(repository, args[1], args.Skip(2).ToArray());
            case "delete":
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: Tool delete requires a name");
                    return 1;
                }
                return await DeleteTool(repository, args[1]);
            default:
                Console.WriteLine($"Unknown tool action: {action}");
                return 1;
        }
    }

    private static async Task<int> ListProviders(IConfigurationRepository repository)
    {
        await repository.SeedDefaultsAsync();
        var providers = await repository.GetAllProvidersAsync();
        var activeProvider = await repository.GetActiveProviderAsync();

        if (!providers.Any())
        {
            Console.WriteLine("No providers configured.");
            return 0;
        }

        Console.WriteLine("LLM Providers:");
        Console.WriteLine();

        foreach (var provider in providers)
        {
            var isActive = activeProvider != null && 
                          provider.ProviderType == activeProvider.ProviderType &&
                          provider.BaseUrl == activeProvider.BaseUrl;
            
            var activeMarker = isActive ? " [ACTIVE]" : "";
            Console.WriteLine($"Provider: {provider.Name}{activeMarker}");
            Console.WriteLine($"  Type: {provider.ProviderType}");
            Console.WriteLine($"  Model: {provider.Model}");
            Console.WriteLine($"  Base URL: {provider.BaseUrl}");
            Console.WriteLine($"  API Key: {MaskApiKey(provider.ApiKey)}");
            Console.WriteLine($"  Timeout: {provider.Timeout}s");
            Console.WriteLine();
        }

        return 0;
    }

    private static async Task<int> CreateProvider(IConfigurationRepository repository, string name, string[] args)
    {
        var options = ParseOptions(args);

        if (!options.ContainsKey("--provider-type"))
        {
            Console.WriteLine("Error: --provider-type is required");
            return 1;
        }

        if (!options.TryGetValue("--apikey", out var option))
        {
            Console.WriteLine("Error: --apikey is required");
            return 1;
        }

        var config = new ProviderConfiguration
        {
            ProviderType = options["--provider-type"],
            ApiKey = option,
            Model = options.GetValueOrDefault("--model", string.Empty),
            BaseUrl = options.GetValueOrDefault("--base-url", string.Empty),
            Timeout = int.TryParse(options.GetValueOrDefault("--timeout", "60"), out var timeout) ? timeout : 60
        };

        await repository.SaveProviderAsync(name, config);
        Console.WriteLine($"Provider '{name}' created successfully.");
        return 0;
    }

    private static async Task<int> UpdateProvider(IConfigurationRepository repository, string name, string[] args)
    {
        var existing = await repository.GetProviderByNameAsync(name);
        if (existing == null)
        {
            Console.WriteLine($"Error: Provider '{name}' not found");
            return 1;
        }

        var options = ParseOptions(args);

        // Partial update - only update provided fields
        if (options.TryGetValue("--provider-type", out var option))
        {
            existing.ProviderType = option;
        }
        if (options.TryGetValue("--apikey", out var option1))
        {
            existing.ApiKey = option1;
        }
        if (options.TryGetValue("--model", out var option2))
        {
            existing.Model = option2;
        }
        if (options.TryGetValue("--name", out var option3))
        {
            existing.Name = option3;
        }
        if (options.TryGetValue("--base-url", out var option4))
        {
            existing.BaseUrl = option4;
        }
        if (options.TryGetValue("--timeout", out var option5))
        {
            if (int.TryParse(option5, out var timeout))
            {
                existing.Timeout = timeout;
            }
        }

        await repository.SaveProviderAsync(name, existing);
        Console.WriteLine($"Provider '{name}' updated successfully.");
        return 0;
    }

    private static async Task<int> DeleteProvider(IConfigurationRepository repository, string name)
    {
        await repository.DeleteProviderAsync(name);
        Console.WriteLine($"Provider '{name}' deleted successfully.");
        return 0;
    }

    private static async Task<int> ActivateProvider(IConfigurationRepository repository, string name)
    {
        try
        {
            await repository.SetActiveProviderAsync(name);
            Console.WriteLine($"Provider '{name}' activated successfully.");
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ListTools(IConfigurationRepository repository)
    {
        await repository.SeedDefaultsAsync();
        var tools = await repository.GetAllToolsAsync();

        if (!tools.Any())
        {
            Console.WriteLine("No tools configured.");
            return 0;
        }

        Console.WriteLine("Search Tools:");
        Console.WriteLine();

        foreach (var tool in tools)
        {
            Console.WriteLine($"Tool: {tool.ToolName}");
            Console.WriteLine($"  URL: {tool.BaseUrl}");
            Console.WriteLine($"  API Key: {MaskApiKey(tool.ApiKey)}");
            Console.WriteLine($"  Timeout: {tool.Timeout}s");
            Console.WriteLine();
        }

        return 0;
    }

    private static async Task<int> CreateTool(IConfigurationRepository repository, string name, string[] args)
    {
        var options = ParseOptions(args);

        if (!options.TryGetValue("--apikey", out var option))
        {
            Console.WriteLine("Error: --apikey is required");
            return 1;
        }

        var config = new RealWebtoolToolConfiguration
        {
            ToolName = name,
            ApiKey = option,
            BaseUrl = options.GetValueOrDefault("--url", string.Empty),
            Timeout = int.TryParse(options.GetValueOrDefault("--timeout", "30"), out var timeout) ? timeout : 30
        };

        await repository.SaveToolAsync(name, config);
        Console.WriteLine($"Tool '{name}' created successfully.");
        return 0;
    }

    private static async Task<int> UpdateTool(IConfigurationRepository repository, string name, string[] args)
    {
        var existing = await repository.GetToolByNameAsync(name);
        if (existing == null)
        {
            Console.WriteLine($"Error: Tool '{name}' not found");
            return 1;
        }

        var options = ParseOptions(args);

        // Create a new configuration with updated values
        var updated = new RealWebtoolToolConfiguration
        {
            ToolName = existing.ToolName,
            ApiKey = options.TryGetValue("--apikey", out var option) ? option : existing.ApiKey,
            BaseUrl = options.TryGetValue("--url", out var option1) ? option1 : existing.BaseUrl,
            Timeout = options.ContainsKey("--timeout") && int.TryParse(options["--timeout"], out var timeout) 
                ? timeout 
                : existing.Timeout
        };

        await repository.SaveToolAsync(name, updated);
        Console.WriteLine($"Tool '{name}' updated successfully.");
        return 0;
    }

    private static async Task<int> DeleteTool(IConfigurationRepository repository, string name)
    {
        await repository.DeleteToolAsync(name);
        Console.WriteLine($"Tool '{name}' deleted successfully.");
        return 0;
    }

    private static async Task<int> HandleAnalyzeCommand(string[] args)
    {
        // Parse analyze-specific arguments
        string? imagePath = null;
        string prompt = "What should I do next?";
        string? provider = null;

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
        }

        if (string.IsNullOrEmpty(imagePath))
        {
            Console.WriteLine("Error: --image parameter is required");
            return 1;
        }
        
        // Image must exist on disk
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Error: Image file '{imagePath}' does not exist");
            return 1;
        }

        if (string.IsNullOrEmpty(provider))
        {
            Console.WriteLine("Error: --provider parameter is required");
            return 1;
        }

        // Load the provider configuration
        try
        {
            var repository = _serviceProvider.GetRequiredService<IConfigurationRepository>();
            var namedProvider = await repository.GetProviderByNameAsync(provider);
            if (namedProvider == null)
            {
                var msg = $"Specified provider '{provider}' not found in configuration.";
                Console.Write(msg);
                return 2;
            }

            await AnalyzeScreenshotAsync(_serviceProvider, imagePath, prompt, namedProvider);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            // _logger.LogError("Error during analysis: {Message}, {Trace}", ex.Message, ex.StackTrace);
            Console.WriteLine(ex.StackTrace);
            return 3;
        }
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") && i + 1 < args.Length)
            {
                options[args[i]] = args[i + 1];
                i++;
            }
        }

        return options;
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "(not set)";
        }

        if (apiKey.Length <= 8)
        {
            return "****";
        }

        return apiKey.Substring(0, 4) + "****" + apiKey.Substring(apiKey.Length - 4);
    }

    private static ServiceProvider ConfigureServices()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var executableDir = AppContext.BaseDirectory;
        
        var basePath = File.Exists(Path.Combine(currentDir, "appsettings.json"))
            ? currentDir
            : executableDir;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();
        
        services.AddRealmConfigurationRepository();
        
        services.AddHttpClient();
        services.AddLogging(sp =>
        {
            sp.AddSerilog();
        });
        services.AddSingleton<IToolFactory, ToolFactory>();
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
        services.AddKeyedTransient<IWebSearchTool, BraveWebSearch>(KnownSearchTools.Brave);
        
        return services.BuildServiceProvider();
    }

    private static async Task AnalyzeScreenshotAsync(
        ServiceProvider serviceProvider, 
        string imagePath, 
        string prompt,
        ProviderConfiguration provider)
    {
        try
        {
            Console.WriteLine($"Analyzing screenshot: {imagePath}");
            Console.WriteLine($"Provider: {provider}");
            Console.WriteLine($"Prompt: {prompt}");
            Console.WriteLine();

            // Get the provider factory and create the chat client
            var factory = serviceProvider.GetRequiredService<ILLMProviderFactory>();
            var llmClient = factory.GetProvider(provider);
            var analysisService = factory.GetAnalysisService(llmClient);

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
