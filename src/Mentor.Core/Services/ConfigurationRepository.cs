using LiteDB;
using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Services;

public static class ConfigurationRepositoryExtensions
{
    public static IServiceCollection AddConfigurationRepository(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationRepository>(sp =>
        {
            // Use a consistent database path
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mentor",
                "mentor.db"
            );

            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            var logger = sp.GetRequiredService<ILogger<ConfigurationRepository>>();
            logger.LogInformation($"Using database path: {dbPath}");

            return new ConfigurationRepository(dbPath);
        });

        return services;
    }
}

public class ConfigurationRepository : IConfigurationRepository, IDisposable
{
    private readonly LiteDatabase _database;

    public ConfigurationRepository(string? databasePath = null)
    {
        var path = databasePath ?? "mentor.db";
        _database = new LiteDatabase(path);
        
        
        var collection = _database.GetCollection<ProviderConfigurationEntity>("providers");
        collection.EnsureIndex(x => x.Id);
        var toolsCollection = _database.GetCollection<ToolConfigurationEntity>("tools");
        toolsCollection.EnsureIndex(x => x.Id);

    }

    public Task<ProviderConfigurationEntity?> GetProviderByNameAsync(string name)
    {
        var collection = _database.GetCollection<ProviderConfigurationEntity>("providers");
        var entity = collection.FindOne(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity)!;
    }

    public Task<IList<ProviderConfigurationEntity>> GetAllProvidersAsync()
    {
        var collection = _database.GetCollection<ProviderConfigurationEntity>("providers");
        var entities = collection.FindAll().ToList();
        return Task.FromResult<IList<ProviderConfigurationEntity>>(entities);
    }

    public Task<ProviderConfigurationEntity> SaveProviderAsync(ProviderConfigurationEntity config)
    {
        // Must have a name field - or throw
        var name = config.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name cannot be null or empty.");
        }

        var collection = _database.GetCollection<ProviderConfigurationEntity>("providers");
        collection.EnsureIndex(x => x.Id);

        var existingProvider = collection.FindOne(p => p.Id.Equals(config.Id, StringComparison.OrdinalIgnoreCase));

        if (existingProvider != null)
        {
            // Update existing provider
            // If config.Name is different, check that the new name is unique
            if (!string.IsNullOrEmpty(config.Name) && !config.Name.Equals(existingProvider.Name, StringComparison.OrdinalIgnoreCase))
            {
                var duplicateName = collection.FindOne(p =>
                    p.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase) && p.Id != existingProvider.Id);

                if (duplicateName != null)
                {
                    throw new InvalidOperationException($"A provider with the name '{config.Name}' already exists.");
                }

                existingProvider.Name = config.Name;
            }

            existingProvider.ProviderType = config.ProviderType;
            existingProvider.ApiKey = config.ApiKey;
            existingProvider.Model = config.Model;
            existingProvider.BaseUrl = config.BaseUrl;
            existingProvider.Timeout = config.Timeout;
            existingProvider.SearchWeb = config.SearchWeb;

            collection.Update(existingProvider);
            
