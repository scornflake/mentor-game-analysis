using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Mentor.Core.Models;

namespace Mentor.Uno.Views;

public sealed partial class AnalysisInputView : UserControl
{
    public string GameName
    {
        get => (string)GetValue(GameNameProperty);
        set => SetValue(GameNameProperty, value);
    }

    public static readonly DependencyProperty GameNameProperty =
        DependencyProperty.Register(
            nameof(GameName),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(string.Empty));

    public string? ImagePath
    {
        get => (string?)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public static readonly DependencyProperty ImagePathProperty =
        DependencyProperty.Register(
            nameof(ImagePath),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null, OnImagePathChanged));

    private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnalysisInputView view)
        {
            // Update ImageFileName when ImagePath changes
            view.ImageFileName = string.IsNullOrEmpty(view.ImagePath) ? null : Path.GetFileName(view.ImagePath);
        }
    }

    public string? ImageFileName
    {
        get => (string?)GetValue(ImageFileNameProperty);
        private set => SetValue(ImageFileNameProperty, value);
    }

    public static readonly DependencyProperty ImageFileNameProperty =
        DependencyProperty.Register(
            nameof(ImageFileName),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public BitmapImage? ImageSource
    {
        get => (BitmapImage?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(
            nameof(ImageSource),
            typeof(BitmapImage),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public string? ImageSourceCaption
    {
        get => (string?)GetValue(ImageSourceCaptionProperty);
        set => SetValue(ImageSourceCaptionProperty, value);
    }

    public static readonly DependencyProperty ImageSourceCaptionProperty =
        DependencyProperty.Register(
            nameof(ImageSourceCaption),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public bool IsValidatingImage
    {
        get => (bool)GetValue(IsValidatingImageProperty);
        set => SetValue(IsValidatingImageProperty, value);
    }

    public static readonly DependencyProperty IsValidatingImageProperty =
        DependencyProperty.Register(
            nameof(IsValidatingImage),
            typeof(bool),
            typeof(AnalysisInputView),
            new PropertyMetadata(false));

    public string? ValidationMessage
    {
        get => (string?)GetValue(ValidationMessageProperty);
        set => SetValue(ValidationMessageProperty, value);
    }

    public static readonly DependencyProperty ValidationMessageProperty =
        DependencyProperty.Register(
            nameof(ValidationMessage),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public string Prompt
    {
        get => (string)GetValue(PromptProperty);
        set => SetValue(PromptProperty, value);
    }

    public static readonly DependencyProperty PromptProperty =
        DependencyProperty.Register(
            nameof(Prompt),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(string.Empty));

    public ObservableCollection<string> Providers
    {
        get => (ObservableCollection<string>)GetValue(ProvidersProperty);
        set => SetValue(ProvidersProperty, value);
    }

    public static readonly DependencyProperty ProvidersProperty =
        DependencyProperty.Register(
            nameof(Providers),
            typeof(ObservableCollection<string>),
            typeof(AnalysisInputView),
            new PropertyMetadata(new ObservableCollection<string>()));

    public string SelectedProvider
    {
        get => (string)GetValue(SelectedProviderProperty);
        set => SetValue(SelectedProviderProperty, value);
    }

    public static readonly DependencyProperty SelectedProviderProperty =
        DependencyProperty.Register(
            nameof(SelectedProvider),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(string.Empty));

    public ICommand AnalyzeCommand
    {
        get => (ICommand)GetValue(AnalyzeCommandProperty);
        set => SetValue(AnalyzeCommandProperty, value);
    }

    public static readonly DependencyProperty AnalyzeCommandProperty =
        DependencyProperty.Register(
            nameof(AnalyzeCommand),
            typeof(ICommand),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public bool IsAnalyzing
    {
        get => (bool)GetValue(IsAnalyzingProperty);
        set => SetValue(IsAnalyzingProperty, value);
    }

    public static readonly DependencyProperty IsAnalyzingProperty =
        DependencyProperty.Register(
            nameof(IsAnalyzing),
            typeof(bool),
            typeof(AnalysisInputView),
            new PropertyMetadata(false));

    public string? AnalyzingMessage
    {
        get => (string?)GetValue(AnalyzingMessageProperty);
        set => SetValue(AnalyzingMessageProperty, value);
    }

    public static readonly DependencyProperty AnalyzingMessageProperty =
        DependencyProperty.Register(
            nameof(AnalyzingMessage),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public ICommand CancelAnalysisCommand
    {
        get => (ICommand)GetValue(CancelAnalysisCommandProperty);
        set => SetValue(CancelAnalysisCommandProperty, value);
    }

    public static readonly DependencyProperty CancelAnalysisCommandProperty =
        DependencyProperty.Register(
            nameof(CancelAnalysisCommand),
            typeof(ICommand),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public AnalysisProgress? AnalysisProgress
    {
        get => (AnalysisProgress?)GetValue(AnalysisProgressProperty);
        set => SetValue(AnalysisProgressProperty, value);
    }

    public static readonly DependencyProperty AnalysisProgressProperty =
        DependencyProperty.Register(
            nameof(AnalysisProgress),
            typeof(AnalysisProgress),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(
            nameof(ErrorMessage),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public string? RejectionMessage
    {
        get => (string?)GetValue(RejectionMessageProperty);
        set => SetValue(RejectionMessageProperty, value);
    }

    public static readonly DependencyProperty RejectionMessageProperty =
        DependencyProperty.Register(
            nameof(RejectionMessage),
            typeof(string),
            typeof(AnalysisInputView),
            new PropertyMetadata(null));

    public event RoutedEventHandler? BrowseClick;
    public event TappedEventHandler? ImagePreviewTapped;

    public AnalysisInputView()
    {
        this.InitializeComponent();
    }

    private void OnBrowseClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        BrowseClick?.Invoke(sender, e);
    }

    private void OnImagePreviewTapped(object sender, TappedRoutedEventArgs e)
    {
        ImagePreviewTapped?.Invoke(sender, e);
    }
}

