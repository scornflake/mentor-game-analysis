using CommunityToolkit.Mvvm.ComponentModel;
using Mentor.Core.Configuration;

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

    public ProviderViewModel(ProviderConfiguration config)
    {
        _id = config.Id;
        _name = config.Name;
        _providerType = config.ProviderType;
        _apiKey = config.ApiKey;
        _model = config.Model;
        _baseUrl = config.BaseUrl;
        _timeout = config.Timeout;
    }

    public ProviderViewModel()
    {
        _id = "";
        _name = "New Provider";
        _providerType = "openai";
        _baseUrl = "https://api.openai.com";
        _timeout = 60;
    }

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

