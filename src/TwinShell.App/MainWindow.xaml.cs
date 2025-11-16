using System.Windows;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, HistoryPanel historyPanel, RecentCommandsWidget recentCommandsWidget)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Set the history panel (will be accessed by name in XAML)
        HistoryTabContent.Content = historyPanel;

        // Set the recent commands widget
        RecentCommandsContainer.Child = recentCommandsWidget;

        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "TwinShell v1.0\n\n" +
            "PowerShell & Bash Command Manager\n" +
            "For System Administrators\n\n" +
            "Sprint 2: User Personalization & History\n\n" +
            "Features:\n" +
            "• Command History Tracking\n" +
            "• Favorites System\n" +
            "• Export/Import Configuration\n" +
            "• Search and Filter\n\n" +
            "© 2025 TwinShell Project",
            "About TwinShell",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
