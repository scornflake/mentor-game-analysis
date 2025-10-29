using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace Mentor.Uno;

public sealed partial class ImageOverlayPage : Page
{
    private Window? _ownerWindow;
    private BitmapImage? _imageSource;

    public BitmapImage? ImageSource
    {
        get => _imageSource;
        set
        {
            _imageSource = value;
            if (OverlayImage != null)
            {
                OverlayImage.Source = value;
            }
        }
    }

    public ImageOverlayPage()
    {
        this.InitializeComponent();
        
        // Ensure image is set when page loads
        this.Loaded += (s, e) =>
        {
            if (OverlayImage != null && _imageSource != null)
            {
                OverlayImage.Source = _imageSource;
            }
            // Focus the page so it can receive keyboard events
            this.Focus(FocusState.Programmatic);
        };
    }

    public void SetOwnerWindow(Window window)
    {
        _ownerWindow = window;
    }

    private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CloseWindow();
            e.Handled = true;
        }
    }
    
    private void OnGridKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CloseWindow();
            e.Handled = true;
        }
    }
    
    private void OnGridGotFocus(object sender, RoutedEventArgs e)
    {
        // Ensure we stay focused so keyboard events work
    }
    
    private void OnScrollViewerKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CloseWindow();
            e.Handled = true;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        if (_ownerWindow != null)
        {
            _ownerWindow.Close();
        }
    }
}

