using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Mentor.Core.Interfaces;
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
            await _configurationRepository.SaveProviderAsync(config);
            
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
            await _configurationRepository.SaveToolAsync(config);
            
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
