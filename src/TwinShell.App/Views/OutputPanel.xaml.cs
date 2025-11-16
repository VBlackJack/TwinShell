using System.Windows.Controls;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

/// <summary>
/// Interaction logic for OutputPanel.xaml
/// </summary>
public partial class OutputPanel : UserControl
{
    private readonly ExecutionViewModel _viewModel;
    private readonly System.Collections.Specialized.NotifyCollectionChangedEventHandler _collectionChangedHandler;

    public OutputPanel(ExecutionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // BUGFIX: Store handler reference for later detachment
        _collectionChangedHandler = (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                OutputScrollViewer.ScrollToEnd();
            });
        };

        // Auto-scroll to bottom when new output is added
        _viewModel.OutputLines.CollectionChanged += _collectionChangedHandler;

        // BUGFIX: Detach event handler when control is unloaded
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Detach event handlers to prevent memory leaks
        _viewModel.OutputLines.CollectionChanged -= _collectionChangedHandler;
        Unloaded -= OnUnloaded;
    }
}
