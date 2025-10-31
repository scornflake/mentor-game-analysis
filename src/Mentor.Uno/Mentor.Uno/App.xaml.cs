using System;
using Mentor.Core.Configuration;
using Mentor.Core.Interfaces;
using Mentor.Core.Services;
using Mentor.Core.Tools;
using Mentor.Uno.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Uno.Resizetizer;
using Mentor.Uno.Platforms;
using Uno.Extensions.Navigation.Toolkit;
using Microsoft.UI.Xaml;

namespace Mentor.Uno;

public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

    }

    private IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Determine base path for configuration files
                var basePath = AppContext.BaseDirectory;

                config.SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register configuration repository
                services.AddConfigurationRepository();

                // Register core services
                services.AddHttpClient();
                services.AddSingleton<WindowStateHelper>();
                services.AddSingleton<IToolFactory, ToolFactory>();
                services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
                services.AddKeyedTransient<IWebSearchTool, BraveWebSearch>(KnownSearchTools.Brave);
                services.AddKeyedTransient<IArticleReader, ArticleReader>(KnownTools.ArticleReader);
                services.AddTransient<IImageAnalyzer, ImageAnalyzer>();

                // Register clipboard monitoring service
                services.AddSingleton<Mentor.Uno.Services.ClipboardMonitor>();

                // Register messaging
                services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);

                // Register ViewModels
                services.AddTransient<MainPageViewModel>();
                services.AddTransient<SettingsPageViewModel>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                
                // Determine log path in user's local app data
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Mentor",
                    "logs",
                    "mentor.log");
                
                // Ensure log directory exists
                var logDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                
                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(
                        logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                
                var logger = logConfig.CreateLogger();
                logging.AddSerilog(logger);
            })
            .Build();
    }

    public static T GetService<T>() where T : class
    {
        return ((App)Current)._host?.Services.GetRequiredService<T>()
            ?? throw new InvalidOperationException("Service not found");
    }

    public Window? MainWindow { get; private set; }

    protected IApplicationBuilder BuildLikeOther(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args);
        builder = builder.UseToolkitNavigation();
        //.Configure(host => {
        //    host.ConfigureAppConfiguration((context, config) =>
        //    {
        //        // Determine base path for configuration files
        //        var basePath = AppContext.BaseDirectory;

        //        config.SetBasePath(basePath)
        //            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //            .AddEnvironmentVariables();
        //    })
        //    .ConfigureServices((context, services) =>
        //     {
        //         // Register configuration repository
        //         services.AddConfigurationRepository();

        //         // Register core services
        //         services.AddHttpClient();
        //         services.AddSingleton<IToolFactory, ToolFactory>();
        //         services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
        //         services.AddKeyedTransient<IWebSearchTool, BraveWebSearch>(KnownSearchTools.Brave);

        //         // Register messaging
        //         services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);

        //         // Register ViewModels
        //         services.AddTransient<MainPageViewModel>();
        //         services.AddTransient<SettingsPageViewModel>();

        //         // Seed defaults on startup
        //         var serviceProvider = services.BuildServiceProvider();
        //         var repo = serviceProvider.GetRequiredService<IConfigurationRepository>();
        //         ////repo.SeedDefaultsAsync().Wait();
        //     })
        //    .ConfigureLogging((context, logging) =>
        //    {
        //        logging.ClearProviders();
        //        var logger = new LoggerConfiguration()
        //            .MinimumLevel.Debug()
        //            .WriteTo.Console()
        //            .CreateLogger();
        //        logging.AddSerilog(logger);
        //    });
        //});
        return builder;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Build the host with services
        _host = BuildHost();
        //var builder = BuildLikeOther(args);

        //MainWindow = builder.Window;
        //MainWindow.SetWindowIcon();

        MainWindow = new Window();
        //_host = await builder.NavigateAsync<MainPage>();

        var repo = _host.Services.GetRequiredService<IConfigurationRepository>();
        await repo.SeedDefaultsAsync();


#if DEBUG
        MainWindow.UseStudio();
#endif


        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        // Set the window icon for Windows
        MainWindow.SetWindowIcon();
        
        // Ensure the current window is active
        MainWindow.Activate();

        // Configure macOS menu bar after window activation (runtime check inside)
        MacOSExtensions.ConfigureMacOSMenu();

        // Restore window state and setup tracking
        var windowStateHelper = _host.Services.GetRequiredService<WindowStateHelper>();
        await windowStateHelper.RestoreWindowStateAsync(MainWindow, "MainWindow", repo, defaultWidth: 1280, defaultHeight: 900);
        windowStateHelper.SetupWindowStateTracking(MainWindow, "MainWindow", repo);
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
