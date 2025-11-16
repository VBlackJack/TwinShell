using System.Globalization;
using System.Resources;
using TwinShell.Core.Interfaces;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing application localization
/// </summary>
public class LocalizationService : ILocalizationService
{
    private CultureInfo _currentCulture;
    private readonly ResourceManager _resourceManager;

    public LocalizationService()
    {
        // Default to French
        _currentCulture = new CultureInfo("fr");

        // Note: Resource manager will be configured when resources are added
        // For now, using a placeholder
        _resourceManager = new ResourceManager("TwinShell.App.Resources.Strings", typeof(LocalizationService).Assembly);
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public CultureInfo[] SupportedCultures => new[]
    {
        new CultureInfo("fr"),  // French (default)
        new CultureInfo("en"),  // English
        new CultureInfo("es")   // Spanish
    };

    public event EventHandler? LanguageChanged;

    public void ChangeLanguage(CultureInfo culture)
    {
        if (culture == null)
            throw new ArgumentNullException(nameof(culture));

        if (!SupportedCultures.Any(c => c.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName))
            throw new NotSupportedException($"Culture '{culture.Name}' is not supported");

        _currentCulture = culture;

        // Update thread culture
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Update current culture for future threads
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ChangeLanguage(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            throw new ArgumentException("Culture code cannot be null or empty", nameof(cultureCode));

        var culture = new CultureInfo(cultureCode);
        ChangeLanguage(culture);
    }

    public string GetString(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key; // Return key if not found
        }
        catch
        {
            return key;
        }
    }

    public string GetString(string key, string fallback)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
