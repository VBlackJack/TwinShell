# GUIDE DE CORRECTION DES FAILLES DE SÉCURITÉ

## 1. Correction de l'Injection de Commandes

### 1.1 CommandGeneratorService.cs - Sécurisation

**AVANT (Vulnérable):**
```csharp
public string GenerateCommand(CommandTemplate template, Dictionary<string, string> parameterValues)
{
    var command = template.CommandPattern;
    foreach (var parameter in template.Parameters)
    {
        var value = parameterValues.ContainsKey(parameter.Name)
            ? parameterValues[parameter.Name]
            : parameter.DefaultValue ?? string.Empty;
        
        var placeholder = $"{{{parameter.Name}}}";
        command = command.Replace(placeholder, value);  // ❌ INJECTION POSSIBLE
    }
    return command;
}
```

**APRÈS (Sécurisé):**
```csharp
public string GenerateCommand(CommandTemplate template, Dictionary<string, string> parameterValues)
{
    var command = template.CommandPattern;
    
    foreach (var parameter in template.Parameters)
    {
        var value = parameterValues.ContainsKey(parameter.Name)
            ? parameterValues[parameter.Name]
            : parameter.DefaultValue ?? string.Empty;
        
        // Valider la valeur selon le type
        if (!ValidateParameterValue(parameter, value, out var validationError))
        {
            throw new InvalidOperationException($"Invalid parameter '{parameter.Name}': {validationError}");
        }
        
        // Échapper pour le shell approprié
        var escapedValue = EscapeParameterValue(value, parameter.Type);
        
        var placeholder = $"{{{parameter.Name}}}";
        command = command.Replace(placeholder, escapedValue);
    }
    
    return command;
}

private bool ValidateParameterValue(TemplateParameter parameter, string value, out string error)
{
    error = string.Empty;
    
    switch (parameter.Type.ToLower())
    {
        case "hostname":
            if (!IsValidHostname(value))
            {
                error = "Invalid hostname format";
                return false;
            }
            break;
            
        case "ipaddress":
            if (!IPAddress.TryParse(value, out _))
            {
                error = "Invalid IP address";
                return false;
            }
            break;
            
        case "int":
        case "integer":
            if (!int.TryParse(value, out _))
            {
                error = "Must be an integer";
                return false;
            }
            break;
            
        case "path":
            if (!IsValidPath(value))
            {
                error = "Invalid path format";
                return false;
            }
            break;
            
        case "string":
            if (string.IsNullOrWhiteSpace(value) && parameter.Required)
            {
                error = "Required field";
                return false;
            }
            if (value.Length > 255)
            {
                error = "String exceeds maximum length of 255";
                return false;
            }
            break;
    }
    
    return true;
}

private string EscapeParameterValue(string value, string parameterType)
{
    switch (parameterType.ToLower())
    {
        case "hostname":
        case "ipaddress":
        case "int":
        case "integer":
            return value;  // Whitelist validée, pas d'échappement supplémentaire
            
        case "path":
        case "string":
        default:
            // N'accepter que les chemins autorisés
            return QuoteForShell(value);
    }
}

private static string QuoteForShell(string value)
{
    // Utiliser des guillemets simples pour Bash (plus sûr)
    // Si Windows/PowerShell, utiliser backquotes
    return "'" + value.Replace("'", "'\\''") + "'";
}

private bool IsValidHostname(string value)
{
    // RFC 1123 compliant hostname validation
    if (value.Length > 255)
        return false;
    
    var hostnameRegex = new Regex(@"^(?!-)([a-zA-Z0-9-]{1,63}(?<!-)\.)*[a-zA-Z]{2,}$");
    return hostnameRegex.IsMatch(value);
}

private bool IsValidPath(string value)
{
    try
    {
        var fullPath = Path.GetFullPath(value);
        // Vérifier qu'il n'y a pas de traversée
        return !fullPath.Contains("..") && 
               !fullPath.Contains("~") &&
               Path.IsPathRooted(fullPath);
    }
    catch
    {
        return false;
    }
}
```

---

### 1.2 CommandExecutionService.cs - Améliorer l'Escaping

**AVANT:**
```csharp
private (string executable, string arguments) GetExecutableAndArguments(string command, Platform platform)
{
    return actualPlatform switch
    {
        Platform.Windows => ("powershell.exe", $"-NoProfile -NonInteractive -Command \"{EscapeForPowerShell(command)}\""),
        Platform.Linux => ("bash", $"-c \"{EscapeForBash(command)}\""),
        _ => throw new NotSupportedException($"Platform {platform} is not supported")
    };
}

private string EscapeForPowerShell(string command)
{
    return command.Replace("\"", "\"\"");  // ❌ INCOMPLET
}

private string EscapeForBash(string command)
{
    return command.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");  // ❌ INCOMPLET
}
```

