using CommunityToolkit.Mvvm.ComponentModel;
using Mentor.Core.Data;

namespace Mentor.Uno.ViewModels;

public partial class ToolViewModel : ObservableObject
{
    private readonly string _originalToolName;

    [ObservableProperty] private string _toolName = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _baseUrl = string.Empty;
    [ObservableProperty] private int _timeout = 30;

    public ToolViewModel(RealWebtoolToolConfiguration config)
    {
        _originalToolName = config.ToolName;
        _toolName = config.ToolName;
        _apiKey = config.ApiKey;
        _baseUrl = config.BaseUrl;
        _timeout = config.Timeout;
    }

    public ToolViewModel()
    {
        _originalToolName = "New Tool";
        _toolName = "New Tool";
        _baseUrl = "https://";
        _timeout = 30;
    }

    public string OriginalToolName => _originalToolName;

    public RealWebtoolToolConfiguration ToConfiguration()
    {
        return new RealWebtoolToolConfiguration
        {
            ToolName = ToolName,
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            Timeout = Timeout
        };
    }
}

