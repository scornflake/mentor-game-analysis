using Windows.Storage.Pickers;
using Mentor.Uno.Helpers;
using Mentor.Uno.Services;

namespace Mentor.Uno;

public sealed partial class MainPage : Page
{
    private WindowStateHelper _windowStateHelper;
    private ClipboardMonitor? _clipboardMonitor;

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<MainPageViewModel>();
        this.DataContext = ViewModel;
        
        _windowStateHelper = new WindowStateHelper(App.GetService<ILogger<WindowStateHelper>>());

        // Add converters to page resources
        this.Resources["NullToVisibilityConverter"] = new NullToVisibilityConverter();
        this.Resources["InverseNullToVisibilityConverter"] = new InverseNullToVisibilityConverter();
        this.Resources["StringToBoolConverter"] = new StringToBoolConverter();
        this.Resources["StringToVisibilityConverter"] = new StringToVisibilityConverter();
        this.Resources["PriorityToBrushConverter"] = new PriorityToBrushConverter();
        
        // Initialize clipboard monitoring
        InitializeClipboardMonitoring();
    }
    
    private void InitializeClipboardMonitoring()
    {
        try
        {
            _clipboardMonitor = App.GetService<ClipboardMonitor>();
            _clipboardMonitor.ImageDetected += OnClipboardImageDetected;
            _clipboardMonitor.StartMonitoring();
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app if clipboard monitoring fails
            var logger = App.GetService<ILogger<MainPage>>();
            logger?.LogError(ex, "Failed to initialize clipboard monitoring");
        }
    }
    
    private void OnClipboardImageDetected(object? sender, ClipboardImageEventArgs e)
    {
        // Update the ViewModel on the UI thread
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.OnClipboardImageDetected(e.ImageData);
        });
    }

    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        // Create a new window for settings
        var settingsWindow = new Window
        {
            Title = "Settings"
        };
        
        // Create the settings page and set it as the window content
        var settingsPage = new SettingsPage();
        settingsPage.SetOwnerWindow(settingsWindow);
        settingsWindow.Content = settingsPage;
        
        // Activate (show) the settings window
        settingsWindow.Activate();
        
        // Restore window state and setup tracking
        var repository = App.GetService<Mentor.Core.Interfaces.IConfigurationRepository>();
        await _windowStateHelper.RestoreWindowStateAsync(settingsWindow, "SettingsWindow", repository, defaultWidth: 800, defaultHeight: 1000);
        _windowStateHelper.SetupWindowStateTracking(settingsWindow, "SettingsWindow", repository);
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            
            // Initialize the picker with the window handle for WinUI
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            // Set file type filters
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ViewModel.ImagePath = file.Path;
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Error selecting file: {ex.Message}";
        }
    }
}

