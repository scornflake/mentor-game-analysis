using Windows.Storage.Pickers;
using Mentor.Uno.Helpers;
using Mentor.Uno.Services;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;

namespace Mentor.Uno;

public sealed partial class MainPage : Page
{
    private WindowStateHelper _windowStateHelper;
    private ClipboardMonitor? _clipboardMonitor;
    private Window? _imageOverlayWindow;
    private ImageOverlayPage? _imageOverlayPage;

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<MainPageViewModel>();
        this.DataContext = ViewModel;
        
        _windowStateHelper = new WindowStateHelper(App.GetService<ILogger<WindowStateHelper>>());

        // Add converters to page resources (only converters used directly in MainPage.xaml)
        this.Resources["InverseNullToVisibilityConverter"] = new InverseNullToVisibilityConverter();
        
        // Initialize clipboard monitoring
        InitializeClipboardMonitoring();
    }
    
    private void OnImagePreviewTapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel.ImageSource != null)
        {
            ShowImageOverlayWindow();
            e.Handled = true;
        }
    }
    
    private void ShowImageOverlayWindow()
    {
        // If window already exists, activate it and update the image
        if (_imageOverlayWindow != null)
        {
            _imageOverlayWindow.Activate();
            if (_imageOverlayPage != null)
            {
                _imageOverlayPage.ImageSource = ViewModel.ImageSource;
                // Ensure focus is set when window is reactivated
                _imageOverlayPage.Focus(FocusState.Programmatic);
            }
            return;
        }

        // Create a new window for the image overlay
        _imageOverlayWindow = new Window
        {
            Title = "Image Viewer"
        };
        
        // Create the overlay page and set it as the window content
        _imageOverlayPage = new ImageOverlayPage();
        _imageOverlayPage.SetOwnerWindow(_imageOverlayWindow);
        _imageOverlayPage.ImageSource = ViewModel.ImageSource;
        _imageOverlayWindow.Content = _imageOverlayPage;
        
        // Handle window activated event to set focus
        _imageOverlayWindow.Activated += (sender, args) =>
        {
            if (_imageOverlayPage != null)
            {
                // Focus the page so it can receive keyboard events immediately
                _imageOverlayPage.Focus(FocusState.Programmatic);
            }
        };
        
        // Handle window closed event to clean up reference
        _imageOverlayWindow.Closed += (sender, args) =>
        {
            _imageOverlayWindow = null;
            _imageOverlayPage = null;
        };
        
        // Activate (show) the window
        _imageOverlayWindow.Activate();
        
        // Set initial window size based on image dimensions or use defaults
        var appWindow = _imageOverlayWindow.AppWindow;
        if (appWindow != null && ViewModel.ImageSource != null)
        {
            // Try to get image dimensions, but use reasonable defaults if not available
            int width = 1200;
            int height = 900;
            
            try
            {
                // Wait for image to load if needed
                if (ViewModel.ImageSource.PixelWidth > 0 && ViewModel.ImageSource.PixelHeight > 0)
                {
                    // Use image dimensions plus some padding for the border and UI
                    width = Math.Min(ViewModel.ImageSource.PixelWidth + 100, 1920);
                    height = Math.Min(ViewModel.ImageSource.PixelHeight + 150, 1080);
                }
            }
            catch
            {
                // Use defaults if we can't get dimensions
            }
            
            appWindow.Resize(new Windows.Graphics.SizeInt32
            {
                Width = width,
                Height = height
            });
        }
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

    private void OnCopyErrorMessageClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(ViewModel.ErrorMessage);
            Clipboard.SetContent(dataPackage);
        }
    }

    private void OnDismissErrorClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ErrorMessage = null;
    }

    private void OnCopyRejectionMessageClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.RejectionMessage))
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(ViewModel.RejectionMessage);
            Clipboard.SetContent(dataPackage);
        }
    }

    private void OnDismissRejectionClick(object sender, RoutedEventArgs e)
    {
        ViewModel.RejectionMessage = null;
    }
}

