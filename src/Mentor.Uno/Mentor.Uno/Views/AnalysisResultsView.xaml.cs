using Mentor.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Mentor.Uno.Views;

public sealed partial class AnalysisResultsView : UserControl
{
    public Recommendation? Result
    {
        get => (Recommendation?)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    public static readonly DependencyProperty ResultProperty =
        DependencyProperty.Register(
            nameof(Result),
            typeof(Recommendation),
            typeof(AnalysisResultsView),
            new PropertyMetadata(null, OnResultChanged));

    public ICommand? SaveAnalysisCommand
    {
        get => (ICommand?)GetValue(SaveAnalysisCommandProperty);
        set => SetValue(SaveAnalysisCommandProperty, value);
    }

    public static readonly DependencyProperty SaveAnalysisCommandProperty =
        DependencyProperty.Register(
            nameof(SaveAnalysisCommand),
            typeof(ICommand),
            typeof(AnalysisResultsView),
            new PropertyMetadata(null));

    public ObservableCollection<RecommendationItem> SortedRecommendations { get; } = new();

    public AnalysisResultsView()
    {
        this.InitializeComponent();
    }

    private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnalysisResultsView view)
        {
            view.UpdateSortedRecommendations();
        }
    }

    private void UpdateSortedRecommendations()
    {
        SortedRecommendations.Clear();
        
        if (Result?.Recommendations != null)
        {
            // Sort by Priority: High (0), Medium (1), Low (2)
            var sorted = Result.Recommendations.OrderBy(r => r.Priority);
            foreach (var item in sorted)
            {
                SortedRecommendations.Add(item);
            }
        }
    }
}

