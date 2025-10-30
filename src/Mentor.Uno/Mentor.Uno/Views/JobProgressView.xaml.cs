using Mentor.Core.Models;
using Microsoft.UI.Xaml.Controls;

namespace Mentor.Uno.Views;

public sealed partial class JobProgressView : UserControl
{
    public AnalysisProgress? AnalysisProgress
    {
        get => (AnalysisProgress?)GetValue(AnalysisProgressProperty);
        set => SetValue(AnalysisProgressProperty, value);
    }

    public static readonly DependencyProperty AnalysisProgressProperty =
        DependencyProperty.Register(
            nameof(AnalysisProgress),
            typeof(AnalysisProgress),
            typeof(JobProgressView),
            new PropertyMetadata(null, OnAnalysisProgressChanged));

    private static void OnAnalysisProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // When AnalysisProgress changes, update the DataContext so Binding can find it
        if (d is JobProgressView view)
        {
            var newProgress = e.NewValue as AnalysisProgress;
            if (newProgress != null)
            {
                System.Diagnostics.Debug.WriteLine($"JobProgressView: AnalysisProgress changed, Jobs count: {newProgress.Jobs.Count}");

                // print per job the name and status, and progress

                foreach (var job in newProgress.Jobs)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Job: {job.Name}, Status: {job.Status}, Progress: {job.Progress}");
                }

            }
            view.DataContext = newProgress;
        }
    }

    public JobProgressView()
    {
        this.InitializeComponent();
    }
}

