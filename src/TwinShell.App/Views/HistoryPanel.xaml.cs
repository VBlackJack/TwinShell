using System.Windows.Controls;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

public partial class HistoryPanel : UserControl
{
    public HistoryPanel(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Initialize the view model
        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }
}
