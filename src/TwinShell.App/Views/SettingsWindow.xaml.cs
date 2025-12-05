using System.Windows;
using System.Windows.Controls;
using TwinShell.App.ViewModels;

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
        };
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
}
