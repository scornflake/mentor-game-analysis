using Mentor.Core.Models;
using Microsoft.UI.Xaml.Data;

namespace Mentor.Uno.Converters;

public class JobStatusToIndeterminateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // This converter receives the Status, but we need to check Progress too
        // For now, show indeterminate for any InProgress status
        // The ProgressBar will show actual progress if Progress value is set
        if (value is JobStatus status)
        {
            return status == JobStatus.InProgress;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

