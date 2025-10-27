using Mentor.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Mentor.Uno;

public class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Priority priority)
        {
            return priority switch
            {
                Priority.High => new SolidColorBrush(Colors.Red),
                Priority.Medium => new SolidColorBrush(Colors.Orange),
                Priority.Low => new SolidColorBrush(Colors.Green),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

