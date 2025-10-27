using Mentor.Core.Interfaces;
using Mentor.Core.Services;
using Mentor.Core.Tests.Helpers;
using Mentor.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Services;

public static class TestServicesExtensions {
    public static IServiceCollection AddWebSearchTool(this IServiceCollection services, IWebSearchTool? tool = null)
    {
        if(tool != null)
        {
            services.AddSingleton<IWebSearchTool>(tool);
        }
        else
        {
            services.AddHttpClient();
            services.AddSingleton<IWebSearchTool, BraveWebSearch>();
        }
        return services;
    }
}

public static class TestHelpers
{
    public static IServiceCollection CreateTestServices(ITestOutputHelper _testOutputHelper)
    {
        var services = new ServiceCollection();

        // Register other necessary services and mocks here as needed for testing
        services.AddLogging(sp => sp.AddSerilog());

        // Set up logging bridge from Microsoft.Extensions.Logging to xUnit
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new XUnitLoggerProvider(_testOutputHelper, LogLevel.Debug));
        });
        services.AddSingleton(loggerFactory);

        return services;
    }
}