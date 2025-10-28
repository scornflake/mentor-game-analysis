using CommunityToolkit.Mvvm.ComponentModel;
using Mentor.Core.Configuration;

namespace Mentor.Uno.ViewModels;

public partial class ProviderViewModel : ObservableObject
{
    private readonly string _originalName;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _providerType = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _model = string.Empty;
    [ObservableProperty] private string _baseUrl = string.Empty;
    [ObservableProperty] private int _timeout = 60;
    [ObservableProperty] private bool _isActive;

    public ProviderViewModel(string name, ProviderConfiguration config)
    {
        _originalName = name;
        _name = name;
        _providerType = config.ProviderType;
        _apiKey = config.ApiKey;
        _model = config.Model;
        _baseUrl = config.BaseUrl;
        _timeout = config.Timeout;
    }

    public ProviderViewModel()
    {
        _originalName = "New Provider";
        _name = "New Provider";
        _providerType = "openai";
        _baseUrl = "https://api.openai.com";
        _timeout = 60;
    }

    public string OriginalName => _originalName;

    public ProviderConfiguration ToConfiguration()
    {
        return new ProviderConfiguration
        {
            ProviderType = ProviderType,
            ApiKey = ApiKey,
            Model = Model,
            BaseUrl = BaseUrl,
            Timeout = Timeout
        };
    }
}

