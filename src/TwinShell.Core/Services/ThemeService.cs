using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ThemeService>? _logger;

    /// <summary>
    /// Initializes the ThemeService and subscribes to Windows theme changes.
    /// </summary>
    public ThemeService(ILogger<ThemeService>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("ThemeService initialized");

        // BUGFIX: Subscribe to Windows theme changes to support dynamic System theme switching
        if (OperatingSystem.IsWindows())
        {
            SystemEvents.UserPreferenceChanged += OnWindowsThemeChanged;
            _logger?.LogDebug("Subscribed to Windows theme changes");
        }
    }

    /// <inheritdoc/>
    public Theme CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public void ApplyTheme(Theme theme)
    {
        try
        {
            _logger?.LogInformation($"Applying theme: {theme}");

            var effectiveTheme = GetEffectiveTheme(theme);
            _currentTheme = theme;

            _logger?.LogDebug($"Effective theme: {effectiveTheme}");

            // Validation: Ensure Application.Current is available
            if (Application.Current == null)
            {
                _logger?.LogError("Application.Current is null - cannot apply theme");
                throw new InvalidOperationException("Application.Current is null. Theme can only be applied after Application initialization.");
            }

            // Remove existing theme ResourceDictionaries
            RemoveExistingTheme();

            // Get the appropriate theme URI
            var themeUri = effectiveTheme == Theme.Dark ? DarkThemeUri : LightThemeUri;
            _logger?.LogDebug($"Loading theme from: {themeUri}");

            // Load and merge the new theme ResourceDictionary
            var themeResourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Add(themeResourceDictionary);
            _logger?.LogInformation($"Theme applied successfully: {theme} (effective: {effectiveTheme})");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to apply theme: {theme}");
            throw;
        }
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
            _logger?.LogDebug("Not running on Windows, defaulting to Light theme");
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
                    var detectedTheme = intValue == 0 ? Theme.Dark : Theme.Light;
                    _logger?.LogInformation($"Windows system theme detected: {detectedTheme} (registry value: {intValue})");
                    return detectedTheme;
                }
            }

            _logger?.LogWarning("Could not read AppsUseLightTheme registry value, defaulting to Light");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to detect system theme from registry, defaulting to Light");
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

        _logger?.LogDebug($"Removing {themesToRemove.Count} existing theme dictionary/dictionaries");

        foreach (var theme in themesToRemove)
        {
            _logger?.LogTrace($"Removing theme dictionary: {theme.Source?.OriginalString}");
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
            _logger?.LogInformation("Windows theme changed, reapplying System theme");

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
            _logger?.LogDebug("Unsubscribed from Windows theme changes");
        }

        _logger?.LogInformation("ThemeService disposed");
    }
}
