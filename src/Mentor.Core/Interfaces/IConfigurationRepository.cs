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
    Task SaveProviderAsync(ProviderConfigurationEntity config);
    
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
    Task SaveToolAsync(ToolConfigurationEntity config);
    
    /// <summary>
    /// Deletes a tool configuration by name
    /// </summary>
    Task DeleteToolAsync(string id);
    
    /// <summary>
    /// Gets the saved UI state (last image path, prompt, and provider)
    /// </summary>
    Task<(string? ImagePath, string? Prompt, string? Provider)> GetUIStateAsync();
    
    /// <summary>
    /// Saves the UI state (last image path, prompt, and provider)
    /// </summary>
    Task SaveUIStateAsync(string? imagePath, string? prompt, string? provider);
    
    /// <summary>
    /// Gets the list of available provider types (e.g., "openai", "perplexity")
    /// </summary>
    Task<IList<string>> GetAvailableProviderTypesAsync();
}

