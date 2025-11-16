using CommunityToolkit.Mvvm.Input;
using System.Windows;
using TwinShell.Core.Constants;

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
        _dialogService.ShowInfo(
            _localizationService.GetString(MessageKeys.HelpContent),
            _localizationService.GetString(MessageKeys.HelpTitle));
    }

    /// <summary>
    /// Refreshes the actions list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = _localizationService.GetString(MessageKeys.StatusRefreshing);
        try
        {
            await LoadActionsAsync();
            StatusMessage = _localizationService.GetFormattedString(MessageKeys.StatusActionsLoaded, _allActions.Count);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
