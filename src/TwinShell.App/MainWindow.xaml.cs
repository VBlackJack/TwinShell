using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.Services;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;
using TwinShell.Core.Interfaces;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;
    private readonly StartupLogger _logger = StartupLogger.Instance;

    public MainWindow(MainViewModel viewModel, HistoryPanel historyPanel, OutputPanel outputPanel, IServiceProvider serviceProvider)
    {
        try
        {
            _logger.LogInfo("MainWindow constructor started");

            InitializeComponent();
            _logger.LogInfo("InitializeComponent completed");

            DataContext = viewModel;
            _mainViewModel = viewModel;
            _serviceProvider = serviceProvider;

            // Set the history panel (will be accessed by name in XAML)
            HistoryTabContent.Content = historyPanel;

            // Wire up the ExecutionViewModel to MainViewModel
            if (outputPanel.DataContext is ExecutionViewModel executionViewModel)
            {
                _mainViewModel.ExecutionViewModel = executionViewModel;
            }

            // Initialize SnackBar Service for visual feedback
            SnackBarService.Instance.Initialize(RootGrid);

            // BUGFIX: Extract async initialization to proper async method to prevent unhandled exceptions
            Loaded += MainWindow_Loaded;

            _logger.LogInfo("MainWindow constructor completed");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSync("MainWindow constructor error", ex);
            throw;
        }
    }

    /// <summary>
    /// BUGFIX: Proper async event handler to prevent unhandled exceptions from crashing the app
    /// Wraps InitializeAsync and theme initialization in try-catch for error handling
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // BUGFIX: Initialize theme after window is loaded to prevent UI thread deadlock
            var themeService = _serviceProvider.GetRequiredService<IThemeService>();
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

            var settings = await settingsService.LoadSettingsAsync();
            themeService.ApplyTheme(settings.Theme);

            await _mainViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Initialization error", ex);

            // LOCALIZATION: Use resource strings for error messages
            var localization = _serviceProvider.GetService<ILocalizationService>();
            var message = localization?.GetString("MessageInitializationError")
                ?? "Failed to initialize the application.\n\nPlease check the startup-error.log file for details.";
            var title = localization?.GetString("DialogTitleInitializationError") ?? "Initialization Error";

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
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
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }
}