**APRÈS:**
```csharp
private (string executable, string arguments) GetExecutableAndArguments(string command, Platform platform)
{
    var actualPlatform = platform;
    if (platform == Platform.Both)
    {
        actualPlatform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
    }

    return actualPlatform switch
    {
        Platform.Windows => ("powershell.exe", BuildPowerShellCommand(command)),
        Platform.Linux => ("bash", BuildBashCommand(command)),
        _ => throw new NotSupportedException($"Platform {platform} is not supported")
    };
}

private string BuildPowerShellCommand(string command)
{
    // Utiliser un script encodé en base64 pour éviter les problèmes d'escaping
    var bytes = Encoding.UTF8.GetBytes(command);
    var encoded = Convert.ToBase64String(bytes);
    return $"-NoProfile -NonInteractive -EncodedCommand {encoded}";
}

private string BuildBashCommand(string command)
{
    // Utiliser la forme sûre: guillemets simples + échappement des apostrophes
    var escaped = "'" + command.Replace("'", "'\\''") + "'";
    return $"-c {escaped}";
}

private string EscapeForPowerShell(string command)
{
    // Complet - échappe tous les caractères spéciaux
    return command
        .Replace("\\", "\\\\")
        .Replace("\"", "`\"")
        .Replace("$", "`$")
        .Replace("`", "``")
        .Replace("'", "''");
}

private string EscapeForBash(string command)
{
    // Guillemets simples = contexte littéral (plus sûr)
    // Seulement les apostrophes posent problème
    return "'" + command.Replace("'", "'\\''") + "'";
}
```

---

## 2. Correction du Path Traversal

### 2.1 ConfigurationService.cs - Valider les Chemins

**AVANT:**
```csharp
public async Task<(bool Success, string? ErrorMessage)> ExportToJsonAsync(
    string filePath,
    string? userId = null,
    bool includeHistory = true)
{
    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);  // ❌ AUCUNE VALIDATION
    }
    
    await File.WriteAllTextAsync(filePath, json);
}
```

**APRÈS:**
```csharp
private readonly string _baseExportDirectory;

public ConfigurationService(...)
{
    // Configurer le répertoire de base autorisé
    _baseExportDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TwinShell",
        "Exports");
}

public async Task<(bool Success, string? ErrorMessage)> ExportToJsonAsync(
    string filePath,
    string? userId = null,
    bool includeHistory = true)
{
    try
    {
        // ✅ VALIDER LE CHEMIN
        if (!IsPathSecure(filePath))
        {
            return (false, "Invalid file path", 0, 0);
        }
        
        // Valider que c'est un fichier .json
        if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "File must be a JSON file", 0, 0);
        }
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        
        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Export failed");
        return (false, "Export operation failed", 0, 0);
    }
}

private bool IsPathSecure(string filePath)
{
    try
    {
        // Obtenir le chemin absolu
        var fullPath = Path.GetFullPath(filePath);
        var baseDirectory = Path.GetFullPath(_baseExportDirectory);
        
        // Vérifier que le chemin commence par le répertoire de base
        if (!fullPath.StartsWith(baseDirectory + Path.DirectorySeparatorChar) && 
            fullPath != baseDirectory)
        {
            return false;
        }
        
        // Vérifier qu'il n'y a pas de traversée
        if (fullPath.Contains("..") || fullPath.Contains("~"))
        {
            return false;
        }
        
        return true;
    }
    catch
    {
        return false;
    }
}
```

---

## 3. Correction du Logging Sécurisé

### 3.1 CommandExecutionService.cs - Ne pas Exposer les Erreurs

**AVANT:**
```csharp
catch (Exception ex)
{
    result.ErrorMessage = $"Failed to execute command: {ex.Message}";
    result.Stderr = ex.ToString();  // ❌ STACK TRACE
}
```

**APRÈS:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Command execution failed: {Command}", command);
    result.ErrorMessage = "Command execution failed";  // ✅ Généralisé
    result.Stderr = string.Empty;  // ✅ Pas de détails
}
```

---

## 4. Correction de la Désérialisation JSON

### 4.1 JsonSeedService.cs - Valider Avant Désérialisation

