using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SettingsService>? _logger;
    private UserSettings _currentSettings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsService(ILogger<SettingsService>? logger = null)
    {
        _logger = logger;

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

            // SECURITY: Read and decrypt settings
            var encrypted = await File.ReadAllBytesAsync(_settingsFilePath);
            string json;

            try
            {
                // Try to decrypt (new format)
                json = DecryptData(encrypted);
            }
            catch
            {
                // If decryption fails, assume it's an old unencrypted file
                // Read as plain text for backward compatibility
                json = Encoding.UTF8.GetString(encrypted);
            }

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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load settings");
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

            // SECURITY: Encrypt settings before saving
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var encrypted = EncryptData(json);
            await File.WriteAllBytesAsync(_settingsFilePath, encrypted);

            // SECURITY: Set restrictive file permissions (Windows only)
            if (OperatingSystem.IsWindows())
            {
                SetRestrictivePermissions(_settingsFilePath);
            }

            _currentSettings = settings;
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings");
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

    /// <summary>
    /// Encrypts data using user-specific key (DPAPI on Windows, AES with user-derived key on Linux)
    /// </summary>
    private byte[] EncryptData(string data)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);

        if (OperatingSystem.IsWindows())
        {
            // Use DPAPI on Windows (user-specific encryption)
            return ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
        }
        else
        {
            // Use AES with user-derived key on Linux/Mac
            using var aes = Aes.Create();
            aes.Key = DeriveUserKey();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var encrypted = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

            // Combine IV + ciphertext
            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return result;
        }
    }

    /// <summary>
    /// Decrypts data using user-specific key
    /// </summary>
    private string DecryptData(byte[] encryptedData)
    {
        if (OperatingSystem.IsWindows())
        {
            // Use DPAPI on Windows
            var decrypted = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        else
        {
            // Use AES on Linux/Mac
            using var aes = Aes.Create();
            aes.Key = DeriveUserKey();

            // Extract IV and ciphertext
            var iv = new byte[aes.IV.Length];
            var ciphertext = new byte[encryptedData.Length - aes.IV.Length];

            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, iv.Length, ciphertext, 0, ciphertext.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            var decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            return Encoding.UTF8.GetString(decrypted);
        }
    }

    /// <summary>
    /// Derives a user-specific encryption key
    /// </summary>
    private byte[] DeriveUserKey()
    {
        // Use username + machine name as entropy for key derivation
        var entropy = Environment.UserName + Environment.MachineName;
        var salt = Encoding.UTF8.GetBytes("TwinShell.Settings.v1");

        using var pbkdf2 = new Rfc2898DeriveBytes(
            entropy,
            salt,
            iterations: 10000,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32); // 256-bit key
    }

    /// <summary>
    /// Sets restrictive file permissions (Windows only)
    /// </summary>
    private void SetRestrictivePermissions(string filePath)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return;

            var fileInfo = new FileInfo(filePath);
            var fileSecurity = fileInfo.GetAccessControl();

            // Remove inherited permissions
            fileSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Clear all existing rules
            var existingRules = fileSecurity.GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount));
            foreach (System.Security.AccessControl.FileSystemAccessRule rule in existingRules)
            {
                fileSecurity.RemoveAccessRule(rule);
            }

            // Add permission only for current user
            var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().User;
            if (currentUser != null)
            {
                var accessRule = new System.Security.AccessControl.FileSystemAccessRule(
                    currentUser,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.AccessControlType.Allow);

                fileSecurity.AddAccessRule(accessRule);
            }

            fileInfo.SetAccessControl(fileSecurity);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to set restrictive permissions on settings file");
        }
    }
}
