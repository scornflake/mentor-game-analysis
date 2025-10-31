using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mentor.Core.Data;

namespace Mentor.Uno.ViewModels;

public partial class ToolViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _toolName = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _baseUrl = string.Empty;
    [ObservableProperty] private int _timeout = 30;
    [ObservableProperty] private bool _isApiKeyVisible = false;

    public ToolViewModel(ToolConfigurationEntity config)
    {
        _id = config.Id;
        _toolName = config.ToolName;
        _apiKey = config.ApiKey;
        _baseUrl = config.BaseUrl;
        _timeout = config.Timeout;
    }

    public ToolViewModel()
    {
        _toolName = "New Tool";
        _baseUrl = "https://";
        _timeout = 30;
    }

    public ToolConfigurationEntity ToConfiguration()
    {
        return new ToolConfigurationEntity
        {
            Id = Id,
            ToolName = ToolName,
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            Timeout = Timeout
        };
    }

    [RelayCommand]
    private void ToggleApiKeyVisibility()
    {
        IsApiKeyVisible = !IsApiKeyVisible;
    }
}

