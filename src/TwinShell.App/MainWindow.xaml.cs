using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;

    public MainWindow(MainViewModel viewModel, HistoryPanel historyPanel, RecentCommandsWidget recentCommandsWidget, OutputPanel outputPanel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _mainViewModel = viewModel;
        _serviceProvider = serviceProvider;

        // Set the history panel (will be accessed by name in XAML)
        HistoryTabContent.Content = historyPanel;

        // Set the recent commands widget
        RecentCommandsContainer.Child = recentCommandsWidget;

        // Set the execution output panel (Sprint 4)
        ExecutionTabContent.Content = outputPanel;

        // Wire up the ExecutionViewModel to MainViewModel
        if (outputPanel.DataContext is ExecutionViewModel executionViewModel)
        {
            _mainViewModel.ExecutionViewModel = executionViewModel;
        }

        // BUGFIX: Extract async initialization to proper async method to prevent unhandled exceptions
        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// BUGFIX: Proper async event handler to prevent unhandled exceptions from crashing the app
    /// Wraps InitializeAsync in try-catch for error handling
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _mainViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize the application: {ex.Message}",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Create and show the Settings window
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void ManageCategories_Click(object sender, RoutedEventArgs e)
    {
        // Create and show the Category Management window
        var categoryWindow = _serviceProvider.GetRequiredService<CategoryManagementWindow>();
        categoryWindow.Owner = this;
        categoryWindow.ShowDialog();
    }

    private void KeyboardShortcuts_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Keyboard Shortcuts:\n\n" +
            "Search & Navigation:\n" +
            "  Tab              - Navigate between controls\n" +
            "  Ctrl+F           - Focus search box\n" +
            "  Enter            - Execute selected action\n" +
            "  Esc              - Clear search/filters\n\n" +
            "Actions:\n" +
            "  Ctrl+C           - Copy command to clipboard\n" +
            "  Ctrl+E           - Export configuration\n" +
            "  Ctrl+I           - Import configuration\n" +
            "  F1               - Help\n\n" +
            "Categories:\n" +
            "  Ctrl+M           - Manage categories\n\n" +
            "General:\n" +
            "  Ctrl+,           - Open settings\n" +
            "  Alt+F4           - Exit application",
            "Keyboard Shortcuts",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
            "Sprint 4: Advanced Features & Integration\n\n" +
            "Features:\n" +
            "• Direct PowerShell/Bash Execution\n" +
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
