using Mentor.Core.Configuration;
using Mentor.Core.Data;

namespace Mentor.Core.Interfaces;

public interface IConfigurationRepository
{
    /// <summary>
    /// Gets the currently active provider configuration
    /// </summary>
    Task<ProviderConfiguration?> GetActiveProviderAsync();
    
    /// <summary>
    /// Gets a provider configuration by name
    /// </summary>
    Task<ProviderConfiguration?> GetProviderByNameAsync(string name);
    
    /// <summary>
    /// Gets all provider configurations
    /// </summary>
    Task<IList<ProviderConfiguration>> GetAllProvidersAsync();
    
    /// <summary>
    /// Sets the active provider by name
    /// </summary>
    Task SetActiveProviderAsync(string name);
    
    /// <summary>
    /// Saves a provider configuration
    /// </summary>
    Task SaveProviderAsync(string name, ProviderConfiguration config);
    
    /// <summary>
    /// Deletes a provider configuration by name
    /// </summary>
    Task DeleteProviderAsync(string name);
    
    
    /// <summary>
    /// Seeds default configurations if database is empty
    /// </summary>
    Task SeedDefaultsAsync();

    /// <summary>
    /// Gets a tool configuration by name
    /// </summary>
    Task<RealWebtoolToolConfiguration?> GetToolByNameAsync(string toolName);
    
    /// <summary>
    /// Gets all tool configurations
    /// </summary>
    Task<IList<RealWebtoolToolConfiguration>> GetAllToolsAsync();
    
    /// <summary>
    /// Saves a tool configuration
    /// </summary>
    Task SaveToolAsync(string toolName, RealWebtoolToolConfiguration config);
    
    /// <summary>
    /// Deletes a tool configuration by name
    /// </summary>
    Task DeleteToolAsync(string toolName);
}

