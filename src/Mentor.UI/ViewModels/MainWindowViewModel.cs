using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mentor.Core.Interfaces;
using Mentor.Core.Models;

namespace MentorUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
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

    public MainWindowViewModel(IAnalysisService analysisService, ILLMProviderFactory providerFactory)
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

            // Result = MakeUpFakeData();
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

    private Recommendation MakeUpFakeData()
    {
        var recommendations = new List<RecommendationItem>();
        recommendations.Add(new RecommendationItem
            {
                Priority = Priority.High,
                Action = "Increase your character's elemental damage by equipping weapons and artifacts that boost specific elemental types.",
                Reasoning = "Elemental damage can exploit enemy weaknesses and significantly increase overall damage output.",
                Context = "Your current build focuses heavily on physical damage, missing out on potential elemental advantages",
            }
        );

        var recommendation = new Recommendation
        {
            Analysis = "The screenshot shows a character build focused on high critical damage and status effects. " +
                       "However, the build lacks survivability and elemental damage types that could enhance overall performance.",
            Summary = "Optimize your build by balancing offense with defense and incorporating elemental damage.",
            Confidence = 0.85,
            GeneratedAt = DateTime.UtcNow,
            ProviderUsed = SelectedProvider,
            Recommendations = recommendations
        };
        return recommendation;
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

    [RelayCommand]
    private async Task SelectImageAsync()
    {
        // This will be called from the View which will handle the file picker
        // For now, just a placeholder
        await Task.CompletedTask;
    }
}