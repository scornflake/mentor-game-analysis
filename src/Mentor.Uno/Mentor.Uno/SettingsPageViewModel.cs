using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mentor.Core.Interfaces;
using Mentor.Uno.Messages;
using Mentor.Uno.ViewModels;

namespace Mentor.Uno;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMessenger _messenger;
    private CancellationTokenSource? _debounceCts;

    [ObservableProperty] private string _saveStatusMessage = string.Empty;
    [ObservableProperty] private bool _isSaveStatusVisible;

    public ObservableCollection<ProviderViewModel> Providers { get; } = new();
    public ObservableCollection<ToolViewModel> Tools { get; } = new();

    public SettingsPageViewModel(IConfigurationRepository configurationRepository, IMessenger messenger)
    {
        _configurationRepository = configurationRepository;
        _messenger = messenger;
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadProvidersAsync();
        await LoadToolsAsync();
    }

    private async Task LoadProvidersAsync()
    {
        var providers = await _configurationRepository.GetAllProvidersAsync();
        var activeProvider = await _configurationRepository.GetActiveProviderAsync();

        Providers.Clear();
        foreach (var provider in providers)
        {
            var name = GetProviderName(provider);
            var providerVm = new ProviderViewModel(name, provider)
            {
                IsActive = activeProvider != null && GetProviderName(activeProvider) == name
            };
            
            // Subscribe to property changes for auto-save
            providerVm.PropertyChanged += (s, e) =>
            {
                if (s is ProviderViewModel pvm)
                {
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

    private string GetProviderName(Core.Configuration.ProviderConfiguration provider)
    {
        // Try to infer a friendly name from the configuration
        if (provider.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return "Local LLM";
        }
        if (provider.BaseUrl.Contains("perplexity", StringComparison.OrdinalIgnoreCase))
        {
            return "Perplexity";
        }
        if (provider.BaseUrl.Contains("openai", StringComparison.OrdinalIgnoreCase))
        {
            return "OpenAI";
        }
        return $"{provider.ProviderType} ({provider.BaseUrl})";
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
        _messenger.Send(new ProvidersChangedMessage());
    }

    [RelayCommand]
    private async Task DeleteProvider(ProviderViewModel? provider)
    {
        if (provider == null) return;

        await _configurationRepository.DeleteProviderAsync(provider.OriginalName);
        Providers.Remove(provider);
        
        _messenger.Send(new ProvidersChangedMessage());
        
        await ShowSaveCompleteAsync();
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
        _messenger.Send(new ToolsChangedMessage());
    }

    [RelayCommand]
    private async Task DeleteTool(ToolViewModel? tool)
    {
        if (tool == null) return;

        await _configurationRepository.DeleteToolAsync(tool.OriginalToolName);
        Tools.Remove(tool);
        
        _messenger.Send(new ToolsChangedMessage());
        
        await ShowSaveCompleteAsync();
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
        try
        {
            var config = provider.ToConfiguration();
            await _configurationRepository.SaveProviderAsync(provider.Name, config);
            
            // Handle active provider status
            if (provider.IsActive)
            {
                await _configurationRepository.SetActiveProviderAsync(provider.Name);
            }
            
            _messenger.Send(new ProvidersChangedMessage());
            
            await ShowSaveCompleteAsync();
        }
        catch (Exception)
        {
            // Handle errors silently or log them
        }
    }

    private async Task SaveToolAsync(ToolViewModel tool)
    {
        try
        {
            var config = tool.ToConfiguration();
            await _configurationRepository.SaveToolAsync(tool.ToolName, config);
            
            _messenger.Send(new ToolsChangedMessage());
            
            await ShowSaveCompleteAsync();
        }
        catch (Exception)
        {
            // Handle errors silently or log them
        }
    }

    private async Task ShowSaveCompleteAsync()
    {
        SaveStatusMessage = "Save complete...";
        IsSaveStatusVisible = true;

        await Task.Delay(2500);

        IsSaveStatusVisible = false;
        SaveStatusMessage = string.Empty;
    }
}

