using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tools;
using Mentor.Uno.Messages;
using Mentor.Uno.Services;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Mentor.Uno;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMessenger _messenger;
    private readonly ILogger<MainPageViewModel> _logger;
    
    private CancellationTokenSource? _analysisCancellationTokenSource;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ImageFileName))]
    private string? _imagePath;

    public string? ImageFileName => string.IsNullOrEmpty(ImagePath) ? null : Path.GetFileName(ImagePath);

    [ObservableProperty] private BitmapImage? _imageSource;
    
    [ObservableProperty] private string? _imageSourceCaption;
    
    private byte[]? _clipboardImageData;

    [ObservableProperty] private string _prompt = "How can I make this weapon do more damage against ...";

    [ObservableProperty] private string _gameName = string.Empty;

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
            if (!string.IsNullOrEmpty(state.LastImagePath))
            {
                ImagePath = state.LastImagePath;
            }
            
            // Restore prompt
            if (!string.IsNullOrEmpty(state.LastPrompt))
            {
                Prompt = state.LastPrompt;
            }
            
            // Restore game name
            if (!string.IsNullOrEmpty(state.LastGameName))
            {
                GameName = state.LastGameName;
            }
            
            // Restore provider selection
            if (!string.IsNullOrEmpty(state.LastProvider) && Providers.Contains(state.LastProvider))
            {
                SelectedProvider = state.LastProvider;
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
            _logger.LogInformation($"Saving UI state: ImagePath={ImagePath}, Prompt={Prompt}, GameName={GameName}, SelectedProvider={SelectedProvider}");
            var state = new Mentor.Core.Data.UIStateEntity
            {
                Name = "default",
                LastImagePath = ImagePath,
                LastPrompt = Prompt,
                LastGameName = GameName,
                LastProvider = SelectedProvider
            };
            await _configurationRepository.SaveUIStateAsync(state);
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
        // Check if we have either clipboard image data or a file path
        if (_clipboardImageData == null && string.IsNullOrWhiteSpace(ImagePath))
            return;

        if (string.IsNullOrWhiteSpace(SelectedProvider))
        {
            ErrorMessage = "Please select a provider";
            return;
        }

        // Create a new cancellation token source for this analysis
        _analysisCancellationTokenSource?.Cancel();
        _analysisCancellationTokenSource?.Dispose();
        _analysisCancellationTokenSource = new CancellationTokenSource();

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

            // Get image data - prioritize clipboard image over file path
            byte[] imageData;
            if (_clipboardImageData != null)
            {
                imageData = _clipboardImageData;
                _logger.LogInformation("Using clipboard image for analysis, size: {Size} bytes", imageData.Length);
            }
            else if (!string.IsNullOrWhiteSpace(ImagePath))
            {
                try
                {
                    imageData = await File.ReadAllBytesAsync(ImagePath, _analysisCancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error reading image file: {ex.Message}";
                    return;
                }
            }
            else
            {
                ErrorMessage = "No image available for analysis";
                return;
            }

            // Create analysis request
            var request = new AnalysisRequest
            {
                ImageData = imageData,
                Prompt = Prompt,
                GameName = GameName
            };

            // Perform analysis
            Result = await analysisService.AnalyzeAsync(request, _analysisCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Analysis cancelled";
            _logger.LogInformation("Analysis cancelled by user");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Analysis error: {ex.Message}";
            _logger.LogError("Analysis error: {ex}", ex);
        }
        finally
        {
            IsAnalyzing = false;
            _analysisCancellationTokenSource?.Dispose();
            _analysisCancellationTokenSource = null;
        }
    }

    private bool CanAnalyze()
    {
        return (_clipboardImageData != null || !string.IsNullOrWhiteSpace(ImagePath)) && !IsAnalyzing;
    }
    
    /// <summary>
    /// Handles clipboard image detection events.
    /// </summary>
    public async void OnClipboardImageDetected(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            return;
        }

        _clipboardImageData = imageData;
        
        // Update the image source to display the clipboard image
        await UpdateImageSourceFromClipboardAsync(imageData);
        
        // Clear the file path since we're using clipboard image
        // Do this after setting ImageSource to ensure preview displays correctly
        ImagePath = null;
        
        // Notify that analyze command availability might have changed
        AnalyzeCommand.NotifyCanExecuteChanged();
        
        _logger.LogInformation("Clipboard image set as current image, size: {Size} bytes", imageData.Length);
    }
    
    private async Task UpdateImageSourceFromClipboardAsync(byte[] imageData)
    {
        try
        {
            var bitmap = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(imageData);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
                
                // Reset stream position to beginning for SetSource
                stream.Seek(0);
                
                // Set source after writing to ensure stream is ready
                bitmap.SetSource(stream);
            }
            
            // Set ImageSource on UI thread (we should already be on UI thread, but ensure it)
            ImageSource = bitmap;
            ImageSourceCaption = "Loaded from clipboard";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying clipboard image");
            ImageSource = null;
            ImageSourceCaption = null;
        }
    }

    [RelayCommand]
    public void CancelAnalysis()
    {
        _analysisCancellationTokenSource?.Cancel();
    }

    partial void OnImagePathChanged(string? value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        _ = SaveUIStateAsync();
        
        // When a file path is set, clear clipboard image data
        if (!string.IsNullOrEmpty(value))
        {
            _clipboardImageData = null;
        }
        
        // Update the image source
        if (!string.IsNullOrEmpty(value) && File.Exists(value))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.UriSource = new Uri(value, UriKind.Absolute);
                ImageSource = bitmap;
                ImageSourceCaption = "Loaded from disk";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image: {ImagePath}", value);
                ImageSource = null;
                ImageSourceCaption = null;
            }
        }
        else if (string.IsNullOrEmpty(value))
        {
            // Only clear ImageSource if we don't have clipboard image data
            // If we have clipboard data, it should already be displayed via UpdateImageSourceFromClipboardAsync
            if (_clipboardImageData == null)
            {
                ImageSource = null;
                ImageSourceCaption = null;
            }
            // If _clipboardImageData is not null, keep the current ImageSource (it should be set by UpdateImageSourceFromClipboardAsync)
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

    partial void OnGameNameChanged(string value)
    {
        _ = SaveUIStateAsync();
    }

    partial void OnSelectedProviderChanged(string value)
    {
        _ = SaveUIStateAsync();
    }
}

