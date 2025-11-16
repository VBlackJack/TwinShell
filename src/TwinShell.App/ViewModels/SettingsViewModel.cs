using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// Manages user preferences including theme selection.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private UserSettings _originalSettings;

    [ObservableProperty]
    private Theme _selectedTheme;

    [ObservableProperty]
    private int _autoCleanupDays;

    [ObservableProperty]
    private int _maxHistoryItems;

    [ObservableProperty]
    private int _recentCommandsCount;

    [ObservableProperty]
    private bool _showRecentCommandsWidget;

    [ObservableProperty]
    private bool _confirmDangerousActions;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _hasChanges;

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _originalSettings = _settingsService.CurrentSettings.Clone();

        // Load current settings
        LoadCurrentSettings();
    }

    /// <summary>
    /// Loads the current settings into the ViewModel properties.
    /// </summary>
    private void LoadCurrentSettings()
    {
        var settings = _settingsService.CurrentSettings;

        SelectedTheme = settings.Theme;
        AutoCleanupDays = settings.AutoCleanupDays;
        MaxHistoryItems = settings.MaxHistoryItems;
        RecentCommandsCount = settings.RecentCommandsCount;
        ShowRecentCommandsWidget = settings.ShowRecentCommandsWidget;
        ConfirmDangerousActions = settings.ConfirmDangerousActions;

        _originalSettings = settings.Clone();
        HasChanges = false;
    }

    /// <summary>
    /// Called when any setting property changes to track if there are unsaved changes.
    /// </summary>
    partial void OnSelectedThemeChanged(Theme value) => CheckForChanges();
    partial void OnAutoCleanupDaysChanged(int value) => CheckForChanges();
    partial void OnMaxHistoryItemsChanged(int value) => CheckForChanges();
    partial void OnRecentCommandsCountChanged(int value) => CheckForChanges();
    partial void OnShowRecentCommandsWidgetChanged(bool value) => CheckForChanges();
    partial void OnConfirmDangerousActionsChanged(bool value) => CheckForChanges();

    /// <summary>
    /// Checks if current settings differ from original settings.
    /// </summary>
    private void CheckForChanges()
    {
        HasChanges = SelectedTheme != _originalSettings.Theme ||
                     AutoCleanupDays != _originalSettings.AutoCleanupDays ||
                     MaxHistoryItems != _originalSettings.MaxHistoryItems ||
                     RecentCommandsCount != _originalSettings.RecentCommandsCount ||
                     ShowRecentCommandsWidget != _originalSettings.ShowRecentCommandsWidget ||
                     ConfirmDangerousActions != _originalSettings.ConfirmDangerousActions;
    }

    /// <summary>
    /// Validates the current settings values.
    /// </summary>
    private bool ValidateCurrentSettings()
    {
        if (AutoCleanupDays < 1 || AutoCleanupDays > 3650)
        {
            ValidationError = "Auto cleanup days must be between 1 and 3650.";
            return false;
        }

        if (MaxHistoryItems < 10 || MaxHistoryItems > 100000)
        {
            ValidationError = "Max history items must be between 10 and 100,000.";
            return false;
        }

        if (RecentCommandsCount < 1 || RecentCommandsCount > 50)
        {
            ValidationError = "Recent commands count must be between 1 and 50.";
            return false;
        }

        ValidationError = null;
        return true;
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validate before saving
        if (!ValidateCurrentSettings())
        {
            MessageBox.Show(ValidationError, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Create new settings object
        var newSettings = new UserSettings
        {
            Theme = SelectedTheme,
            AutoCleanupDays = AutoCleanupDays,
            MaxHistoryItems = MaxHistoryItems,
            RecentCommandsCount = RecentCommandsCount,
            ShowRecentCommandsWidget = ShowRecentCommandsWidget,
            ConfirmDangerousActions = ConfirmDangerousActions,
            DefaultPlatformFilter = _originalSettings.DefaultPlatformFilter
        };

        // Save settings
        var success = await _settingsService.SaveSettingsAsync(newSettings);

        if (success)
        {
            // Apply theme if it changed
            if (SelectedTheme != _originalSettings.Theme)
            {
                _themeService.ApplyTheme(SelectedTheme);
            }

            _originalSettings = newSettings.Clone();
            HasChanges = false;

            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Failed to save settings. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Cancels changes and reverts to original settings.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        LoadCurrentSettings();
        ValidationError = null;
    }

    /// <summary>
    /// Resets all settings to default values.
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all settings to default values?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var defaultSettings = await _settingsService.ResetToDefaultAsync();
            _themeService.ApplyTheme(defaultSettings.Theme);
            LoadCurrentSettings();

            MessageBox.Show("Settings reset to default values.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Applies the selected theme immediately for preview.
    /// </summary>
    [RelayCommand]
    private void PreviewTheme()
    {
        _themeService.ApplyTheme(SelectedTheme);
    }
}
