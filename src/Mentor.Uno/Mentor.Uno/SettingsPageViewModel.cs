using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Mentor.Core.Interfaces;
using Mentor.Core.Tools;
using Mentor.Uno.Messages;
using Mentor.Uno.ViewModels;
using Microsoft.UI.Dispatching;

namespace Mentor.Uno;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMessenger _messenger;
    private readonly ILogger<SettingsPageViewModel> _logger;
    private readonly DispatcherQueue _queueAtCreationTime;
    private CancellationTokenSource? _debounceCts;

    [ObservableProperty] private string _saveStatusMessage = string.Empty;
    [ObservableProperty] private bool _isSaveStatusVisible;
    
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isErrorVisible;

    [ObservableProperty] private string _perplexityApiKey = string.Empty;
    [ObservableProperty] private bool _isPerplexityApiKeyVisible = false;
    [ObservableProperty] private string _braveApiKey = string.Empty;
    [ObservableProperty] private bool _isBraveApiKeyVisible = false;

    public ObservableCollection<ProviderViewModel> Providers { get; } = new();
    public ObservableCollection<ToolViewModel> Tools { get; } = new();
    public ObservableCollection<string> AvailableProviderTypes { get; } = new();

    public SettingsPageViewModel(IConfigurationRepository configurationRepository, IMessenger messenger, ILogger<SettingsPageViewModel> logger)
    {
        _configurationRepository = configurationRepository;
        _messenger = messenger;
        _logger = logger;
        _queueAtCreationTime = DispatcherQueue.GetForCurrentThread();
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadAvailableProviderTypesAsync();
        await LoadProvidersAsync();
        await LoadToolsAsync();
        await LoadGlobalSettingsAsync();
    }

    private async Task LoadAvailableProviderTypesAsync()
    {
        var providerTypes = await _configurationRepository.GetAvailableProviderTypesAsync();
        
        AvailableProviderTypes.Clear();
        foreach (var providerType in providerTypes)
        {
            AvailableProviderTypes.Add(providerType);
        }
    }

    private async Task LoadProvidersAsync()
    {
        var providers = await _configurationRepository.GetAllProvidersAsync();

        Providers.Clear();
        foreach (var provider in providers)
        {
            // Filter out fixed providers (Perplexity)
            if (provider.ProviderType.ToLower() == KnownProviderTools.Perplexity.ToLower())
            {
                continue;
            }

            var providerVm = new ProviderViewModel(provider);
            
            // Subscribe to property changes for auto-save
            providerVm.PropertyChanged += (s, e) =>
            {
                if (s is ProviderViewModel pvm)
                {
                    _logger.LogInformation("Property changed for provider {ProviderName}, scheduling save...", pvm.Name);
                    DebounceAndSaveProvider(pvm);
                }
            };
            
            Providers.Add(providerVm);
        }
    }

    private async Task LoadToolsAsync()
    {
        var tools = await _configurationRepository.GetAllToolsAsync();

        Tools.Clear();
        foreach (var tool in tools)
        {
            // Filter out fixed tools (Brave)
            if (tool.ToolName.ToLower() == KnownSearchTools.Brave.ToLower())
            {
                continue;
            }

            var toolVm = new ToolViewModel(tool);
            
            // Subscribe to property changes for auto-save
            toolVm.PropertyChanged += (s, e) =>
            {
                if (s is ToolViewModel tvm)
                {
                    DebounceAndSaveTool(tvm);
                }
            };
            
            Tools.Add(toolVm);
        }
    }

    private void NotifyProvidersChanged()
    {
        _queueAtCreationTime.TryEnqueue(() => { _messenger.Send(new ProvidersChangedMessage()); });
    }

    private void NotifyToolsChanged()
    {
        _queueAtCreationTime.TryEnqueue(() => { _messenger.Send(new ToolsChangedMessage()); });
    }

    private async Task LoadGlobalSettingsAsync()
    {
        try
        {
            // Load Perplexity provider API key
            var perplexityProvider = await _configurationRepository.GetProviderByNameAsync("Perplexity");
            if (perplexityProvider != null)
            {
                PerplexityApiKey = perplexityProvider.ApiKey;
            }
            else
            {
                // Try to find by provider type
                var providers = await _configurationRepository.GetAllProvidersAsync();
                var perplexity = providers.FirstOrDefault(p => p.ProviderType.ToLower() == KnownProviderTools.Perplexity.ToLower());
                if (perplexity != null)
                {
                    PerplexityApiKey = perplexity.ApiKey;
                }
            }

            // Load Brave tool API key
            var braveTool = await _configurationRepository.GetToolByNameAsync(KnownSearchTools.Brave);
            if (braveTool != null)
            {
                BraveApiKey = braveTool.ApiKey;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading global settings: {ErrorMessage}", ex.Message);
        }
    }

    partial void OnPerplexityApiKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_perplexityApiKey))
        {
            DebounceAndSavePerplexityApiKey();
        }
    }

    partial void OnBraveApiKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_braveApiKey))
        {
            DebounceAndSaveBraveApiKey();
        }
    }

    [RelayCommand]
    private void TogglePerplexityApiKeyVisibility()
    {
        IsPerplexityApiKeyVisible = !IsPerplexityApiKeyVisible;
    }

    [RelayCommand]
    private void ToggleBraveApiKeyVisibility()
    {
        IsBraveApiKeyVisible = !IsBraveApiKeyVisible;
    }

    private void DebounceAndSavePerplexityApiKey()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                if (!token.IsCancellationRequested)
                {
                    await SavePerplexityApiKeyAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debouncing
            }
        }, token);
    }

    private void DebounceAndSaveBraveApiKey()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                if (!token.IsCancellationRequested)
                {
                    await SaveBraveApiKeyAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debouncing
            }
        }, token);
    }

    private async Task SavePerplexityApiKeyAsync()
    {
        try
        {
            // Find Perplexity provider
            var providers = await _configurationRepository.GetAllProvidersAsync();
            var perplexityProvider = providers.FirstOrDefault(p => p.ProviderType.ToLower() == KnownProviderTools.Perplexity.ToLower());

            if (perplexityProvider != null)
            {
                perplexityProvider.ApiKey = PerplexityApiKey;
                await _configurationRepository.SaveProviderAsync(perplexityProvider);
                NotifyProvidersChanged();
                await ShowSaveCompleteAsync();
            }
            else
            {
                _logger.LogWarning("Perplexity provider not found when saving API key");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Perplexity API key: {ErrorMessage}", ex.Message);
            await ShowErrorAsync($"Failed to save Perplexity API key: {ex.Message}");
        }
    }

    private async Task SaveBraveApiKeyAsync()
    {
        try
        {
            // Find Brave tool
            var braveTool = await _configurationRepository.GetToolByNameAsync(KnownSearchTools.Brave);

            if (braveTool != null)
            {
                braveTool.ApiKey = BraveApiKey;
                await _configurationRepository.SaveToolAsync(braveTool);
                NotifyToolsChanged();
                await ShowSaveCompleteAsync();
            }
            else
            {
                _logger.LogWarning("Brave tool not found when saving API key");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Brave API key: {ErrorMessage}", ex.Message);
            await ShowErrorAsync($"Failed to save Brave API key: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddProvider()
    {
        var newProvider = new ProviderViewModel();
        
        // Subscribe to property changes for auto-save
        newProvider.PropertyChanged += (s, e) =>
        {
            if (s is ProviderViewModel pvm)
            {
                DebounceAndSaveProvider(pvm);
            }
        };
        
        Providers.Add(newProvider);
        
        // Save immediately
        await SaveProviderAsync(newProvider);
        NotifyProvidersChanged();
    }

    [RelayCommand]
    private async Task DeleteProvider(ProviderViewModel? provider)
    {
        if (provider == null) return;

        try
        {
            await _configurationRepository.DeleteProviderAsync(provider.Id);
            Providers.Remove(provider);
            
            NotifyProvidersChanged();
            
            await ShowSaveCompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provider {ProviderName}: {ErrorMessage}", provider.Name, ex.Message);
            await ShowErrorAsync($"Failed to delete provider: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddTool()
    {
        var newTool = new ToolViewModel();
        
        // Subscribe to property changes for auto-save
        newTool.PropertyChanged += (s, e) =>
        {
            if (s is ToolViewModel tvm)
            {
                DebounceAndSaveTool(tvm);
            }
        };
        
        Tools.Add(newTool);
        
        // Save immediately
        await SaveToolAsync(newTool);
        NotifyToolsChanged();
    }

    [RelayCommand]
    private async Task DeleteTool(ToolViewModel? tool)
    {
        if (tool == null) return;

        try
        {
            await _configurationRepository.DeleteToolAsync(tool.Id);
            Tools.Remove(tool);
            
            NotifyToolsChanged();
            
            await ShowSaveCompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tool {ToolName}: {ErrorMessage}", tool.ToolName, ex.Message);
            await ShowErrorAsync($"Failed to delete tool: {ex.Message}");
        }
    }

    private void DebounceAndSaveProvider(ProviderViewModel provider)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                if (!token.IsCancellationRequested)
                {
                    await SaveProviderAsync(provider);
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debouncing
            }
        }, token);
    }

    private void DebounceAndSaveTool(ToolViewModel tool)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                if (!token.IsCancellationRequested)
                {
                    await SaveToolAsync(tool);
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debouncing
            }
        }, token);
    }

    private async Task SaveProviderAsync(ProviderViewModel provider)
    {
        _logger.LogInformation("Saving provider {ProviderName}...", provider.Name);
        try
        {
            var config = provider.ToConfiguration();
            var savedEntity = await _configurationRepository.SaveProviderAsync(config);
            
            // Update the ViewModel with the saved entity (especially the ID) - must run on UI thread
            _queueAtCreationTime.TryEnqueue(() =>
            {
                provider.Id = savedEntity.Id;
                provider.Name = savedEntity.Name;
                provider.ProviderType = savedEntity.ProviderType;
                provider.ApiKey = savedEntity.ApiKey;
                provider.Model = savedEntity.Model;
                provider.BaseUrl = savedEntity.BaseUrl;
                provider.Timeout = savedEntity.Timeout;
                provider.SearchWeb = savedEntity.SearchWeb;
            });
            
            NotifyProvidersChanged();
            
            await ShowSaveCompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving provider {ProviderName}: {ErrorMessage}", provider.Name, ex.Message);
            await ShowErrorAsync($"Failed to save provider: {ex.Message}");
        }
    }

    private async Task SaveToolAsync(ToolViewModel tool)
    {
        try
        {
            var config = tool.ToConfiguration();
            var savedEntity = await _configurationRepository.SaveToolAsync(config);
            
            // Update the ViewModel with the saved entity (especially the ID) - must run on UI thread
            _queueAtCreationTime.TryEnqueue(() =>
            {
                tool.Id = savedEntity.Id;
                tool.ToolName = savedEntity.ToolName;
                tool.ApiKey = savedEntity.ApiKey;
                tool.BaseUrl = savedEntity.BaseUrl;
                tool.Timeout = savedEntity.Timeout;
            });
            
            NotifyToolsChanged();
            
            await ShowSaveCompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tool {ToolName}: {ErrorMessage}", tool.ToolName, ex.Message);
            await ShowErrorAsync($"Failed to save tool: {ex.Message}");
        }
    }

    private async Task ShowSaveCompleteAsync()
    {
        // Ensure UI updates happen on the UI thread
        _queueAtCreationTime.TryEnqueue(() =>
        {
            _logger.LogInformation("Showing save complete message...");
            SaveStatusMessage = "Save complete...";
            IsSaveStatusVisible = true;
        });

        await Task.Delay(2500);

        _queueAtCreationTime.TryEnqueue(() =>
        {
            _logger.LogInformation("Hiding save complete message...");
            IsSaveStatusVisible = false;
            SaveStatusMessage = string.Empty;
        });
    }

    private async Task ShowErrorAsync(string message)
    {
        // Ensure UI updates happen on the UI thread
        _queueAtCreationTime.TryEnqueue(() =>
        {
            _logger.LogInformation("Showing error message: {ErrorMessage}", message);
            ErrorMessage = message;
            IsErrorVisible = true;
        });

        await Task.Delay(5000); // Show errors longer than success messages

        _queueAtCreationTime.TryEnqueue(() =>
        {
            _logger.LogInformation("Hiding error message...");
            IsErrorVisible = false;
            ErrorMessage = string.Empty;
        });
    }
}
