using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace TwinShell.App.ViewModels;

/// <summary>
/// Partial class containing additional commands for MainViewModel.
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Opens the Settings window.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        // This will be handled by MainWindow code-behind
        Application.Current.MainWindow?.Focus();
        // Trigger via routed event or direct call
    }

    /// <summary>
    /// Opens the Categories management window.
    /// </summary>
    [RelayCommand]
    private void OpenCategories()
    {
        Application.Current.MainWindow?.Focus();
    }

    /// <summary>
    /// Shows the help dialog.
    /// </summary>
    [RelayCommand]
    private void ShowHelp()
    {
        MessageBox.Show(
            "TwinShell - Help\n\n" +
            "Keyboard Shortcuts:\n" +
            "  Ctrl+,       - Open Settings\n" +
            "  Ctrl+M       - Manage Categories\n" +
            "  Ctrl+E       - Export Configuration\n" +
            "  Ctrl+I       - Import Configuration\n" +
            "  F1           - Show this Help\n" +
            "  F5           - Refresh Actions\n" +
            "  Esc          - Clear Search\n\n" +
            "For more information, visit the documentation.",
            "Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Refreshes the actions list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing...";
        try
        {
            await LoadActionsAsync();
            StatusMessage = $"{_allActions.Count} actions loaded";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
