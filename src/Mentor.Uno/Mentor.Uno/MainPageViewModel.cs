using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tools;
using Mentor.Uno.Messages;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Mentor.Uno;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMessenger _messenger;
    private readonly ILogger<MainPageViewModel> _logger;
    [ObservableProperty] private string? _imagePath;

    [ObservableProperty] private BitmapImage? _imageSource;

    [ObservableProperty] private string _prompt = "How can I make this weapon do more damage against ...";

    [ObservableProperty] private string _selectedProvider = string.Empty;

    [ObservableProperty] private bool _isAnalyzing;

    [ObservableProperty] private Recommendation? _result;

    [ObservableProperty] private string? _errorMessage;
    
    private bool _systemIsLoaded = false;

    public ObservableCollection<string> Providers { get; } = new();

    public MainPageViewModel(ILLMProviderFactory providerFactory, IConfigurationRepository configurationRepository, IMessenger messenger, ILogger<MainPageViewModel> logger)
    {
        _providerFactory = providerFactory;
        _configurationRepository = configurationRepository;
        _messenger = messenger;
        _logger = logger;
        // Subscribe to providers changed message
        _messenger.Register<ProvidersChangedMessage>(this, (r, m) =>
        {
            _ = ReloadProvidersAsync();
        });
        
        // Load providers asynchronously
        _ = InitializeProvidersAsync();
    }
    
    private async Task InitializeProvidersAsync()
    {
        await LoadProvidersAsync();
        await LoadUIStateAsync();
        _systemIsLoaded = true;
    }
    
    private async Task LoadUIStateAsync()
    {
        _logger.LogInformation("Loading UI state");
        try
        {
            var state = await _configurationRepository.GetUIStateAsync();
            
            // Restore image path
            if (!string.IsNullOrEmpty(state.ImagePath))
            {
                ImagePath = state.ImagePath;
            }
            
            // Restore prompt
            if (!string.IsNullOrEmpty(state.Prompt))
            {
                Prompt = state.Prompt;
            }
            
            // Restore provider selection
            if (!string.IsNullOrEmpty(state.Provider) && Providers.Contains(state.Provider))
            {
                SelectedProvider = state.Provider;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't show to user - this is not critical
            ErrorMessage = $"Error loading saved state: {ex.Message}";
        }
    }
    
    private async Task SaveUIStateAsync()
    {
        if (!_systemIsLoaded)
        {
            return;
        }
        try
        {
            _logger.LogInformation($"Saving UI state: ImagePath={ImagePath}, Prompt={Prompt}, SelectedProvider={SelectedProvider}");
            await _configurationRepository.SaveUIStateAsync(ImagePath, Prompt, SelectedProvider);
        }
        catch
        {
            // Silently fail - saving UI state is not critical
        }
    }

    private async Task ReloadProvidersAsync()
    {
        var currentSelection = SelectedProvider;
        await LoadProvidersAsync();
        
        // Try to maintain selection if provider still exists
        if (!string.IsNullOrEmpty(currentSelection) && Providers.Contains(currentSelection))
        {
            SelectedProvider = currentSelection;
        }
        else if (Providers.Any())
        {
            // Select first available if current selection no longer exists
            SelectedProvider = Providers.First();
        }
        else
        {
            // Clear selection if no providers available
            SelectedProvider = string.Empty;
        }
    }

    private async Task LoadProvidersAsync()
    {
        try
        {
            Providers.Clear();
            
            // Load all available providers from the repository
            var providers = await _configurationRepository.GetAllProvidersAsync();
            
            foreach (var provider in providers)
            {
                // Use the provider's Name property directly
                Providers.Add(provider.Name);
            }
            
            // SelectedProvider will be loaded from UI state in LoadUIStateAsync
            // If no provider is set yet and we have providers, select the first one
            if (string.IsNullOrEmpty(SelectedProvider) && Providers.Any())
            {
                SelectedProvider = Providers.First();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading providers: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    public async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(ImagePath))
            return;

        if (string.IsNullOrWhiteSpace(SelectedProvider))
        {
            ErrorMessage = "Please select a provider";
            return;
        }

        IsAnalyzing = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            // Get the provider configuration by the selected provider name
            var providerConfig = await _configurationRepository.GetProviderByNameAsync(SelectedProvider);
            if (providerConfig == null)
            {
                ErrorMessage = $"Provider '{SelectedProvider}' not found in configuration";
                return;
            }

            // Create the LLM client and analysis service at runtime
            var llmClient = _providerFactory.GetProvider(providerConfig);
            var analysisService = _providerFactory.GetAnalysisService(llmClient);

            // Read the image file
            byte[] imageData;
            try
            {
                imageData = await File.ReadAllBytesAsync(ImagePath);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error reading image file: {ex.Message}";
                return;
            }

            // Create analysis request
            var request = new AnalysisRequest
            {
                ImageData = imageData,
                Prompt = Prompt
            };

            // Perform analysis
            Result = await analysisService.AnalyzeAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Analysis error: {ex.Message}";
            _logger.LogError("Analysis error: {ex}", ex);
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private bool CanAnalyze()
    {
        return !string.IsNullOrWhiteSpace(ImagePath) && !IsAnalyzing;
    }

    partial void OnImagePathChanged(string? value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        _ = SaveUIStateAsync();
        
        // Update the image source
        if (!string.IsNullOrEmpty(value) && File.Exists(value))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.UriSource = new Uri(value, UriKind.Absolute);
                ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image: {ImagePath}", value);
                ImageSource = null;
            }
        }
        else
        {
            ImageSource = null;
        }
    }

    partial void OnIsAnalyzingChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnPromptChanged(string value)
    {
        _ = SaveUIStateAsync();
    }
    
    partial void OnSelectedProviderChanged(string value)
    {
        _ = SaveUIStateAsync();
    }
}

