using System.Runtime.InteropServices;
using TwinShell.Core.Enums;

namespace TwinShell.Core.Helpers;

/// <summary>
/// Helper class for platform detection and operations.
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// Gets the current operating system platform.
    /// </summary>
    /// <returns>The current platform (Windows or Linux)</returns>
    public static Platform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Platform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Platform.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS is treated as Linux for command purposes
            return Platform.Linux;
        }

        // Default to Windows if unknown
        return Platform.Windows;
    }

    /// <summary>
    /// Checks if an action is compatible with the current platform.
    /// </summary>
    /// <param name="actionPlatform">The platform requirement of the action</param>
    /// <returns>True if compatible with current platform</returns>
    public static bool IsCompatibleWithCurrentPlatform(Platform actionPlatform)
    {
        if (actionPlatform == Platform.Both)
            return true;

        var currentPlatform = GetCurrentPlatform();
        return actionPlatform == currentPlatform;
    }

    /// <summary>
    /// Gets a human-readable platform name.
    /// </summary>
    /// <param name="platform">The platform</param>
    /// <returns>Platform name as string</returns>
    public static string GetPlatformName(Platform platform)
    {
        return platform switch
        {
            Platform.Windows => "Windows",
            Platform.Linux => "Linux",
            Platform.Both => "Cross-Platform",
            _ => "Unknown"
        };
    }
}
