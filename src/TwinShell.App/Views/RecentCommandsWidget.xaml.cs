using System.Windows.Controls;
using TwinShell.App.ViewModels;

namespace TwinShell.App.Views;

public partial class RecentCommandsWidget : UserControl
{
    public RecentCommandsWidget(RecentCommandsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Initialize the view model
        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }
}
