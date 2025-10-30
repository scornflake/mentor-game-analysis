using Mentor.Core.Models;
using Microsoft.UI.Xaml.Controls;

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
            new PropertyMetadata(null));

    public AnalysisResultsView()
    {
        this.InitializeComponent();
    }
}

