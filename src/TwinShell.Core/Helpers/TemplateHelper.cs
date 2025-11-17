using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Core.Helpers;

/// <summary>
/// Helper class for command template operations.
/// </summary>
public static class TemplateHelper
{
    /// <summary>
    /// Gets the active command template for an action based on the current platform.
    /// Returns the template matching the current platform, with fallback logic.
    /// </summary>
    /// <param name="action">The action to get the template from</param>
    /// <returns>The active template, or null if no template is available</returns>
    public static CommandTemplate? GetActiveTemplate(ActionModel action)
    {
        if (action == null)
            return null;

        // BUGFIX: Use PlatformHelper to get the current platform instead of hardcoding preference
        var currentPlatform = PlatformHelper.GetCurrentPlatform();

        // Return the template for the current platform, with fallback to the other platform
        if (currentPlatform == Platform.Windows)
        {
            return action.WindowsCommandTemplate ?? action.LinuxCommandTemplate;
        }
        else
        {
            return action.LinuxCommandTemplate ?? action.WindowsCommandTemplate;
        }
    }

    /// <summary>
    /// Gets the platform for a given command template relative to an action.
    /// </summary>
    /// <param name="action">The action containing the templates</param>
    /// <param name="template">The template to check</param>
    /// <returns>The platform (Windows or Linux)</returns>
    public static Platform GetPlatformForTemplate(ActionModel action, CommandTemplate template)
    {
        // BUGFIX: Specify parameter name in ArgumentNullException
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        return template == action.WindowsCommandTemplate ? Platform.Windows : Platform.Linux;
    }

    /// <summary>
    /// Determines if a template is valid (not null and has parameters or base command).
    /// </summary>
    /// <param name="template">The template to validate</param>
    /// <returns>True if the template is valid</returns>
    public static bool IsValidTemplate(CommandTemplate? template)
    {
        return template != null && !string.IsNullOrWhiteSpace(template.CommandPattern);
    }
}
