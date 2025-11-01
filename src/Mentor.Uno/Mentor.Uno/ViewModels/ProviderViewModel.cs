using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mentor.Core.Data;

namespace Mentor.Uno.ViewModels;

public partial class ProviderViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _providerType = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _model = string.Empty;
    [ObservableProperty] private string _baseUrl = string.Empty;
    [ObservableProperty] private int _timeout = 60;
    [ObservableProperty] private bool _retrievalAugmentedGeneration = false;
    [ObservableProperty] private bool _serverHasMcpSearch = false;
    [ObservableProperty] private bool _isApiKeyVisible = false;

    public ProviderViewModel(ProviderConfigurationEntity config)
    {
        _id = config.Id;
        _name = config.Name;
        _providerType = config.ProviderType;
        _apiKey = config.ApiKey;
        _model = config.Model;
        _baseUrl = config.BaseUrl;
        _timeout = config.Timeout;
        _retrievalAugmentedGeneration = config.RetrievalAugmentedGeneration;
    }

    public ProviderViewModel()
    {
        _id = "";
        _name = "New Provider";
        _providerType = "openai";
        _baseUrl = "https://api.openai.com";
        _timeout = 60;
        _retrievalAugmentedGeneration = false;
        _serverHasMcpSearch = false;
    }

    public ProviderConfigurationEntity ToConfiguration()
    {
        return new ProviderConfigurationEntity
        {
            Id = Id,
            Name = Name,
            ProviderType = ProviderType,
            ApiKey = ApiKey,
            Model = Model,
            BaseUrl = BaseUrl,
            Timeout = Timeout,
            RetrievalAugmentedGeneration = RetrievalAugmentedGeneration,
        };
    }

    public bool IsOpenAIProvider => ProviderType?.Equals("openai", StringComparison.OrdinalIgnoreCase) ?? false;
    
    public bool IsRetrievalAugmentedGenerationEnabled => !ServerHasMcpSearch;

    partial void OnProviderTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsOpenAIProvider));
    }

    partial void OnServerHasMcpSearchChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRetrievalAugmentedGenerationEnabled));
    }

    [RelayCommand]
    private void ToggleApiKeyVisibility()
    {
        IsApiKeyVisible = !IsApiKeyVisible;
    }
}

