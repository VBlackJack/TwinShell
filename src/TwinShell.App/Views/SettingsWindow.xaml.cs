using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.ViewModels;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Views;

/// <summary>
/// Settings window for configuring user preferences.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Load existing token into PasswordBox (PasswordBox doesn't support binding)
        Loaded += (s, e) =>
        {
            if (viewModel.GitAccessToken != null)
            {
                GitAccessTokenBox.Password = viewModel.GitAccessToken;
            }
            ApplyAcrylicBackdrop();
        };
    }

    private void ApplyAcrylicBackdrop()
    {
        var backdropService = App.ServiceProvider?.GetService<IBackdropEffectService>();
        if (backdropService?.IsBackdropEffectSupported == true)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var isDark = Application.Current.Resources["BackgroundBrush"] is SolidColorBrush brush
                         && brush.Color.R < 128;
            if (backdropService.ApplyAcrylic(hwnd, isDark))
            {
                Background = Brushes.Transparent;
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Auto-preview theme when selection changes
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.PreviewThemeCommand.Execute(null);
        }
    }

    private void GitAccessTokenBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        // PasswordBox doesn't support binding for security reasons
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.GitAccessToken = GitAccessTokenBox.Password;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Dispose ViewModel to unsubscribe from events
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.Dispose();
        }
        base.OnClosed(e);
    }

    // Custom Title Bar Button Handlers
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
