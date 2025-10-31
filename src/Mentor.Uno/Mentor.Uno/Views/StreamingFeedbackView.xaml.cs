using System.Collections.Specialized;
using System.ComponentModel;
using Mentor.Uno.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Mentor.Uno.Views;

public sealed partial class StreamingFeedbackView : UserControl
{
    public StreamingFeedbackViewModel? ViewModel
    {
        get => (StreamingFeedbackViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(StreamingFeedbackViewModel),
            typeof(StreamingFeedbackView),
            new PropertyMetadata(null, OnViewModelChanged));

    public StreamingFeedbackView()
    {
        this.InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StreamingFeedbackView view)
        {
            // Unsubscribe from old ViewModel
            if (e.OldValue is StreamingFeedbackViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= view.OnViewModelPropertyChanged;
                oldViewModel.Events.CollectionChanged -= view.OnEventsCollectionChanged;
            }

            // Subscribe to new ViewModel
            if (e.NewValue is StreamingFeedbackViewModel newViewModel)
            {
                newViewModel.PropertyChanged += view.OnViewModelPropertyChanged;
                newViewModel.Events.CollectionChanged += view.OnEventsCollectionChanged;
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StreamingFeedbackViewModel.AccumulatedText))
        {
            // Auto-scroll to bottom when text is added
            DispatcherQueue.TryEnqueue(() =>
            {
                ResponseScrollViewer?.ChangeView(null, ResponseScrollViewer.ScrollableHeight, null, disableAnimation: true);
            });
        }
    }

    private void OnEventsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Auto-scroll to bottom when new events are added
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                EventsScrollViewer?.ChangeView(null, EventsScrollViewer.ScrollableHeight, null, disableAnimation: true);
            });
        }
    }
}

