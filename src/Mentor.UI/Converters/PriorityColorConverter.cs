using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Mentor.Core.Models;

namespace MentorUI.Converters;

public class PriorityColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Priority priority)
        {
            return priority switch
            {
                Priority.High => new SolidColorBrush(Color.Parse("#DC3545")), // Red
                Priority.Medium => new SolidColorBrush(Color.Parse("#FFC107")), // Amber
                Priority.Low => new SolidColorBrush(Color.Parse("#28A745")), // Green
                _ => new SolidColorBrush(Color.Parse("#6C757D")) // Gray
            };
        }

        return new SolidColorBrush(Color.Parse("#6C757D")); // Default gray
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

