using Mentor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Services;

public static class UserDataPathServiceExtensions
{
    public static IServiceCollection AddUserDataPathService(this IServiceCollection services)
    {
        services.AddSingleton<IUserDataPathService, UserDataPathService>();
        return services;
    }
}

public class UserDataPathService : IUserDataPathService
{
    private readonly ILogger<UserDataPathService> _logger;
    private readonly string _basePath;
    private static readonly object _directoryLock = new();

    public UserDataPathService(ILogger<UserDataPathService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Set up base path: AppData/Local/Mentor
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mentor"
        );

        _logger.LogInformation("UserDataPathService initialized with base path: {BasePath}", _basePath);
    }

    public string GetBasePath()
    {
        return _basePath;
    }

    public string GetRulesPath(string gameName)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            throw new ArgumentException("Game name cannot be null or empty", nameof(gameName));
        }

        // AppData/Local/Mentor/Data/rules/{gameName}
        var rulesPath = Path.Combine(_basePath, "data", "rules", gameName);
        return rulesPath;
    }

    public string GetSavedAnalysisPath()
    {
        // AppData/Local/Mentor/Saved Analysis
        var analysisPath = Path.Combine(_basePath, "Saved Analysis");
        return analysisPath;
    }

    public string GetCachePath()
    {
        // AppData/Local/Mentor/Cache
        var cachePath = Path.Combine(_basePath, "Cache");
        return cachePath;
    }

    public void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        // Thread-safe directory creation
        lock (_directoryLock)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);
            }
        }
    }
}

