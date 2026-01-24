using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.Services;
using TwinShell.App.ViewModels;
using TwinShell.App.Views;
using TwinShell.Core.Constants;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;

namespace TwinShell.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;
    private readonly StartupLogger _logger = StartupLogger.Instance;
    private IBackdropEffectService? _backdropEffectService;

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

            // Apply Mica backdrop effect (Windows 11 only)
            ApplyBackdropEffect(settings.Theme);

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

    /// <summary>
    /// Applies Windows 11 Mica backdrop effect to the main window.
    /// Falls back gracefully on unsupported systems or when accessibility settings disable transparency.
    /// </summary>
    private void ApplyBackdropEffect(Theme currentTheme)
    {
        try
        {
            _backdropEffectService = _serviceProvider.GetService<IBackdropEffectService>();
            if (_backdropEffectService == null || !_backdropEffectService.IsBackdropEffectSupported)
            {
                _logger.LogInfo("Mica backdrop not supported or service unavailable");
                return;
            }

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                _logger.LogInfo("Window handle not available for Mica effect");
                return;
            }

            // Determine if dark mode based on effective theme
            var themeService = _serviceProvider.GetService<IThemeService>();
            var effectiveTheme = themeService?.GetEffectiveTheme(currentTheme) ?? currentTheme;
            bool isDarkMode = effectiveTheme == Theme.Dark || effectiveTheme == Theme.HighContrast;

            // Apply Mica effect
            bool success = _backdropEffectService.ApplyMica(hwnd, isDarkMode);
            if (success)
            {
                // Set window background to transparent to show Mica effect
                Background = System.Windows.Media.Brushes.Transparent;
                _logger.LogInfo($"Mica backdrop applied successfully (dark mode: {isDarkMode})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to apply Mica backdrop", ex);
            // Silently fall back to solid colors
        }
    }

    /// <summary>
    /// Updates the Mica backdrop effect when the theme changes.
    /// Called from ThemeService when themes are switched.
    /// </summary>
    public void OnThemeChanged(Theme newTheme)
    {
        if (_backdropEffectService == null || !_backdropEffectService.IsBackdropEffectSupported)
            return;

        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var themeService = _serviceProvider.GetService<IThemeService>();
            var effectiveTheme = themeService?.GetEffectiveTheme(newTheme) ?? newTheme;
            bool isDarkMode = effectiveTheme == Theme.Dark || effectiveTheme == Theme.HighContrast;

            _backdropEffectService.ApplyMica(hwnd, isDarkMode);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update Mica backdrop on theme change", ex);
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
        // TD-004: Use localized messages for keyboard shortcuts dialog
        var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
        var content = localizationService.GetString(MessageKeys.HelpKeyboardShortcutsContent);
        var title = localizationService.GetString(MessageKeys.HelpKeyboardShortcutsTitle);

        MessageBox.Show(
            content,
            title,
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

    /// <summary>
    /// Dispose all IDisposable ViewModels when the window closes to prevent resource leaks.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        // Dispose MainViewModel (contains SemaphoreSlim)
        _mainViewModel?.Dispose();

        // Dispose ExecutionViewModel if set
        if (_mainViewModel?.ExecutionViewModel is IDisposable disposableExecution)
        {
            disposableExecution.Dispose();
        }

        // Dispose HistoryPanel's ViewModel if it implements IDisposable
        if (HistoryTabContent.Content is HistoryPanel historyPanel &&
            historyPanel.DataContext is IDisposable disposableHistory)
        {
            disposableHistory.Dispose();
        }

        base.OnClosed(e);
    }
}
