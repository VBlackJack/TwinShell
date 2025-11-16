using System.Windows;
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
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ThemeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Auto-preview theme when selection changes
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.PreviewThemeCommand.Execute(null);
        }
    }
}
