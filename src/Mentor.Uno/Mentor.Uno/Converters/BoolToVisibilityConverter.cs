using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Mentor.Uno.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Visibility visibility = Visibility.Collapsed;
        if (value is bool boolValue)
        {
            bool invert = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;
            visibility = (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        // Console.WriteLine("Convert: " + value + " to " + visibility);
        return visibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

