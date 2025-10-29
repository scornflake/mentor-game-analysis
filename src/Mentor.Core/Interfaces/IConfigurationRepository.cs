using Mentor.Core.Data;

namespace Mentor.Core.Interfaces;

public interface IConfigurationRepository
{
    /// <summary>
    /// Gets a provider configuration by name
    /// </summary>
    Task<ProviderConfigurationEntity?> GetProviderByNameAsync(string name);
    
    /// <summary>
    /// Gets all provider configurations
    /// </summary>
    Task<IList<ProviderConfigurationEntity>> GetAllProvidersAsync();
    
    /// <summary>
    /// Saves a provider configuration
    /// </summary>
    Task<ProviderConfigurationEntity> SaveProviderAsync(ProviderConfigurationEntity config);
    
    /// <summary>
    /// Deletes a provider configuration by name
    /// </summary>
    Task DeleteProviderAsync(string id);
    
    
    /// <summary>
    /// Seeds default configurations if database is empty
    /// </summary>
    Task SeedDefaultsAsync();

    /// <summary>
    /// Gets a tool configuration by name
    /// </summary>
    Task<ToolConfigurationEntity?> GetToolByNameAsync(string toolName);
    
    /// <summary>
    /// Gets all tool configurations
    /// </summary>
    Task<IList<ToolConfigurationEntity>> GetAllToolsAsync();
    
    /// <summary>
    /// Saves a tool configuration
    /// </summary>
    Task<ToolConfigurationEntity> SaveToolAsync(ToolConfigurationEntity config);
    
    /// <summary>
    /// Deletes a tool configuration by name
    /// </summary>
    Task DeleteToolAsync(string id);
    
    /// <summary>
    /// Gets the saved UI state (last image path, prompt, provider, and game name)
    /// </summary>
    Task<UIStateEntity> GetUIStateAsync();
    
    /// <summary>
    /// Saves the UI state (last image path, prompt, provider, and game name)
    /// </summary>
    Task SaveUIStateAsync(UIStateEntity state);
    
    /// <summary>
    /// Gets the list of available provider types (e.g., "openai", "perplexity")
    /// </summary>
    Task<IList<string>> GetAvailableProviderTypesAsync();
    
    /// <summary>
    /// Gets the saved window state (position and size)
    /// </summary>
    Task<WindowStateEntity?> GetWindowStateAsync(string windowName);
    
    /// <summary>
    /// Saves the window state (position and size)
    /// </summary>
    Task SaveWindowStateAsync(WindowStateEntity windowState);
}

