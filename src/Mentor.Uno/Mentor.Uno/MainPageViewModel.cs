using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tools;

namespace Mentor.Uno;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IConfigurationRepository _configurationRepository;

    [ObservableProperty] private string? _imagePath;

    [ObservableProperty] private string _prompt = "How can I make this weapon do more damage against ...";

    [ObservableProperty] private string _selectedProvider = string.Empty;

    [ObservableProperty] private bool _isAnalyzing;

    [ObservableProperty] private Recommendation? _result;

    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<string> Providers { get; } = new();

    public MainPageViewModel(ILLMProviderFactory providerFactory, IConfigurationRepository configurationRepository)
    {
        _providerFactory = providerFactory;
        _configurationRepository = configurationRepository;
        
        // Load providers asynchronously
        _ = InitializeProvidersAsync();
    }
    
    private async Task InitializeProvidersAsync()
    {
        try
        {
            // Load all available providers from the repository
            var providers = await _configurationRepository.GetAllProvidersAsync();
            
            foreach (var provider in providers)
            {
                // Get a friendly name for the provider
                var providerName = GetProviderName(provider);
                Providers.Add(providerName);
            }
            
            // Get the active provider and set it as selected
            var activeProvider = await _configurationRepository.GetActiveProviderAsync();
            if (activeProvider != null)
            {
                SelectedProvider = GetProviderName(activeProvider);
            }
            else if (Providers.Any())
            {
                // If no active provider, select the first one
                SelectedProvider = Providers.First();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading providers: {ex.Message}";
        }
    }
    
    private string GetProviderName(Core.Configuration.ProviderConfiguration provider)
    {
        // Try to infer a friendly name from the configuration
        if (provider.BaseUrl.Contains("localhost"))
        {
            return "Local LLM";
        }
        if (provider.BaseUrl.Contains("perplexity"))
        {
            return "Perplexity";
        }
        if (provider.BaseUrl.Contains("openai"))
        {
            return "OpenAI";
        }
        return $"{provider.ProviderType} ({provider.BaseUrl})";
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
    }

    partial void OnIsAnalyzingChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
    }
}

