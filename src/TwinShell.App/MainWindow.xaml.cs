using System.Windows;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, HistoryPanel historyPanel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Set the history panel (will be accessed by name in XAML)
        HistoryTabContent.Content = historyPanel;

        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }
}
