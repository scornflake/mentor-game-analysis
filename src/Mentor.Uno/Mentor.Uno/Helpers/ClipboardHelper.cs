using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Mentor.Uno.Helpers;

/// <summary>
/// Helper class for clipboard operations.
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// Copies text to the clipboard and provides visual feedback via a button.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard.</param>
    /// <param name="feedbackButton">Optional button to show "Copied!" feedback. If null, no feedback is shown.</param>
    public static async Task CopyToClipboardAsync(string? text, Button? feedbackButton = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);

        // Show a brief notification that it was copied
        if (feedbackButton != null)
        {
            var originalContent = feedbackButton.Content;
            feedbackButton.Content = "Copied!";
            await Task.Delay(1000);
            feedbackButton.Content = originalContent;
        }
    }
}

