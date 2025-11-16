using TwinShell.Core.Enums;
using TwinShell.Core.Models;

namespace TwinShell.Core.Helpers;

/// <summary>
/// Helper class for command template operations.
/// </summary>
public static class TemplateHelper
{
    /// <summary>
    /// Gets the active command template for an action based on the current platform.
    /// Prefers Windows template, falls back to Linux template.
    /// </summary>
    /// <param name="action">The action to get the template from</param>
    /// <returns>The active template, or null if no template is available</returns>
    public static CommandTemplate? GetActiveTemplate(Action action)
    {
        if (action == null)
            return null;

        return action.WindowsCommandTemplate ?? action.LinuxCommandTemplate;
    }

    /// <summary>
    /// Gets the platform for a given command template relative to an action.
    /// </summary>
    /// <param name="action">The action containing the templates</param>
    /// <param name="template">The template to check</param>
    /// <returns>The platform (Windows or Linux)</returns>
    public static Platform GetPlatformForTemplate(Action action, CommandTemplate template)
    {
        if (action == null || template == null)
            throw new ArgumentNullException();

        return template == action.WindowsCommandTemplate ? Platform.Windows : Platform.Linux;
    }

    /// <summary>
    /// Determines if a template is valid (not null and has parameters or base command).
    /// </summary>
    /// <param name="template">The template to validate</param>
    /// <returns>True if the template is valid</returns>
    public static bool IsValidTemplate(CommandTemplate? template)
    {
        return template != null && !string.IsNullOrWhiteSpace(template.BaseCommand);
    }
}
