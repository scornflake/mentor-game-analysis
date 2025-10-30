using Mentor.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Mentor.Uno.Converters;

public class JobStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is JobStatus status)
        {
            // Show progress bar for InProgress or Completed jobs, hide for Pending and Failed
            return status == JobStatus.InProgress || status == JobStatus.Completed 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

