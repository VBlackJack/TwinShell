using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.Services;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;

    public MainWindow(MainViewModel viewModel, HistoryPanel historyPanel, RecentCommandsWidget recentCommandsWidget, OutputPanel outputPanel, IServiceProvider serviceProvider)
    {
        try
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow constructor started\n");

            InitializeComponent();
            System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] InitializeComponent completed\n");

            DataContext = viewModel;
            _mainViewModel = viewModel;
            _serviceProvider = serviceProvider;

            // Set the history panel (will be accessed by name in XAML)
            HistoryTabContent.Content = historyPanel;

            // Set the recent commands widget
            RecentCommandsContainer.Child = recentCommandsWidget;

        // Set the execution output panel (Sprint 4)
        // TODO: Uncomment when ExecutionTabContent is added to MainWindow.xaml
        // ExecutionTabContent.Content = outputPanel;

        // Wire up the ExecutionViewModel to MainViewModel
        if (outputPanel.DataContext is ExecutionViewModel executionViewModel)
        {
            _mainViewModel.ExecutionViewModel = executionViewModel;
        }

            // Initialize SnackBar Service for visual feedback
            SnackBarService.Instance.Initialize(RootGrid);

            // BUGFIX: Extract async initialization to proper async method to prevent unhandled exceptions
            Loaded += MainWindow_Loaded;

            System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow constructor completed\n");
        }
        catch (Exception ex)
        {
            // SECURITY: Sanitize exception logging
            System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow constructor error\n" +
                $"Type: {ex.GetType().Name}\n" +
                $"Message: {ex.Message}\n\n");
            throw;
        }
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
            // SECURITY: Don't expose detailed error messages
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Initialization error: {ex.GetType().Name} - {ex.Message}\n");

            MessageBox.Show(
                "Échec de l'initialisation de l'application.\n\nConsultez le fichier startup-error.log pour plus de détails.",
                "Erreur d'initialisation",
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
            "Author: Julien Bombled\n\n" +
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