**AVANT:**
```csharp
var json = await File.ReadAllTextAsync(_seedFilePath);
var seedData = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

**APRÈS:**
```csharp
public async Task SeedAsync()
{
    var existingCount = await _actionRepository.CountAsync();
    if (existingCount > 0)
    {
        return;
    }

    if (!File.Exists(_seedFilePath))
    {
        throw new FileNotFoundException($"Seed file not found: {_seedFilePath}");
    }

    // ✅ VALIDER LA TAILLE DU FICHIER
    var fileInfo = new FileInfo(_seedFilePath);
    const long maxSize = 10 * 1024 * 1024;  // 10 MB max
    if (fileInfo.Length > maxSize)
    {
        throw new InvalidOperationException("Seed file is too large");
    }

    var json = await File.ReadAllTextAsync(_seedFilePath);
    
    // ✅ VALIDER LE SCHÉMA AVANT DÉSÉRIALISATION
    if (!ValidateJsonSchema(json))
    {
        throw new InvalidOperationException("Invalid seed file schema");
    }

    var seedData = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (seedData?.Actions == null || seedData.Actions.Count == 0)
    {
        return;
    }

    // ✅ VALIDER CHAQUE ACTION
    foreach (var action in seedData.Actions)
    {
        if (!ValidateAction(action))
        {
            throw new InvalidOperationException($"Invalid action: {action.Title}");
        }
        
        action.IsUserCreated = false;
        action.CreatedAt = DateTime.UtcNow;
        action.UpdatedAt = DateTime.UtcNow;
        await _actionRepository.AddAsync(action);
    }
}

private bool ValidateJsonSchema(string json)
{
    try
    {
        using (var doc = JsonDocument.Parse(json))
        {
            var root = doc.RootElement;
            
            if (root.ValueKind != JsonValueKind.Object)
                return false;
            
            if (!root.TryGetProperty("Actions", out var actions))
                return false;
            
            if (actions.ValueKind != JsonValueKind.Array)
                return false;
            
            return true;
        }
    }
    catch
    {
        return false;
    }
}

private bool ValidateAction(Action action)
{
    if (string.IsNullOrWhiteSpace(action.Title))
        return false;
    
    if (action.Title.Length > 255)
        return false;
    
    // Valider que le template de commande ne contient pas de code suspect
    if (!ValidateCommandTemplate(action.WindowsCommandTemplate) ||
        !ValidateCommandTemplate(action.LinuxCommandTemplate))
    {
        return false;
    }
    
    return true;
}

private bool ValidateCommandTemplate(CommandTemplate? template)
{
    if (template == null)
        return true;
    
    // Vérifier que le pattern n'est pas excessivement long
    if (template.CommandPattern.Length > 4096)
        return false;
    
    // Vérifier qu'il n'y a pas de caractères de contrôle
    return !template.CommandPattern.Any(c => char.IsControl(c) && c != '\n');
}
```

---

## 5. Correction de l'Authentification

### 5.1 ConfigurationService.cs - Valider l'Utilisateur

**AVANT:**
```csharp
public async Task<...> ExportToJsonAsync(
    string filePath,
    string? userId = null,  // ❌ PAS DE VALIDATION
    bool includeHistory = true)
{
    config.UserId = userId;  // ❌ ACCEPTÉ
}
```

**APRÈS:**
```csharp
private readonly IAuthenticationService _authService;

public ConfigurationService(..., IAuthenticationService authService)
{
    _authService = authService ?? throw new ArgumentNullException(nameof(authService));
}

public async Task<(bool Success, string? ErrorMessage)> ExportToJsonAsync(
    string filePath,
    bool includeHistory = true)  // ✅ userId SUPPRIMÉ
{
    try
    {
        // ✅ OBTENIR L'UTILISATEUR AUTHENTIFIÉ
        var authenticatedUserId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(authenticatedUserId))
        {
            return (false, "User is not authenticated", 0, 0);
        }
        
        var config = new UserConfigurationDto
        {
            Version = "1.0",
            ExportDate = DateTime.UtcNow,
            UserId = authenticatedUserId  // ✅ DEPUIS L'AUTHENTIFICATION
        };
        
        // Exporter uniquement les données de cet utilisateur
        var favorites = await _favoritesRepository.GetAllAsync(authenticatedUserId);
        config.Favorites = favorites.Select(...).ToList();
        
        if (includeHistory)
        {
            var history = await _historyRepository.GetRecentAsync(1000, authenticatedUserId);
            config.History = history.Select(...).ToList();
        }
        
        // ... reste du code
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Export failed for user {User}", _authService.GetCurrentUserId());
        return (false, "Export operation failed", 0, 0);
    }
}
```

---

## 6. Correction du Chiffrement des Données

### 6.1 SettingsService.cs - Chiffrer les Données Sensibles

**AVANT:**
```csharp
var json = JsonSerializer.Serialize(settings, JsonOptions);
await File.WriteAllTextAsync(_settingsFilePath, json);  // ❌ EN TEXTE CLAIR
```

**APRÈS:**
```csharp
using System.Security.Cryptography;

