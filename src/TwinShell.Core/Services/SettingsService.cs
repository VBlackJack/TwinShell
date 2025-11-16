using System.Text.Json;
using System.Text.Json.Serialization;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing user settings and preferences.
/// Settings are persisted in JSON format at %APPDATA%/TwinShell/settings.json
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private UserSettings _currentSettings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsService()
    {
        // Get AppData directory: %APPDATA%/TwinShell
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TwinShell");

        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
        _currentSettings = UserSettings.Default;
    }

    /// <inheritdoc/>
    public UserSettings CurrentSettings => _currentSettings;

    /// <inheritdoc/>
    public async Task<UserSettings> LoadSettingsAsync()
    {
        try
        {
            // Ensure directory exists
            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
            }

            // Check if settings file exists
            if (!File.Exists(_settingsFilePath))
            {
                // Create default settings file
                _currentSettings = UserSettings.Default;
                await SaveSettingsAsync(_currentSettings);
                return _currentSettings;
            }

            // Read and deserialize settings
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);

            if (settings == null || !ValidateSettings(settings))
            {
                // If deserialization fails or validation fails, use defaults
                _currentSettings = UserSettings.Default;
                await SaveSettingsAsync(_currentSettings);
                return _currentSettings;
            }

            _currentSettings = settings;
            return _currentSettings;
        }
        catch (Exception)
        {
            // On any error, return default settings
            _currentSettings = UserSettings.Default;
            return _currentSettings;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SaveSettingsAsync(UserSettings settings)
    {
        try
        {
            // Validate before saving
            if (!ValidateSettings(settings))
            {
                return false;
            }

            // Ensure directory exists
            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
            }

            // Serialize and write to file
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            _currentSettings = settings;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<UserSettings> ResetToDefaultAsync()
    {
        _currentSettings = UserSettings.Default;
        await SaveSettingsAsync(_currentSettings);
        return _currentSettings;
    }

    /// <inheritdoc/>
    public string GetSettingsFilePath()
    {
        return _settingsFilePath;
    }

    /// <inheritdoc/>
    public bool ValidateSettings(UserSettings settings)
    {
        if (settings == null)
            return false;

        // Validate AutoCleanupDays (minimum 1 day, maximum 3650 days / 10 years)
        if (settings.AutoCleanupDays < 1 || settings.AutoCleanupDays > 3650)
            return false;

        // Validate MaxHistoryItems (minimum 10, maximum 100000)
        if (settings.MaxHistoryItems < 10 || settings.MaxHistoryItems > 100000)
            return false;

        // Validate RecentCommandsCount (minimum 1, maximum 50)
        if (settings.RecentCommandsCount < 1 || settings.RecentCommandsCount > 50)
            return false;

        return true;
    }
}
