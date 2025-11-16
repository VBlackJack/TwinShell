using Microsoft.Win32;
using System.Windows;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing application themes (Light/Dark mode).
/// Handles dynamic theme switching by merging ResourceDictionaries.
/// </summary>
public class ThemeService : IThemeService
{
    private Theme _currentTheme = Theme.Light;
    private const string LightThemeUri = "/TwinShell.App;component/Themes/LightTheme.xaml";
    private const string DarkThemeUri = "/TwinShell.App;component/Themes/DarkTheme.xaml";

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
}
