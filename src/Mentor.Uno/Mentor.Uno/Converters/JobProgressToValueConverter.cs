using Microsoft.UI.Xaml.Data;

namespace Mentor.Uno.Converters;

public class JobProgressToValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double progress)
        {
            return progress;
        }
        
        var nullableProgress = value as double?;
        if (nullableProgress.HasValue)
        {
            return nullableProgress.Value;
        }
        
        // Return 0 for null progress - ProgressBar will show indeterminate if IsIndeterminate is true
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

