using Microsoft.Win32;
using System.Windows;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing application themes (Light/Dark mode).
/// Handles dynamic theme switching by merging ResourceDictionaries.
/// BUGFIX: Now listens to Windows theme changes for System mode.
/// </summary>
public class ThemeService : IThemeService, IDisposable
{
    private Theme _currentTheme = Theme.Light;
    private const string LightThemeUri = "/TwinShell.App;component/Themes/LightTheme.xaml";
    private const string DarkThemeUri = "/TwinShell.App;component/Themes/DarkTheme.xaml";

    /// <summary>
    /// Initializes the ThemeService and subscribes to Windows theme changes.
    /// </summary>
    public ThemeService()
    {
        // BUGFIX: Subscribe to Windows theme changes to support dynamic System theme switching
        if (OperatingSystem.IsWindows())
        {
            SystemEvents.UserPreferenceChanged += OnWindowsThemeChanged;
        }
    }

    /// <inheritdoc/>
    public Theme CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public void ApplyTheme(Theme theme)
    {
        var effectiveTheme = GetEffectiveTheme(theme);
        _currentTheme = theme;

        // Remove existing theme ResourceDictionaries
        RemoveExistingTheme();

        // Get the appropriate theme URI
        var themeUri = effectiveTheme == Theme.Dark ? DarkThemeUri : LightThemeUri;

        // Load and merge the new theme ResourceDictionary
        var themeResourceDictionary = new ResourceDictionary
        {
            Source = new Uri(themeUri, UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Add(themeResourceDictionary);
    }

    /// <inheritdoc/>
    public Theme GetEffectiveTheme(Theme theme)
    {
        if (theme == Theme.System)
        {
            return DetectSystemTheme();
        }

        return theme;
    }

    /// <inheritdoc/>
    public Theme DetectSystemTheme()
    {
        // BUGFIX: Check if running on Windows before accessing registry
        if (!OperatingSystem.IsWindows())
        {
            return Theme.Light; // Default to Light on non-Windows platforms
        }

        try
        {
            // Check Windows Registry for system theme preference
            // Path: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
            // Value: AppsUseLightTheme (0 = Dark, 1 = Light)
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0 ? Theme.Dark : Theme.Light;
                }
            }
        }
        catch (Exception)
        {
            // If registry access fails, default to Light theme
        }

        // Default to Light if unable to detect
        return Theme.Light;
    }

    /// <summary>
    /// Removes existing theme ResourceDictionaries from the application resources.
    /// </summary>
    private void RemoveExistingTheme()
    {
        var themesToRemove = Application.Current.Resources.MergedDictionaries
            .Where(d => d.Source != null &&
                       (d.Source.OriginalString.Contains("/Themes/LightTheme.xaml") ||
                        d.Source.OriginalString.Contains("/Themes/DarkTheme.xaml")))
            .ToList();

        foreach (var theme in themesToRemove)
        {
            Application.Current.Resources.MergedDictionaries.Remove(theme);
        }
    }

    /// <summary>
    /// BUGFIX: Handles Windows theme preference changes.
    /// When the user changes Windows theme and the app is in System mode, this updates the UI automatically.
    /// </summary>
    private void OnWindowsThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // Only react to General category changes (which includes theme changes)
        // and only if the app is currently using System theme
        if (e.Category == UserPreferenceCategory.General && _currentTheme == Theme.System)
        {
            // Use Dispatcher to ensure UI thread safety
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ApplyTheme(Theme.System);
            });
        }
    }

    /// <summary>
    /// Cleans up resources and unsubscribes from Windows theme events.
    /// </summary>
    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
        {
            SystemEvents.UserPreferenceChanged -= OnWindowsThemeChanged;
        }
    }
}
