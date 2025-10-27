using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;

namespace Mentor.Uno;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IAnalysisService _analysisService;
    private readonly ILLMProviderFactory _providerFactory;

    [ObservableProperty] private string? _imagePath;

    [ObservableProperty] private string _prompt = "How can I make this weapon do more damage against ...";

    [ObservableProperty] private string _selectedProvider = "perplexity";

    [ObservableProperty] private bool _isAnalyzing;

    [ObservableProperty] private Recommendation? _result;

    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<string> Providers { get; } = new()
    {
        "openai",
        "perplexity",
        "local"
    };

    public MainPageViewModel(IAnalysisService analysisService, ILLMProviderFactory providerFactory)
    {
        _analysisService = analysisService;
        _providerFactory = providerFactory;
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    public async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(ImagePath))
            return;

        IsAnalyzing = true;
        ErrorMessage = null;
        Result = null;

        try
        {
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
            Result = await _analysisService.AnalyzeAsync(request);
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

