using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Services;
using MentorUI.ViewModels;
using MentorUI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MentorUI;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure services
        ServiceProvider = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
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
            return factory.GetProvider("perplexity");
        });

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}

