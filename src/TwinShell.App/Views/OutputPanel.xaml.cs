using System.Windows.Controls;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

/// <summary>
/// Interaction logic for OutputPanel.xaml
/// </summary>
public partial class OutputPanel : UserControl
{
    public OutputPanel(ExecutionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-scroll to bottom when new output is added
        viewModel.OutputLines.CollectionChanged += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                OutputScrollViewer.ScrollToEnd();
            });
        };
    }
}
