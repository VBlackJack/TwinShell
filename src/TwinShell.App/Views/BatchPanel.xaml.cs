using System.Windows.Controls;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

/// <summary>
/// Interaction logic for BatchPanel.xaml
/// </summary>
public partial class BatchPanel : UserControl
{
    public BatchPanel(BatchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
