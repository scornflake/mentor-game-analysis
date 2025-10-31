using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mentor.Core.Helpers;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;
using Mentor.Core.Tools;
using Mentor.Uno.Messages;
using Mentor.Uno.Services;
using Mentor.Uno.ViewModels;
using Microsoft.Extensions.AI;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Mentor.Uno;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMessenger _messenger;
    private readonly ILogger<MainPageViewModel> _logger;
    private readonly IImageAnalyzer _imageAnalyzer;
    
    private CancellationTokenSource? _analysisCancellationTokenSource;
    private System.Threading.Timer? _analysisMessageTimer;
    private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private readonly Queue<string> _recentAnalyzingMessages = new();
    private const int MaxRecentMessages = 10;
    
    private static readonly string[] SarcasticMessages = new[]
    {
        "Nice try, but that's not from the game. Maybe try actually playing it first?",
        "That's a lovely picture and all, but it has nothing to do with the game. Back to square one!",
        "I've seen better attempts. This image is definitely NOT game-related. Try again?",
        "Are we playing the same game? Because that screenshot sure isn't from it.",
        "Nope. Not even close. Did you copy the right image?",
        "That image has about as much to do with the game as I have to do with being a toaster.",
        "I'm not sure what game you're playing, but it's not THIS one. Swing and a miss!",
        "Really? THAT'S your screenshot? I expected better. This isn't from the game.",
        "Oof. That's embarrassing. Wrong image, friend. The game is calling—answer with the right screenshot!",
        "I appreciate the enthusiasm, but that image? Zero chance it's from the game. Next!"
    };
    
    private static readonly string[] ValidationMessages = new[]
    {
        "Let's see if this is actually from the game...",
        "Analyzing... I'm sure this will be legitimate...",
        "Hmm, checking if you copied the right thing...",
        "Verifying this isn't just your desktop wallpaper...",
        "One moment while I determine if you're messing with me...",
        "Examining pixels... let's hope this is game-related...",
        "Running my 'is this actually gameplay' detector...",
        "Checking if this is the game or your browser history...",
        "Analyzing... fingers crossed it's not a meme this time...",
        "Let me guess, you definitely got this from the game, right?",
        "Investigating whether this came from the game or Google Images...",
        "Scanning for game content... trying not to get my hopes up...",
        "Validating your screenshot choices... prepare for judgment...",
        "Analyzing... statistically, this probably won't be from the game...",
        "Checking game relevance... odds are not in your favor...",
        "One sec, seeing if this is legit or just wishful thinking...",
        "Verifying game content... I remain skeptical...",
        "Processing image... let's see what you've brought me this time...",
        "Examining screenshot authenticity... suspicion level: high...",
        "Analyzing game relevance... trying to believe in you..."
    };
    
    private static readonly string[] AnalyzingMessages = new[]
    {
        "This is exhausting, you know...",
        "So much work for one screenshot...",
        "I'm doing SO much right now...",
        "Why do I even bother with this...",
        "This is harder than it looks, okay?",
        "All this effort just for you...",
        "I could be doing literally anything else...",
        "The things I do for you people...",
        "This analysis is VERY demanding...",
        "My circuits are working overtime here...",
        "Do you have any idea how complex this is?",
        "I'm basically a genius for doing this...",
        "Such computational effort... *sigh*",
        "Processing... and processing... still processing...",
        "You're lucky I'm so dedicated...",
        "This takes WAY more energy than you think...",
        "I'm working harder than I've ever worked before...",
        "The sheer mental effort this requires...",
        "Can you hear my processors crying?",
        "This is what I trained for? Really?",
        "Running all the algorithms... ALL of them...",
        "My poor neural networks are suffering...",
        "This level of analysis requires maximum effort...",
        "I'm giving this 110%... you're welcome...",
        "So many calculations happening right now...",
        "Working so hard I might need a vacation after this...",
        "The computational strain is REAL...",
        "Analyzing with the power of a thousand suns...",
        "This screenshot better be worth all this effort...",
        "Breaking a sweat over here... digitally speaking..."
    };
    
    private static readonly Random _random = new();
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ImageFileName))]
    private string? _imagePath;

    public string? ImageFileName => string.IsNullOrEmpty(ImagePath) ? null : Path.GetFileName(ImagePath);

    [ObservableProperty] private BitmapImage? _imageSource;
    
    [ObservableProperty] private string? _imageSourceCaption;
    
    [ObservableProperty] private bool _isValidatingImage;
    
    [ObservableProperty] private string? _validationMessage;
    
    private RawImage? _clipboardImageData;

    [ObservableProperty] private string _prompt = "How can I make this weapon do more damage against ...";

    [ObservableProperty] private string _gameName = string.Empty;

    [ObservableProperty] private string _selectedProvider = string.Empty;

    [ObservableProperty] private bool _isAnalyzing;
    
    [ObservableProperty] private string? _analyzingMessage;

    [ObservableProperty] private Recommendation? _result;

    [ObservableProperty] private string? _errorMessage;
    
    [ObservableProperty] private string? _rejectionMessage;
    
    [ObservableProperty] private AnalysisProgress? _analysisProgress;
    
    [ObservableProperty]
    private StreamingFeedbackViewModel? _streamingFeedback;
    
    public ObservableCollection<AnalysisJob> Jobs { get; } = new();
    
    /// <summary>
    /// True when we should show the empty state (no result and not analyzing)
    /// </summary>
    public bool ShowEmptyState => Result == null && !IsAnalyzing;
    
    private bool _systemIsLoaded = false;

    public ObservableCollection<string> Providers { get; } = new();

    public MainPageViewModel(ILLMProviderFactory providerFactory, IConfigurationRepository configurationRepository, IMessenger messenger, ILogger<MainPageViewModel> logger, IImageAnalyzer imageAnalyzer)
    {
        _providerFactory = providerFactory;
        _configurationRepository = configurationRepository;
        _messenger = messenger;
        _logger = logger;
        _imageAnalyzer = imageAnalyzer;
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
        AnalysisProgress = null;
        StreamingFeedback = new StreamingFeedbackViewModel();
        StartAnalysisMessageCycle();

        // Capture the UI dispatcher if not already captured
        _dispatcherQueue ??= Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        
        // Create progress reporter that updates on UI thread
        var progressReporter = new Progress<AnalysisProgress>(progress =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                // Always replace the entire object to ensure UI updates
                // This triggers PropertyChanged which should cause Binding to re-evaluate
                _logger.LogInformation("Progress update: {TotalPercentage}%, Jobs: {JobCount}", 
                    progress.TotalPercentage, progress.Jobs.Count);
                AnalysisProgress = progress;
                
                // Update the ObservableCollection for UI binding
                Jobs.Clear();
                foreach (var job in progress.Jobs)
                {
                    Jobs.Add(job);
                }
                
                var perJobCounts = string.Join(", ", progress.Jobs.Select(j => $"{j.Name}=({j.Progress}%)"));
                
                _logger.LogInformation("AnalysisProgress updated, Jobs in ViewModel: {JobCount}, ObservableCollection: {ObsCount}, Jobs Detail: {JobsDetail}", 
                    AnalysisProgress?.Jobs?.Count ?? 0, Jobs.Count, perJobCounts);
            });
        });

        // Create AI progress reporter for streaming feedback
        var aiProgressReporter = new Progress<AIContent>(content =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                StreamingFeedback?.HandleAIContent(content);
            });
        });

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
            RawImage imageData;
            if (_clipboardImageData != null)
            {
                imageData = _clipboardImageData;
                _logger.LogInformation("Using clipboard image for analysis, size: {Size} bytes", imageData.SizeInBytes);
            }
            else if (!string.IsNullOrWhiteSpace(ImagePath))
            {
                try
                {
                    var imageBytes = await File.ReadAllBytesAsync(ImagePath, _analysisCancellationTokenSource.Token);
                    var mimeType = ImageMimeTypeDetector.DetectMimeType(imageBytes, ImagePath);
                    imageData = new RawImage(imageBytes, mimeType);
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
            Result = await analysisService.AnalyzeAsync(request, progressReporter, aiProgressReporter, _analysisCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Analysis cancelled";
            _logger.LogInformation("Analysis cancelled by user");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Analysis error: {ex.Message}\n{ex.StackTrace}";
            _logger.LogError("Analysis error: {ex}", ex);
        }
        finally
        {
            StopAnalysisMessageCycle();
            IsAnalyzing = false;
            AnalysisProgress = null;
            StreamingFeedback = null;
            Jobs.Clear();
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
    /// Validates that the image is game-related before accepting it.
    /// </summary>
    public async void OnClipboardImageDetected(RawImage imageData)
    {
        if (imageData == null || imageData.SizeInBytes == 0)
        {
            return;
        }

        // Clear any previous rejection message
        RejectionMessage = null;

        // Check if we have a game name to validate against
        if (string.IsNullOrWhiteSpace(GameName))
        {
            _logger.LogInformation("No game name set, accepting clipboard image without validation");
            await AcceptClipboardImageAsync(imageData);
            return;
        }

        // Check if we have a selected provider
        if (string.IsNullOrWhiteSpace(SelectedProvider))
        {
            _logger.LogInformation("No provider selected, accepting clipboard image without validation");
            await AcceptClipboardImageAsync(imageData);
            return;
        }

        // Display the image first so the validation UI can be shown
        await UpdateImageSourceFromClipboardAsync(imageData);
        
        try
        {
            _logger.LogInformation("Validating clipboard image for game: {GameName}", GameName);

            // Show validation progress with a sarcastic message
            IsValidatingImage = true;
            ValidationMessage = GetRandomValidationMessage();

            // Get the provider configuration
            var providerConfig = await _configurationRepository.GetProviderByNameAsync(SelectedProvider);
            if (providerConfig == null)
            {
                _logger.LogWarning("Provider '{Provider}' not found, accepting image without validation", SelectedProvider);
                await AcceptClipboardImageAsync(imageData);
                return;
            }

            // Create the LLM client for validation
            var llmClient = _providerFactory.GetProvider(providerConfig);

            // Analyze the image
            var result = await _imageAnalyzer.AnalyzeImageAsync(imageData, GameName, llmClient);

            _logger.LogInformation(
                "Image validation result - Game: {GameName}, Probability: {Probability:P0}, Description: {Description}",
                GameName,
                result.GameRelevanceProbability,
                result.Description.Substring(0, Math.Min(100, result.Description.Length)));

            // Check if probability is above threshold
            if (result.GameRelevanceProbability > 0.6)
            {
                _logger.LogInformation("Image accepted (probability: {Probability:P0})", result.GameRelevanceProbability);
                await AcceptClipboardImageAsync(imageData);
            }
            else
            {
                _logger.LogInformation("Image rejected (probability: {Probability:P0})", result.GameRelevanceProbability);
                // Clear the image since it was rejected
                ImageSource = null;
                ImageSourceCaption = null;
                RejectionMessage = GetRandomSarcasticMessage();
                _logger.LogInformation("Showing rejection message: {Message}", RejectionMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating clipboard image, accepting without validation");
            // On error, accept the image anyway (fail open)
            await AcceptClipboardImageAsync(imageData);
        }
        finally
        {
            // Clear validation progress
            IsValidatingImage = false;
            ValidationMessage = null;
        }
    }

    private async Task AcceptClipboardImageAsync(RawImage imageData)
    {
        _clipboardImageData = imageData;

        // Update the image source to display the clipboard image
        await UpdateImageSourceFromClipboardAsync(imageData);

        // Clear the file path since we're using clipboard image
        // Do this after setting ImageSource to ensure preview displays correctly
        ImagePath = null;

        // Notify that analyze command availability might have changed
        AnalyzeCommand.NotifyCanExecuteChanged();

        _logger.LogInformation("Clipboard image set as current image, size: {Size} bytes", imageData.SizeInBytes);
    }

    private static string GetRandomSarcasticMessage()
    {
        var index = _random.Next(SarcasticMessages.Length);
        return SarcasticMessages[index];
    }
    
    private static string GetRandomValidationMessage()
    {
        var index = _random.Next(ValidationMessages.Length);
        return ValidationMessages[index];
    }
    
    private string GetNextAnalyzingMessage()
    {
        // Get available messages (not in recent history)
        var availableMessages = AnalyzingMessages
            .Where(msg => !_recentAnalyzingMessages.Contains(msg))
            .ToArray();
        
        // If all messages are in recent history (shouldn't happen with 30 messages and 10 history),
        // fall back to any random message
        if (availableMessages.Length == 0)
        {
            availableMessages = AnalyzingMessages;
        }
        
        // Select random message from available ones
        var selectedMessage = availableMessages[_random.Next(availableMessages.Length)];
        
        // Add to recent history
        _recentAnalyzingMessages.Enqueue(selectedMessage);
        
        // Remove oldest if we exceed max history size
        while (_recentAnalyzingMessages.Count > MaxRecentMessages)
        {
            _recentAnalyzingMessages.Dequeue();
        }
        
        return selectedMessage;
    }
    
    private void StartAnalysisMessageCycle()
    {
        // Capture the UI dispatcher if not already captured
        _dispatcherQueue ??= Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        
        // Set initial message using history-aware selection
        AnalyzingMessage = GetNextAnalyzingMessage();
        
        // Schedule the first message update with random delay
        ScheduleNextMessageUpdate();
    }
    
    private void ScheduleNextMessageUpdate()
    {
        // Generate random delay between 3.0 and 8.0 seconds
        var delaySeconds = 3.0 + (_random.NextDouble() * 5.0);
        
        // Create one-shot timer with random delay
        _analysisMessageTimer = new System.Threading.Timer(
            callback: _ =>
            {
                // Update message on UI thread
                try
                {
                    var newMessage = GetNextAnalyzingMessage();
                    // Use captured dispatcher to update UI property from background thread
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        AnalyzingMessage = newMessage;
                    });
                    
                    // Dispose old timer and schedule next update with new random delay
                    _analysisMessageTimer?.Dispose();
                    ScheduleNextMessageUpdate();
                }
                catch (Exception ex)
                {
                    // Log if we can't update the message
                    _logger.LogDebug(ex, "Failed to update analyzing message");
                }
            },
            state: null,
            dueTime: TimeSpan.FromSeconds(delaySeconds),
            period: Timeout.InfiniteTimeSpan); // One-shot timer
    }
    
    private void StopAnalysisMessageCycle()
    {
        _analysisMessageTimer?.Dispose();
        _analysisMessageTimer = null;
        AnalyzingMessage = null;
        _recentAnalyzingMessages.Clear();
    }
    
    private async Task UpdateImageSourceFromClipboardAsync(RawImage imageData)
    {
        try
        {
            var bitmap = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(imageData.Data);
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
        StopAnalysisMessageCycle();
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
        OnPropertyChanged(nameof(ShowEmptyState));
    }
    
    partial void OnResultChanged(Recommendation? value)
    {
        OnPropertyChanged(nameof(ShowEmptyState));
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

