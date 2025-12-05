using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// Manages user preferences including theme selection and GitOps synchronization.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly ISyncService _syncService;
    private readonly IGitSyncService _gitSyncService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private UserSettings _originalSettings;

    [ObservableProperty]
    private Theme _selectedTheme;

    [ObservableProperty]
    private int _autoCleanupDays;

    [ObservableProperty]
    private int _maxHistoryItems;

    [ObservableProperty]
    private bool _confirmDangerousActions;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _hasChanges;

    // --- Propriétés GitOps ---

    [ObservableProperty]
    private string? _gitRepositoryPath;

    [ObservableProperty]
    private string? _gitRemoteUrl;

    [ObservableProperty]
    private string _gitBranch = "main";

    [ObservableProperty]
    private string _gitAuthMethod = "https";

    [ObservableProperty]
    private string? _gitAccessToken;

    [ObservableProperty]
    private string? _gitUserName;

    [ObservableProperty]
    private string? _gitUserEmail;

    [ObservableProperty]
    private bool _gitSyncOnStartup = true;

    [ObservableProperty]
    private bool _gitAutoPush = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSync))]
    [NotifyPropertyChangedFor(nameof(CanTestConnection))]
    private bool _isSyncing;

    [ObservableProperty]
    private string? _syncStatusMessage;

    [ObservableProperty]
    private string? _gitRepositoryStatus;

    [ObservableProperty]
    private bool _isGitConfigured;

    public bool CanSync => !IsSyncing && !string.IsNullOrWhiteSpace(GitRepositoryPath);
    public bool CanTestConnection => !IsSyncing && !string.IsNullOrWhiteSpace(GitRemoteUrl);

    public string[] AuthMethods => new[] { "https", "ssh" };
    public string[] CommonBranches => new[] { "main", "master", "develop" };

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        ISyncService syncService,
        IGitSyncService gitSyncService,
        IDialogService dialogService,
        INotificationService notificationService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _syncService = syncService;
        _gitSyncService = gitSyncService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _originalSettings = _settingsService.CurrentSettings.Clone();

        // Subscribe to Git status changes
        _gitSyncService.StatusChanged += OnGitStatusChanged;

        // Load current settings
        LoadCurrentSettings();

        // Refresh Git repository status
        _ = RefreshGitStatusAsync();
    }

    private void OnGitStatusChanged(object? sender, GitSyncStatusEventArgs e)
    {
        SyncStatusMessage = e.Status;
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
        ConfirmDangerousActions = settings.ConfirmDangerousActions;

        // Git settings
        GitRepositoryPath = settings.GitRepositoryPath;
        GitRemoteUrl = settings.GitRemoteUrl;
        GitBranch = settings.GitBranch;
        GitAuthMethod = settings.GitAuthMethod;
        GitAccessToken = settings.GitAccessToken;
        GitUserName = settings.GitUserName;
        GitUserEmail = settings.GitUserEmail;
        GitSyncOnStartup = settings.GitSyncOnStartup;
        GitAutoPush = settings.GitAutoPush;

        IsGitConfigured = _gitSyncService.IsConfigured;

        _originalSettings = settings.Clone();
        HasChanges = false;
    }

    /// <summary>
    /// Called when any setting property changes to track if there are unsaved changes.
    /// </summary>
    partial void OnSelectedThemeChanged(Theme value) => CheckForChanges();
    partial void OnAutoCleanupDaysChanged(int value) => CheckForChanges();
    partial void OnMaxHistoryItemsChanged(int value) => CheckForChanges();
    partial void OnConfirmDangerousActionsChanged(bool value) => CheckForChanges();
    partial void OnGitRepositoryPathChanged(string? value)
    {
        CheckForChanges();
        OnPropertyChanged(nameof(CanSync));
    }
    partial void OnGitRemoteUrlChanged(string? value)
    {
        CheckForChanges();
        OnPropertyChanged(nameof(CanTestConnection));
    }
    partial void OnGitBranchChanged(string value) => CheckForChanges();
    partial void OnGitAuthMethodChanged(string value) => CheckForChanges();
    partial void OnGitAccessTokenChanged(string? value) => CheckForChanges();
    partial void OnGitUserNameChanged(string? value) => CheckForChanges();
    partial void OnGitUserEmailChanged(string? value) => CheckForChanges();
    partial void OnGitSyncOnStartupChanged(bool value) => CheckForChanges();
    partial void OnGitAutoPushChanged(bool value) => CheckForChanges();

    partial void OnIsSyncingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSync));
        OnPropertyChanged(nameof(CanTestConnection));
    }

    /// <summary>
    /// Checks if current settings differ from original settings.
    /// </summary>
    private void CheckForChanges()
    {
        HasChanges = SelectedTheme != _originalSettings.Theme ||
                     AutoCleanupDays != _originalSettings.AutoCleanupDays ||
                     MaxHistoryItems != _originalSettings.MaxHistoryItems ||
                     ConfirmDangerousActions != _originalSettings.ConfirmDangerousActions ||
                     GitRepositoryPath != _originalSettings.GitRepositoryPath ||
                     GitRemoteUrl != _originalSettings.GitRemoteUrl ||
                     GitBranch != _originalSettings.GitBranch ||
                     GitAuthMethod != _originalSettings.GitAuthMethod ||
                     GitAccessToken != _originalSettings.GitAccessToken ||
                     GitUserName != _originalSettings.GitUserName ||
                     GitUserEmail != _originalSettings.GitUserEmail ||
                     GitSyncOnStartup != _originalSettings.GitSyncOnStartup ||
                     GitAutoPush != _originalSettings.GitAutoPush;
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
            ConfirmDangerousActions = ConfirmDangerousActions,
            DefaultPlatformFilter = _originalSettings.DefaultPlatformFilter,
            GitRepositoryPath = GitRepositoryPath,
            GitRemoteUrl = GitRemoteUrl,
            GitBranch = GitBranch,
            GitAuthMethod = GitAuthMethod,
            GitAccessToken = GitAccessToken,
            GitUserName = GitUserName,
            GitUserEmail = GitUserEmail,
            GitSyncOnStartup = GitSyncOnStartup,
            GitAutoPush = GitAutoPush
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

    // --- Commandes GitOps ---

    /// <summary>
    /// Opens folder browser dialog to select Git repository path.
    /// </summary>
    [RelayCommand]
    private void BrowseGitRepository()
    {
        var selectedPath = _dialogService.ShowFolderBrowserDialog(
            "Select Git Repository Folder for GitOps Synchronization",
            GitRepositoryPath);

        if (!string.IsNullOrEmpty(selectedPath))
        {
            GitRepositoryPath = selectedPath;
        }
    }

    /// <summary>
    /// Exports all data to YAML files in the Git repository folder.
    /// </summary>
    [RelayCommand]
    private async Task ExportToYamlAsync()
    {
        if (string.IsNullOrWhiteSpace(GitRepositoryPath))
        {
            _dialogService.ShowWarning("Please select a Git repository folder first.", "Export");
            return;
        }

        if (!Directory.Exists(GitRepositoryPath))
        {
            var create = _dialogService.ShowQuestion(
                $"The folder '{GitRepositoryPath}' does not exist. Do you want to create it?",
                "Create Folder");

            if (create)
            {
                try
                {
                    Directory.CreateDirectory(GitRepositoryPath);
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError($"Failed to create folder: {ex.Message}", "Error");
                    return;
                }
            }
            else
            {
                return;
            }
        }

        IsSyncing = true;
        SyncStatusMessage = "Exporting data to YAML...";

        try
        {
            var result = await _syncService.ExportDataToYamlAsync(GitRepositoryPath);

            if (result.Success)
            {
                var message = $"Export completed successfully!\n\n" +
                              $"Actions: {result.ActionsExported}\n" +
                              $"Batches: {result.BatchesExported}\n" +
                              $"Templates: {result.TemplatesExported}\n" +
                              $"Categories: {result.CategoriesExported}\n" +
                              $"Total: {result.TotalExported} items";

                if (result.Warnings.Count > 0)
                {
                    message += $"\n\nWarnings ({result.Warnings.Count}):\n" +
                               string.Join("\n", result.Warnings.Take(5));
                    if (result.Warnings.Count > 5)
                    {
                        message += $"\n... and {result.Warnings.Count - 5} more";
                    }
                }

                _notificationService.ShowSuccess($"Exported {result.TotalExported} items to YAML", "Export Complete");
                _dialogService.ShowSuccess(message, "Export Complete");
                SyncStatusMessage = $"Export completed: {result.TotalExported} items.";
            }
            else
            {
                var errorMessage = "Export failed:\n" + string.Join("\n", result.Errors);
                _notificationService.ShowError("Export failed", "Export Error");
                _dialogService.ShowError(errorMessage, "Export Error");
                SyncStatusMessage = "Export failed.";
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Export failed: {ex.Message}", "Export Error");
            _dialogService.ShowError($"An error occurred during export:\n\n{ex.Message}", "Export Error");
            SyncStatusMessage = "Export failed.";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    /// <summary>
    /// Imports data from YAML files in the Git repository folder.
    /// </summary>
    [RelayCommand]
    private async Task ImportFromYamlAsync()
    {
        if (string.IsNullOrWhiteSpace(GitRepositoryPath))
        {
            _dialogService.ShowWarning("Please select a Git repository folder first.", "Import");
            return;
        }

        if (!Directory.Exists(GitRepositoryPath))
        {
            _dialogService.ShowError($"The folder '{GitRepositoryPath}' does not exist.", "Import Error");
            return;
        }

        // Validate before import
        var validation = await _syncService.ValidateFolderAsync(GitRepositoryPath);

        if (validation.TotalFilesFound == 0)
        {
            _dialogService.ShowWarning(
                "No YAML files found in the selected folder.\n\n" +
                "Expected folder structure:\n" +
                "  /actions/{category}/*.yaml\n" +
                "  /batches/*.yaml\n" +
                "  /templates/*.yaml\n" +
                "  /categories/*.yaml",
                "No Files Found");
            return;
        }

        var confirmMessage = $"Found {validation.TotalFilesFound} YAML files:\n\n" +
                            $"Actions: {validation.ActionFilesFound}\n" +
                            $"Batches: {validation.BatchFilesFound}\n" +
                            $"Templates: {validation.TemplateFilesFound}\n" +
                            $"Categories: {validation.CategoryFilesFound}\n\n" +
                            "Do you want to import these files?\n" +
                            "(Existing items with matching IDs will be updated)";

        if (!_dialogService.ShowQuestion(confirmMessage, "Confirm Import"))
        {
            return;
        }

        IsSyncing = true;
        SyncStatusMessage = "Importing data from YAML...";

        try
        {
            var result = await _syncService.ImportDataFromYamlAsync(GitRepositoryPath);

            if (result.Success)
            {
                var message = $"Import completed successfully!\n\n" +
                              $"Created:\n" +
                              $"  Actions: {result.ActionsCreated}\n" +
                              $"  Batches: {result.BatchesCreated}\n" +
                              $"  Templates: {result.TemplatesCreated}\n" +
                              $"  Categories: {result.CategoriesCreated}\n\n" +
                              $"Updated:\n" +
                              $"  Actions: {result.ActionsUpdated}\n" +
                              $"  Batches: {result.BatchesUpdated}\n" +
                              $"  Templates: {result.TemplatesUpdated}\n" +
                              $"  Categories: {result.CategoriesUpdated}\n\n" +
                              $"Total: {result.TotalCreated} created, {result.TotalUpdated} updated";

                if (result.Warnings.Count > 0)
                {
                    message += $"\n\nWarnings ({result.Warnings.Count}):\n" +
                               string.Join("\n", result.Warnings.Take(5));
                    if (result.Warnings.Count > 5)
                    {
                        message += $"\n... and {result.Warnings.Count - 5} more";
                    }
                }

                _notificationService.ShowSuccess(
                    $"Imported {result.TotalCreated} new items, updated {result.TotalUpdated}",
                    "Import Complete");
                _dialogService.ShowSuccess(message, "Import Complete");
                SyncStatusMessage = $"Import completed: {result.TotalCreated} created, {result.TotalUpdated} updated.";
            }
            else
            {
                var errorMessage = "Import failed:\n" + string.Join("\n", result.Errors);
                _notificationService.ShowError("Import failed", "Import Error");
                _dialogService.ShowError(errorMessage, "Import Error");
                SyncStatusMessage = "Import failed.";
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Import failed: {ex.Message}", "Import Error");
            _dialogService.ShowError($"An error occurred during import:\n\n{ex.Message}", "Import Error");
            SyncStatusMessage = "Import failed.";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    // --- Commandes Git Sync ---

    /// <summary>
    /// Tests the connection to the remote Git repository.
    /// </summary>
    [RelayCommand]
    private async Task TestGitConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(GitRemoteUrl))
        {
            _dialogService.ShowWarning("Please enter a remote Git URL first.", "Test Connection");
            return;
        }

        // Save settings first to ensure GitSyncService has the latest config
        await SaveAsync();

        IsSyncing = true;
        SyncStatusMessage = "Testing connection...";

        try
        {
            var result = await _gitSyncService.TestConnectionAsync();

            if (result.Success)
            {
                _notificationService.ShowSuccess("Connection successful!", "Git");
                _dialogService.ShowSuccess(result.Message, "Connection Test");
            }
            else
            {
                _dialogService.ShowError($"{result.Message}\n\n{result.ErrorDetails}", "Connection Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Connection test failed:\n\n{ex.Message}", "Error");
        }
        finally
        {
            IsSyncing = false;
            await RefreshGitStatusAsync();
        }
    }

    /// <summary>
    /// Initializes/clones the Git repository.
    /// </summary>
    [RelayCommand]
    private async Task InitializeGitRepositoryAsync()
    {
        if (string.IsNullOrWhiteSpace(GitRemoteUrl) || string.IsNullOrWhiteSpace(GitRepositoryPath))
        {
            _dialogService.ShowWarning("Please configure remote URL and local path first.", "Initialize Repository");
            return;
        }

        // Save settings first
        await SaveAsync();

        IsSyncing = true;
        SyncStatusMessage = "Initializing repository...";

        try
        {
            var result = await _gitSyncService.InitializeRepositoryAsync();

            if (result.Success)
            {
                _notificationService.ShowSuccess("Repository initialized!", "Git");
                _dialogService.ShowSuccess(result.Message, "Repository Initialized");
            }
            else
            {
                _dialogService.ShowError($"{result.Message}\n\n{result.ErrorDetails}", "Initialization Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Initialization failed:\n\n{ex.Message}", "Error");
        }
        finally
        {
            IsSyncing = false;
            await RefreshGitStatusAsync();
        }
    }

    /// <summary>
    /// Pulls changes from remote and imports into database.
    /// </summary>
    [RelayCommand]
    private async Task GitPullAsync()
    {
        if (!_gitSyncService.IsConfigured)
        {
            _dialogService.ShowWarning("Git repository not configured. Please configure and initialize first.", "Pull");
            return;
        }

        IsSyncing = true;
        SyncStatusMessage = "Pulling changes...";

        try
        {
            var result = await _gitSyncService.PullAndImportAsync();

            if (result.Success)
            {
                var message = $"{result.Message}\n\n" +
                              $"Commits merged: {result.CommitsMerged}\n" +
                              $"Items imported: {result.ItemsImported}";
                _notificationService.ShowSuccess($"Pull complete: {result.ItemsImported} items imported", "Git");
                _dialogService.ShowSuccess(message, "Pull Complete");
            }
            else
            {
                _dialogService.ShowError($"{result.Message}\n\n{result.ErrorDetails}", "Pull Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Pull failed:\n\n{ex.Message}", "Error");
        }
        finally
        {
            IsSyncing = false;
            await RefreshGitStatusAsync();
        }
    }

    /// <summary>
    /// Exports database to YAML, commits and pushes to remote.
    /// </summary>
    [RelayCommand]
    private async Task GitPushAsync()
    {
        if (!_gitSyncService.IsConfigured)
        {
            _dialogService.ShowWarning("Git repository not configured. Please configure and initialize first.", "Push");
            return;
        }

        IsSyncing = true;
        SyncStatusMessage = "Exporting and pushing...";

        try
        {
            var result = await _gitSyncService.ExportAndPushAsync();

            if (result.Success)
            {
                var message = $"{result.Message}\n\n" +
                              $"Items exported: {result.ItemsExported}";
                _notificationService.ShowSuccess($"Push complete: {result.ItemsExported} items exported", "Git");
                _dialogService.ShowSuccess(message, "Push Complete");
            }
            else
            {
                _dialogService.ShowError($"{result.Message}\n\n{result.ErrorDetails}", "Push Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Push failed:\n\n{ex.Message}", "Error");
        }
        finally
        {
            IsSyncing = false;
            await RefreshGitStatusAsync();
        }
    }

    /// <summary>
    /// Performs a full sync: pull, import, export, push.
    /// </summary>
    [RelayCommand]
    private async Task GitFullSyncAsync()
    {
        if (!_gitSyncService.IsConfigured)
        {
            _dialogService.ShowWarning("Git repository not configured. Please configure and initialize first.", "Full Sync");
            return;
        }

        IsSyncing = true;
        SyncStatusMessage = "Running full sync...";

        try
        {
            var result = await _gitSyncService.FullSyncAsync();

            if (result.Success)
            {
                var message = $"{result.Message}\n\n" +
                              $"Commits merged: {result.CommitsMerged}\n" +
                              $"Items imported: {result.ItemsImported}\n" +
                              $"Items exported: {result.ItemsExported}";
                _notificationService.ShowSuccess("Full sync completed!", "Git");
                _dialogService.ShowSuccess(message, "Full Sync Complete");
            }
            else
            {
                _dialogService.ShowError($"{result.Message}\n\n{result.ErrorDetails}", "Sync Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Sync failed:\n\n{ex.Message}", "Error");
        }
        finally
        {
            IsSyncing = false;
            await RefreshGitStatusAsync();
        }
    }

    /// <summary>
    /// Refreshes the Git repository status display.
    /// </summary>
    private async Task RefreshGitStatusAsync()
    {
        try
        {
            IsGitConfigured = _gitSyncService.IsConfigured;

            if (!IsGitConfigured)
            {
                GitRepositoryStatus = "Not configured";
                return;
            }

            var status = await _gitSyncService.GetRepositoryStatusAsync();

            if (!status.IsInitialized)
            {
                GitRepositoryStatus = "Repository not initialized";
                return;
            }

            var statusParts = new List<string>
            {
                $"Branch: {status.CurrentBranch}"
            };

            if (status.CommitsAhead > 0 || status.CommitsBehind > 0)
            {
                statusParts.Add($"↑{status.CommitsAhead} ↓{status.CommitsBehind}");
            }

            if (status.HasLocalChanges)
            {
                statusParts.Add("(modified)");
            }

            GitRepositoryStatus = string.Join(" | ", statusParts);
        }
        catch
        {
            GitRepositoryStatus = "Unable to get status";
        }
    }
}
