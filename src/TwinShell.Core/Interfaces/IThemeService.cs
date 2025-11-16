using TwinShell.Core.Enums;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Service for managing application themes (Light/Dark mode).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    Theme CurrentTheme { get; }

    /// <summary>
    /// Applies the specified theme to the application.
    /// This merges the appropriate ResourceDictionary into Application.Current.Resources.
    /// </summary>
    /// <param name="theme">The theme to apply (Light, Dark, or System).</param>
    void ApplyTheme(Theme theme);

    /// <summary>
    /// Gets the effective theme based on the specified theme and system settings.
    /// If theme is System, returns the current Windows theme (Light or Dark).
    /// </summary>
    /// <param name="theme">The theme preference.</param>
    /// <returns>The effective theme (Light or Dark).</returns>
    Theme GetEffectiveTheme(Theme theme);

    /// <summary>
    /// Detects the current Windows system theme.
    /// </summary>
    /// <returns>Light or Dark based on Windows theme settings.</returns>
    Theme DetectSystemTheme();
}