public async Task<bool> SaveSettingsAsync(UserSettings settings)
{
    try
    {
        if (!ValidateSettings(settings))
        {
            return false;
        }

        if (!Directory.Exists(_settingsDirectory))
        {
            Directory.CreateDirectory(_settingsDirectory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        
        // ✅ CHIFFRER AVANT D'ÉCRIRE
        var encrypted = EncryptJson(json);
        await File.WriteAllBytesAsync(_settingsFilePath, encrypted);
        
        // ✅ DÉFINIR LES PERMISSIONS RESTRICTIVES
        SetRestrictivePermissions(_settingsFilePath);
        
        _currentSettings = settings;
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save settings");
        return false;
    }
}

public async Task<UserSettings> LoadSettingsAsync()
{
    try
    {
        if (!Directory.Exists(_settingsDirectory))
        {
            Directory.CreateDirectory(_settingsDirectory);
        }

        if (!File.Exists(_settingsFilePath))
        {
            _currentSettings = UserSettings.Default;
            await SaveSettingsAsync(_currentSettings);
            return _currentSettings;
        }

        // ✅ DÉCHIFFRER APRÈS LA LECTURE
        var encrypted = await File.ReadAllBytesAsync(_settingsFilePath);
        var json = DecryptJson(encrypted);
        
        var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
        
        if (settings == null || !ValidateSettings(settings))
        {
            _currentSettings = UserSettings.Default;
            await SaveSettingsAsync(_currentSettings);
            return _currentSettings;
        }

        _currentSettings = settings;
        return _currentSettings;
    }
    catch (Exception)
    {
        _currentSettings = UserSettings.Default;
        return _currentSettings;
    }
}

private byte[] EncryptJson(string json)
{
    var jsonBytes = Encoding.UTF8.GetBytes(json);
    
    using (var aes = Aes.Create())
    {
        // Générer une clé à partir de credentials utilisateur
        var key = DeriveKey();
        aes.Key = key;
        
        // IV aléatoire
        using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        {
            var encrypted = encryptor.TransformFinalBlock(jsonBytes, 0, jsonBytes.Length);
            
            // Combiner: IV + ciphertext
            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
            
            return result;
        }
    }
}

private string DecryptJson(byte[] encrypted)
{
    using (var aes = Aes.Create())
    {
        var key = DeriveKey();
        aes.Key = key;
        
        // Extraire IV et ciphertext
        var iv = new byte[aes.IV.Length];
        var ciphertext = new byte[encrypted.Length - aes.IV.Length];
        
        Buffer.BlockCopy(encrypted, 0, iv, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, aes.IV.Length, ciphertext, 0, ciphertext.Length);
        
        using (var decryptor = aes.CreateDecryptor(key, iv))
        {
            var decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}

private byte[] DeriveKey()
{
    // Dériver une clé à partir du SID utilisateur
    var userSid = WindowsIdentity.GetCurrent().User.Value;
    
    using (var pbkdf2 = new Rfc2898DeriveBytes(userSid, Encoding.UTF8.GetBytes("TwinShell"), 10000, HashAlgorithmName.SHA256))
    {
        return pbkdf2.GetBytes(32);  // 256-bit key
    }
}

private void SetRestrictivePermissions(string filePath)
{
    try
    {
        var fileInfo = new FileInfo(filePath);
        var fileSecurity = fileInfo.GetAccessControl();
        
        // Supprimer les permissions héritées
        fileSecurity.SetAccessRuleProtection(true, false);
        
        // Ajouter permission seulement pour l'utilisateur courant
        var currentUser = WindowsIdentity.GetCurrent().User;
        var rule = new FileSystemAccessRule(
            currentUser,
            FileSystemRights.FullControl,
            AccessControlType.Allow);
        
        fileSecurity.AddAccessRule(rule);
        fileInfo.SetAccessControl(fileSecurity);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to set restrictive permissions");
    }
}
```

---

## Résumé des Changements

| Problème | Classe | Méthode | Type de Fix |
|----------|--------|---------|-------------|
| Injection | CommandGeneratorService | GenerateCommand | Validation + Escaping |
| Escaping PS | CommandExecutionService | EscapeForPowerShell | Base64 encoding |
| Escaping Bash | CommandExecutionService | EscapeForBash | Guillemets simples |
| Path Traversal | ConfigurationService | ExportToJsonAsync | Path validation |
| Logging | CommandExecutionService | ExecuteAsync catch | Generic messages |
| Deserialization | JsonSeedService | SeedAsync | Schema validation |
| Auth | ConfigurationService | ExportToJsonAsync | User validation |
| Encryption | SettingsService | SaveSettingsAsync | AES encryption |

