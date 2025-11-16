using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainViewModel viewModel, HistoryPanel historyPanel, RecentCommandsWidget recentCommandsWidget, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;

        // Set the history panel (will be accessed by name in XAML)
        HistoryTabContent.Content = historyPanel;

        // Set the recent commands widget
        RecentCommandsContainer.Child = recentCommandsWidget;

        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Create and show the Settings window
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
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
            "Sprint 3: UI/UX & Customization\n\n" +
            "Features:\n" +
            "• Dark Mode Support\n" +
            "• Command History Tracking\n" +
            "• Favorites System\n" +
            "• Export/Import Configuration\n" +
            "• Customizable Settings\n" +
            "• Search and Filter\n\n" +
            "© 2025 TwinShell Project",
            "About TwinShell",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
