using Mentor.Core.Models;
using Microsoft.UI.Xaml.Data;

namespace Mentor.Uno.Converters;

public class JobProgressMultiConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // This converter receives the AnalysisJob object
        // Parameter can be "Value" or "IsIndeterminate"
        if (value is Mentor.Core.Models.AnalysisJob job)
        {
            if (parameter is string param && param == "IsIndeterminate")
            {
                // Show indeterminate if InProgress and Progress is null
                return job.Status == JobStatus.InProgress && job.Progress == null;
            }
            
            // Return progress value
            if (job.Status == JobStatus.Completed)
            {
                return 100.0;
            }
            
            return job.Progress ?? 0.0;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

