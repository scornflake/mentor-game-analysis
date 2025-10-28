using System.Collections.Concurrent;
using Mentor.Core.Configuration;
using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Realms;

namespace Mentor.Core.Services;

/// <summary>
/// A TaskScheduler that executes all tasks on a single dedicated thread.
/// This ensures Realm access is always on the same thread.
/// </summary>
internal sealed class SingleThreadTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _tasks = new();
    private readonly Thread _thread;
    private volatile bool _disposed;

    public SingleThreadTaskScheduler()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "RealmDedicatedThread"
        };
        _thread.Start();
    }

    private void Run()
    {
        foreach (var task in _tasks.GetConsumingEnumerable())
        {
            if (_disposed) break;
            TryExecuteTask(task);
        }
    }

    protected override void QueueTask(Task task)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingleThreadTaskScheduler));
        
        _tasks.Add(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // Only allow inline execution if we're on the scheduler's thread
        return Thread.CurrentThread == _thread && TryExecuteTask(task);
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return _tasks.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _tasks.CompleteAdding();
        
        // Give thread time to finish current task
        if (!_thread.Join(TimeSpan.FromSeconds(5)))
        {
            // Thread didn't exit gracefully, but we tried
        }
        
        _tasks.Dispose();
    }
}

public static class RealmConfigurationRepositoryExtensions
{
    public static IServiceCollection AddRealmConfigurationRepository(this IServiceCollection services)
    {
        // Create a single-thread scheduler for all Realm operations
        var scheduler = new SingleThreadTaskScheduler();
        
        services.AddSingleton<IConfigurationRepository>(sp =>
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

            return new RealmConfigurationRepository(dbPath, scheduler);
        });
        
        return services;
    }
}

public class RealmConfigurationRepository : IConfigurationRepository
{
    private readonly Realm _realm;
    private readonly TaskScheduler? _scheduler;

    public RealmConfigurationRepository(string? databasePath = null, TaskScheduler? scheduler = null)
    {
        _scheduler = scheduler;
        
        if (_scheduler != null)
        {
            // Initialize Realm on the scheduler's thread
            var initTask = Task.Factory.StartNew(() =>
            {
                return InitializeRealm(databasePath);
            }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            
            _realm = initTask.Result;
        }
        else
        {
            // Initialize Realm synchronously on the current thread (for tests)
            _realm = InitializeRealm(databasePath);
        }
    }

    private static Realm InitializeRealm(string? databasePath)
    {
        var config = new RealmConfiguration(databasePath ?? "mentor.realm")
        {
            SchemaVersion = 3,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                // Migration from version 1 to 2: Added RealmUIState
                if (oldSchemaVersion < 2)
                {
                    // No data migration needed, just schema addition
                    // RealmUIState will be created on first use
                }
                
                // Migration from version 2 to 3: Removed IsActive from RealmProviderConfiguration
                // Id remains the primary key, but Name must be unique (enforced in code)
                if (oldSchemaVersion < 3)
                {
                    // Realm handles schema changes automatically
                    // IsActive field will be removed
                }
            }
        };
        return Realm.GetInstance(config);
    }

