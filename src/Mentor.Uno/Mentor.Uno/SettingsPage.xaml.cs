using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Mentor.Uno.Helpers;

namespace Mentor.Uno;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }
    private Window? _ownerWindow;

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<SettingsPageViewModel>();
        DataContext = ViewModel;
    }

    public void SetOwnerWindow(Window window)
    {
        _ownerWindow = window;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        // If opened in a separate window, close it
        if (_ownerWindow != null)
        {
            _ownerWindow.Close();
        }
        // Otherwise, navigate back (for backward compatibility)
        else if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
    }

    private async void OnCopyErrorMessageClick(object sender, RoutedEventArgs e)
    {
        await ClipboardHelper.CopyToClipboardAsync(ViewModel.ErrorMessage, sender as Button);
    }
}

