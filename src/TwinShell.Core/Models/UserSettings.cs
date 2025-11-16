using TwinShell.Core.Enums;

namespace TwinShell.Core.Models;

/// <summary>
/// Represents user preferences and application settings.
/// Persisted in JSON format at %APPDATA%/TwinShell/settings.json
/// </summary>
public class UserSettings
{
    /// <summary>
    /// The selected application theme (Light, Dark, or System).
    /// </summary>
    public Theme Theme { get; set; } = Theme.Light;

    /// <summary>
    /// Number of days to retain command history before automatic cleanup.
    /// Minimum value: 1 day, Default: 90 days.
    /// </summary>
    public int AutoCleanupDays { get; set; } = 90;

    /// <summary>
    /// Maximum number of items to display in history views.
    /// Default: 1000 items.
    /// </summary>
    public int MaxHistoryItems { get; set; } = 1000;

    /// <summary>
    /// Default platform filter to apply when the application starts.
    /// Null means no default filter (show all platforms).
    /// </summary>
    public Platform? DefaultPlatformFilter { get; set; } = null;

    /// <summary>
    /// Whether to show the recent commands widget on startup.
    /// Default: true.
    /// </summary>
    public bool ShowRecentCommandsWidget { get; set; } = true;

    /// <summary>
    /// Whether to show confirmation dialog before executing dangerous commands.
    /// Default: true.
    /// </summary>
    public bool ConfirmDangerousActions { get; set; } = true;

    /// <summary>
    /// Default number of recent commands to display in the widget.
    /// Default: 5.
    /// </summary>
    public int RecentCommandsCount { get; set; } = 5;

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public UserSettings()
    {
    }

    /// <summary>
    /// Creates a deep copy of the settings.
    /// </summary>
    public UserSettings Clone()
    {
        return new UserSettings
        {
            Theme = this.Theme,
            AutoCleanupDays = this.AutoCleanupDays,
            MaxHistoryItems = this.MaxHistoryItems,
            DefaultPlatformFilter = this.DefaultPlatformFilter,
            ShowRecentCommandsWidget = this.ShowRecentCommandsWidget,
            ConfirmDangerousActions = this.ConfirmDangerousActions,
            RecentCommandsCount = this.RecentCommandsCount
        };
    }

    /// <summary>
    /// Gets the default user settings.
    /// </summary>
    public static UserSettings Default => new UserSettings();
}