    private Task<T> ExecuteOnRealmThread<T>(Func<T> operation)
    {
        if (_scheduler != null)
        {
            return Task.Factory.StartNew(operation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        }
        
        // No scheduler - execute synchronously (for tests)
        return Task.FromResult(operation());
    }

    private Task ExecuteOnRealmThread(Action operation)
    {
        if (_scheduler != null)
        {
            return Task.Factory.StartNew(operation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        }
        
        // No scheduler - execute synchronously (for tests)
        operation();
        return Task.CompletedTask;
    }

    public Task<ProviderConfiguration?> GetProviderByNameAsync(string name)
    {
        return ExecuteOnRealmThread(() =>
        {
            var realmProviderConfigurations = _realm.All<RealmProviderConfiguration>();
            var realmConfig = realmProviderConfigurations
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return realmConfig != null ? MapToProviderConfiguration(realmConfig) : null;
        });
    }

    public Task<IList<ProviderConfiguration>> GetAllProvidersAsync()
    {
        return ExecuteOnRealmThread(() =>
        {
            var realmConfigs = _realm.All<RealmProviderConfiguration>().ToList();
            return (IList<ProviderConfiguration>)realmConfigs.Select(MapToProviderConfiguration).ToList();
        });
    }

    public Task SaveProviderAsync(ProviderConfiguration config)
    {
        // Must have a name field - or throw
        var name = config.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name cannot be null or empty.");
        }
            

        return ExecuteOnRealmThread(() =>
        {
            _realm.Write(() =>
            {
                var existingProvider = _realm.All<RealmProviderConfiguration>()
                    .FirstOrDefault(p => p.Name.Equals(config.Id, StringComparison.OrdinalIgnoreCase));

                if (existingProvider != null)
                {
                    // Update existing provider
                    // If config.Name is different, check that the new name is unique
                    if (!string.IsNullOrEmpty(config.Name) && !config.Name.Equals(existingProvider.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var duplicateName = _realm.All<RealmProviderConfiguration>()
                            .Any(p => p.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase) && p.Id != existingProvider.Id);
                        
                        if (duplicateName)
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
                }
                else
                {
                    // Creating new provider - check name is unique
                    var duplicateName = _realm.All<RealmProviderConfiguration>()
                        .Any(p => p.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (duplicateName)
                    {
                        throw new InvalidOperationException($"A provider with the name '{name}' already exists.");
                    }
                    
                    // Create new
                    var newProvider = new RealmProviderConfiguration
                    {
                        Name = name,
                        ProviderType = config.ProviderType,
                        ApiKey = config.ApiKey,
                        Model = config.Model,
                        BaseUrl = config.BaseUrl,
                        Timeout = config.Timeout,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _realm.Add(newProvider);
                }
            });
        });
    }

    public Task DeleteProviderAsync(string id)
    {
        return ExecuteOnRealmThread(() =>
        {
            _realm.Write(() =>
            {
                var provider = _realm.All<RealmProviderConfiguration>()
                    .FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (provider != null)
                {
                    _realm.Remove(provider);
                }
            });
        });
    }

    public Task<RealWebtoolToolConfiguration?> GetToolByNameAsync(string toolName)
    {
        return ExecuteOnRealmThread(() =>
        {
            var toolConfig = _realm.All<RealWebtoolToolConfiguration>()
                .FirstOrDefault(t => t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase));
            return toolConfig;
        });
    }

    public Task<IList<RealWebtoolToolConfiguration>> GetAllToolsAsync()
    {
        return ExecuteOnRealmThread(() =>
        {
            var realmConfigs = _realm.All<RealWebtoolToolConfiguration>().ToList();
            return (IList<RealWebtoolToolConfiguration>)realmConfigs.ToList();
        });
    }

    public Task SaveToolAsync(RealWebtoolToolConfiguration config)
    {
        return ExecuteOnRealmThread(() =>
        {
            _realm.Write(() =>
            {
                var existingTool = _realm.All<RealWebtoolToolConfiguration>()
                    .FirstOrDefault(t => t.Id.Equals(config.Id, StringComparison.OrdinalIgnoreCase));

                if (existingTool != null)
                {
                    // Update existing
                    existingTool.ApiKey = config.ApiKey;
                    existingTool.BaseUrl = config.BaseUrl;
                    existingTool.Timeout = config.Timeout;
                }
                else
                {
                    _realm.Add(config);
                }
            });
        });
    }

    public Task DeleteToolAsync(string id)
    {
        return ExecuteOnRealmThread(() =>
        {
            _realm.Write(() =>
            {
                var tool = _realm.All<RealWebtoolToolConfiguration>()
                    .FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (tool != null)
                {
                    _realm.Remove(tool);
                }
            });
        });
    }

    public Task SeedDefaultsAsync()
    {
        return ExecuteOnRealmThread(() =>
        {
            // Check if database is empty
            var hasProviders = _realm.All<RealmProviderConfiguration>().Any();
            var hasTools = _realm.All<RealWebtoolToolConfiguration>().Any();

            if (hasProviders && hasTools)
            {
                return; // Already seeded
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
        });
    }


    private static ProviderConfiguration MapToProviderConfiguration(RealmProviderConfiguration realmConfig)
    {
        return new ProviderConfiguration
        {
            Id = realmConfig.Id,
            Name = realmConfig.Name,
            ProviderType = realmConfig.ProviderType,
            ApiKey = realmConfig.ApiKey,
            Model = realmConfig.Model,
            BaseUrl = realmConfig.BaseUrl,
            Timeout = realmConfig.Timeout
        };
    }

    public Task<(string? ImagePath, string? Prompt, string? Provider)> GetUIStateAsync()
    {
        return ExecuteOnRealmThread(() =>
        {
            var uiState = _realm.All<RealmUIState>()
                .FirstOrDefault(s => s.Id == "ui_state_singleton");

            if (uiState == null)
            {
                return ((string?)null, (string?)null, (string?)null);
            }

            return (uiState.LastImagePath, uiState.LastPrompt, uiState.LastProvider);
        });
    }

    public Task SaveUIStateAsync(string? imagePath, string? prompt, string? provider)
    {
        return ExecuteOnRealmThread(() =>
        {
            _realm.Write(() =>
            {
                var existingState = _realm.All<RealmUIState>()
                    .FirstOrDefault(s => s.Id == "ui_state_singleton");

                if (existingState != null)
                {
                    // Update existing state
                    existingState.LastImagePath = imagePath;
                    existingState.LastPrompt = prompt;
                    existingState.LastProvider = provider;
                    existingState.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    // Create new state
                    var newState = new RealmUIState
                    {
                        Id = "ui_state_singleton",
                        LastImagePath = imagePath,
                        LastPrompt = prompt,
                        LastProvider = provider,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    _realm.Add(newState);
                }
            });
        });
    }

    public Task<IList<string>> GetAvailableProviderTypesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            "openai",
            "perplexity"
        });
    }

    public void Dispose()
    {
        if (_scheduler != null)
        {
            // Dispose on the Realm thread
            var disposeTask = Task.Factory.StartNew(() =>
            {
                _realm?.Dispose();
            }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            
            disposeTask.Wait();
            
            // Dispose the scheduler if it's our custom one
            if (_scheduler is SingleThreadTaskScheduler singleThreadScheduler)
            {
                singleThreadScheduler.Dispose();
            }
        }
        else
        {
            // Synchronous disposal for tests
            _realm?.Dispose();
        }
    }
}