            // Return the updated entity
            return Task.FromResult(existingProvider);
        }
        else
        {
            // Creating new provider - check name is unique
            var duplicateName = collection.FindOne(p =>
                p.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase));

            if (duplicateName != null)
            {
                throw new InvalidOperationException($"A provider with the name '{name}' already exists.");
            }

            // Create new
            var newProvider = new ProviderConfigurationEntity
            {
                Id = string.IsNullOrEmpty(config.Id) ? Guid.NewGuid().ToString() : config.Id,
                Name = name,
                ProviderType = config.ProviderType,
                ApiKey = config.ApiKey,
                Model = config.Model,
                BaseUrl = config.BaseUrl,
                Timeout = config.Timeout,
                SearchWeb = config.SearchWeb,
                CreatedAt = DateTimeOffset.UtcNow
            };
            collection.Insert(newProvider);
            
            // Return the newly inserted entity
            return Task.FromResult(newProvider);
        }
    }

    public Task DeleteProviderAsync(string id)
    {
        var collection = _database.GetCollection<ProviderConfigurationEntity>("providers");
        var provider = collection.FindOne(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (provider != null)
        {
            collection.Delete(provider.Id);
        }

        return Task.CompletedTask;
    }

    public Task<ToolConfigurationEntity?> GetToolByNameAsync(string toolName)
    {
        var collection = _database.GetCollection<ToolConfigurationEntity>("tools");
        var tool = collection.FindOne(t => t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<ToolConfigurationEntity?>(tool);
    }

    public Task<IList<ToolConfigurationEntity>> GetAllToolsAsync()
    {
        var collection = _database.GetCollection<ToolConfigurationEntity>("tools");
        var tools = collection.FindAll().ToList();
        return Task.FromResult<IList<ToolConfigurationEntity>>(tools);
    }

    public Task<ToolConfigurationEntity> SaveToolAsync(ToolConfigurationEntity config)
    {
        var collection = _database.GetCollection<ToolConfigurationEntity>("tools");
        collection.EnsureIndex(x => x.Id);
        var existingTool = collection.FindOne(t => t.Id.Equals(config.Id, StringComparison.OrdinalIgnoreCase));

        if (existingTool != null)
        {
            // Update existing
            existingTool.ApiKey = config.ApiKey;
            existingTool.BaseUrl = config.BaseUrl;
            existingTool.Timeout = config.Timeout;
            existingTool.MaxArticleLength = config.MaxArticleLength;
            collection.Update(existingTool);
            
            // Return the updated entity
            return Task.FromResult(existingTool);
        }
        else
        {
            // Ensure we have an ID
            if (string.IsNullOrEmpty(config.Id))
            {
                config.Id = Guid.NewGuid().ToString();
            }
            
            // Set creation timestamp
            if (config.CreatedAt == default)
            {
                config.CreatedAt = DateTimeOffset.UtcNow;
            }
            
            collection.Insert(config);
            
            // Return the newly inserted entity
            return Task.FromResult(config);
        }
    }

    public Task DeleteToolAsync(string id)
    {
        var collection = _database.GetCollection<ToolConfigurationEntity>("tools");
        var tool = collection.FindOne(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (tool != null)
        {
            collection.Delete(tool.Id);
        }

        return Task.CompletedTask;
    }

    public Task SeedDefaultsAsync()
    {
        var providerCollection = _database.GetCollection<ProviderConfigurationEntity>("providers");
        var toolCollection = _database.GetCollection<ToolConfigurationEntity>("tools");

        // Check if database is empty
        var hasProviders = providerCollection.Count() > 0;
        var hasTools = toolCollection.Count() > 0;

        if (hasProviders && hasTools)
        {
            return Task.CompletedTask; // Already seeded
        }

        // Seed providers if empty
        if (!hasProviders)
        {
            // Seed Perplexity provider
            var perplexityProvider = new ProviderConfigurationEntity
            {
                Name = "Perplexity",
                ProviderType = "perplexity",
                Model = "sonar",
                BaseUrl = "https://api.perplexity.ai",
                ApiKey = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };
            providerCollection.Insert(perplexityProvider);

            // Seed Local LLM provider
            var localProvider = new ProviderConfigurationEntity
            {
                Name = "Local LLM",
                ProviderType = "openai",
                Model = "google/gemma-3-27b",
                BaseUrl = "http://localhost:1234/v1",
                ApiKey = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };
            providerCollection.Insert(localProvider);
        }

        // Seed tools if empty
        if (!hasTools)
        {
            var braveTool = new ToolConfigurationEntity
            {
                ToolName = "Brave",
                ApiKey = string.Empty,
                BaseUrl = "https://api.search.brave.com/res/v1/web/search",
                Timeout = 30,
                MaxArticleLength = 2000,
                CreatedAt = DateTimeOffset.UtcNow
            };
            toolCollection.Insert(braveTool);

            var articleReaderTool = new ToolConfigurationEntity
            {
                ToolName = "article-reader",
                ApiKey = string.Empty,
                BaseUrl = string.Empty,
                Timeout = 30,
                MaxArticleLength = 2000,
                CreatedAt = DateTimeOffset.UtcNow
            };
            toolCollection.Insert(articleReaderTool);
        }

        return Task.CompletedTask;
    }

    public Task<UIStateEntity> GetUIStateAsync()
    {
        var collection = _database.GetCollection<UIStateEntity>("uistate");
        var uiState = collection.FindOne(u => u.Name.Equals("default"));

        if (uiState == null)
        {
            return Task.FromResult(new UIStateEntity { Name = "default" });
        }

        return Task.FromResult(uiState);
    }

    public Task SaveUIStateAsync(UIStateEntity state)
    {
        var collection = _database.GetCollection<UIStateEntity>("uistate");
        collection.EnsureIndex(x => x.Name);
        var existingState = collection.FindOne(u => u.Name.Equals("default"));

        if (existingState != null)
        {
            // Update existing state
            existingState.LastImagePath = state.LastImagePath;
            existingState.LastPrompt = state.LastPrompt;
            existingState.LastProvider = state.LastProvider;
            existingState.LastGameName = state.LastGameName;
            existingState.UpdatedAt = DateTimeOffset.UtcNow;
            collection.Update(existingState);
        }
        else
        {
            // Create new state
            var newState = new UIStateEntity
            {
                Name = "default",
                LastImagePath = state.LastImagePath,
                LastPrompt = state.LastPrompt,
                LastProvider = state.LastProvider,
                LastGameName = state.LastGameName,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            collection.Insert(newState);
        }

        return Task.CompletedTask;
    }

    public Task<IList<string>> GetAvailableProviderTypesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            "openai",
            "perplexity"
        });
    }

    public Task<WindowStateEntity?> GetWindowStateAsync(string windowName)
    {
        var collection = _database.GetCollection<WindowStateEntity>("windowstate");
        collection.EnsureIndex(x => x.WindowName);
        var windowState = collection.FindOne(w => w.WindowName.Equals(windowName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<WindowStateEntity?>(windowState);
    }

    public Task SaveWindowStateAsync(WindowStateEntity windowState)
    {
        var collection = _database.GetCollection<WindowStateEntity>("windowstate");
        collection.EnsureIndex(x => x.WindowName);
        var existingState = collection.FindOne(w => w.WindowName.Equals(windowState.WindowName, StringComparison.OrdinalIgnoreCase));

        if (existingState != null)
        {
            // Update existing state
            existingState.X = windowState.X;
            existingState.Y = windowState.Y;
            existingState.Width = windowState.Width;
            existingState.Height = windowState.Height;
            existingState.UpdatedAt = DateTimeOffset.UtcNow;
            collection.Update(existingState);
        }
        else
        {
            // Create new state
            windowState.UpdatedAt = DateTimeOffset.UtcNow;
            collection.Insert(windowState);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}