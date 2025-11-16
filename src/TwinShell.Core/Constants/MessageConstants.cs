namespace TwinShell.Core.Constants;

/// <summary>
/// Message keys for localization and error messages.
/// These keys should be used with ILocalizationService to get localized messages.
/// </summary>
public static class MessageKeys
{
    // Common messages
    public const string Ready = "Common.Ready";
    public const string Loading = "Common.Loading";
    public const string Error = "Common.Error";
    public const string Success = "Common.Success";
    public const string Warning = "Common.Warning";
    public const string Confirmation = "Common.Confirmation";

    // Validation messages
    public const string ParameterRequired = "Validation.ParameterRequired";
    public const string ParameterMustBeInteger = "Validation.ParameterMustBeInteger";
    public const string ParameterMustBeBoolean = "Validation.ParameterMustBeBoolean";
    public const string ValidationErrors = "Validation.Errors";
    public const string NoCommandTemplate = "Validation.NoCommandTemplate";
    public const string NoValidCommand = "Validation.NoValidCommand";

    // Additional validation messages (aliases for consistency)
    public const string ValidationParameterRequired = ParameterRequired;
    public const string ValidationParameterMustBeInteger = ParameterMustBeInteger;
    public const string ValidationParameterMustBeBoolean = ParameterMustBeBoolean;

    // New validation messages
    public const string ValidationParameterMaxLength = "Validation.ParameterMaxLength";
    public const string ValidationParameterDangerousCharacters = "Validation.ParameterDangerousCharacters";
    public const string ValidationParameterInvalidHostname = "Validation.ParameterInvalidHostname";
    public const string ValidationParameterInvalidIPAddress = "Validation.ParameterInvalidIPAddress";
    public const string ValidationParameterInvalidPath = "Validation.ParameterInvalidPath";

    // Execution messages
    public const string ExecutionError = "Execution.Error";
    public const string ExecutionSuccess = "Execution.Success";
    public const string ExecutionCancelled = "Execution.Cancelled";
    public const string ExecutionTimeout = "Execution.Timeout";

    // Favorites messages
    public const string FavoriteAdded = "Favorites.Added";
    public const string FavoriteRemoved = "Favorites.Removed";
    public const string FavoritesLimitReached = "Favorites.LimitReached";
    public const string FavoritesLimitReachedMessage = "Favorites.LimitReachedMessage";

    // Configuration messages
    public const string ConfigExportSuccess = "Config.ExportSuccess";
    public const string ConfigExportError = "Config.ExportError";
    public const string ConfigImportSuccess = "Config.ImportSuccess";
    public const string ConfigImportError = "Config.ImportError";
    public const string ConfigValidationError = "Config.ValidationError";
    public const string ConfigImportConfirmation = "Config.ImportConfirmation";
    public const string ConfigImportConfirmationMessage = "Config.ImportConfirmationMessage";

    // Help messages
    public const string HelpTitle = "Help.Title";
    public const string HelpContent = "Help.Content";

    // Status messages
    public const string ActionsLoaded = "Status.ActionsLoaded";
    public const string Refreshing = "Status.Refreshing";
}
