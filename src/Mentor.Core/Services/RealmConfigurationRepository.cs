using Mentor.Core.Configuration;
using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Realms;

namespace Mentor.Core.Services;

public static class RealmConfigurationRepositoryExtensions
{
    public static IServiceCollection AddRealmConfigurationRepository(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationRepository, RealmConfigurationRepository>(sp =>
        {
            // Use a consistent database path
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mentor",
                "mentor.realm"
            );

            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            var logger = sp.GetRequiredService<ILogger<RealmConfigurationRepository>>();
            logger.LogInformation($"Using database path: {dbPath}");

            return new RealmConfigurationRepository(dbPath);
        });
        return services;
    }
}

public class RealmConfigurationRepository : IConfigurationRepository
{
    private readonly Realm _realm;

    public RealmConfigurationRepository(string? databasePath = null)
    {
        var config = new RealmConfiguration(databasePath ?? "mentor.realm")
        {
            SchemaVersion = 1
        };
        _realm = Realm.GetInstance(config);
    }

    public Task<ProviderConfiguration?> GetActiveProviderAsync()
    {
        var realmConfig = _realm.All<RealmProviderConfiguration>()
            .FirstOrDefault(p => p.IsActive);

        return Task.FromResult(realmConfig != null ? MapToProviderConfiguration(realmConfig) : null);
    }

    public Task<ProviderConfiguration?> GetProviderByNameAsync(string name)
    {
        var realmProviderConfigurations = _realm.All<RealmProviderConfiguration>();
        var realmConfig = realmProviderConfigurations
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(realmConfig != null ? MapToProviderConfiguration(realmConfig) : null);
    }

    public Task<IList<ProviderConfiguration>> GetAllProvidersAsync()
    {
        var realmConfigs = _realm.All<RealmProviderConfiguration>().ToList();
        return Task.FromResult<IList<ProviderConfiguration>>(realmConfigs.Select(MapToProviderConfiguration).ToList());
    }

    public Task SetActiveProviderAsync(string name)
    {
        _realm.Write(() =>
        {
            // Deactivate all providers
            var allProviders = _realm.All<RealmProviderConfiguration>();
            foreach (var provider in allProviders)
            {
                provider.IsActive = false;
            }

            // Activate the specified provider
            var targetProvider = allProviders
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (targetProvider == null)
            {
                throw new ArgumentException($"Provider '{name}' not found", nameof(name));
            }

            targetProvider.IsActive = true;
        });

        return Task.CompletedTask;
    }

    public Task SaveProviderAsync(string name, ProviderConfiguration config)
    {
        _realm.Write(() =>
        {
            var existingProvider = _realm.All<RealmProviderConfiguration>()
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingProvider != null)
            {
                // Update existing
                existingProvider.Name = config.Name;
                existingProvider.ProviderType = config.ProviderType;
                existingProvider.ApiKey = config.ApiKey;
                existingProvider.Model = config.Model;
                existingProvider.BaseUrl = config.BaseUrl;
                existingProvider.Timeout = config.Timeout;
            }
            else
            {
                // Create new
                var newProvider = new RealmProviderConfiguration
                {
                    Name = name,
                    ProviderType = config.ProviderType,
                    ApiKey = config.ApiKey,
                    Model = config.Model,
                    BaseUrl = config.BaseUrl,
                    Timeout = config.Timeout,
                    IsActive = false,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _realm.Add(newProvider);
            }
        });

        return Task.CompletedTask;
    }

    public Task DeleteProviderAsync(string name)
    {
        _realm.Write(() =>
        {
            var provider = _realm.All<RealmProviderConfiguration>()
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (provider != null)
            {
                _realm.Remove(provider);
            }
        });

        return Task.CompletedTask;
    }

    public Task<RealWebtoolToolConfiguration?> GetToolByNameAsync(string toolName)
    {
        var toolConfig = _realm.All<RealWebtoolToolConfiguration>()
            .FirstOrDefault(t => t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(toolConfig);
    }

    public Task<IList<RealWebtoolToolConfiguration>> GetAllToolsAsync()
    {
        var realmConfigs = _realm.All<RealWebtoolToolConfiguration>().ToList();
        return Task.FromResult<IList<RealWebtoolToolConfiguration>>(realmConfigs.ToList());
    }

    public Task SaveToolAsync(string toolName, RealWebtoolToolConfiguration config)
    {
        _realm.Write(() =>
        {
            var existingTool = _realm.All<RealWebtoolToolConfiguration>()
                .FirstOrDefault(t => t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase));

            if (existingTool != null)
            {
                // Update existing
                existingTool.ApiKey = config.ApiKey;
                existingTool.BaseUrl = config.BaseUrl;
                existingTool.Timeout = config.Timeout;
            }
            else
            {
                // Create new
                var newTool = new RealWebtoolToolConfiguration
                {
                    ToolName = toolName,
                    ApiKey = config.ApiKey,
                    BaseUrl = config.BaseUrl,
                    Timeout = config.Timeout,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _realm.Add(newTool);
            }
        });

        return Task.CompletedTask;
    }

    public Task DeleteToolAsync(string toolName)
    {
        _realm.Write(() =>
        {
            var tool = _realm.All<RealWebtoolToolConfiguration>()
                .FirstOrDefault(t => t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase));

            if (tool != null)
            {
                _realm.Remove(tool);
            }
        });

        return Task.CompletedTask;
    }

    public Task SeedDefaultsAsync()
    {
        // Check if database is empty
        var hasProviders = _realm.All<RealmProviderConfiguration>().Any();
        var hasTools = _realm.All<RealWebtoolToolConfiguration>().Any();

        if (hasProviders && hasTools)
        {
            return Task.CompletedTask; // Already seeded
        }

        _realm.Write(() =>
        {
            // Seed providers if empty
            if (!hasProviders)
            {
                // Seed Perplexity provider
                var perplexityProvider = new RealmProviderConfiguration
                {
                    Name = "Perplexity",
                    ProviderType = "perplexity",
                    Model = "sonar",
                    BaseUrl = "https://api.perplexity.ai",
                    ApiKey = string.Empty,
                    IsActive = false,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _realm.Add(perplexityProvider);

                // Seed Local LLM provider
                var localProvider = new RealmProviderConfiguration
                {
                    Name = "Local LLM",
                    ProviderType = "openai",
                    Model = "google/gemma-3-27b",
                    BaseUrl = "http://localhost:1234/v1",
                    ApiKey = string.Empty,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _realm.Add(localProvider);
            }

            // Seed tools if empty
            if (!hasTools)
            {
                var braveTool = new RealWebtoolToolConfiguration
                {
                    ToolName = "Brave",
                    ApiKey = string.Empty,
                    BaseUrl = "https://api.search.brave.com/res/v1/web/search",
                    Timeout = 30,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _realm.Add(braveTool);
            }
        });

        return Task.CompletedTask;
    }


    private static ProviderConfiguration MapToProviderConfiguration(RealmProviderConfiguration realmConfig)
    {
        return new ProviderConfiguration
        {
            Name = realmConfig.Name,
            ProviderType = realmConfig.ProviderType,
            ApiKey = realmConfig.ApiKey,
            Model = realmConfig.Model,
            BaseUrl = realmConfig.BaseUrl,
            Timeout = realmConfig.Timeout
        };
    }

    public void Dispose()
    {
        _realm?.Dispose();
    }
}