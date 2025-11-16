namespace TwinShell.Core.Constants;

/// <summary>
/// Validation constants for input validation across TwinShell.
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Maximum length for a parameter value (256 characters).
    /// </summary>
    public const int MaxParameterLength = 256;

    /// <summary>
    /// Maximum length for a command (1024 characters).
    /// </summary>
    public const int MaxCommandLength = 1024;

    /// <summary>
    /// Maximum length for a file path (260 characters, standard Windows MAX_PATH).
    /// </summary>
    public const int MaxPathLength = 260;

    /// <summary>
    /// Minimum search text length before triggering search.
    /// </summary>
    public const int MinSearchLength = 3;

    /// <summary>
    /// Minimum number of days for auto cleanup (1 day).
    /// </summary>
    public const int MinAutoCleanupDays = 1;

    /// <summary>
    /// Maximum number of days for auto cleanup (3650 days = ~10 years).
    /// </summary>
    public const int MaxAutoCleanupDays = 3650;

    /// <summary>
    /// Minimum number of history items to keep (10 items).
    /// </summary>
    public const int MinHistoryItems = 10;

    /// <summary>
    /// Maximum number of history items to keep (100,000 items).
    /// </summary>
    public const int MaxHistoryItems = 100000;

    /// <summary>
    /// Maximum number of recent commands to keep (50 commands).
    /// </summary>
    public const int MaxRecentCommands = 50;

    /// <summary>
    /// History retention in days (90 days).
    /// </summary>
    public const int HistoryRetentionDays = 90;
}
