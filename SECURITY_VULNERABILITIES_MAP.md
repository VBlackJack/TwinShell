# CARTE DES VULNÉRABILITÉS - Localisation Précise

## Vue d'ensemble

14 vulnérabilités identifiées dans 7 fichiers principaux.

---

## CRITICAL - 3 Vulnérabilités

### 1. Injection de Commandes via CommandGeneratorService
**Fichier:** `/home/user/TwinShell/src/TwinShell.Core/Services/CommandGeneratorService.cs`  
**Lignes:** 12-27  
**Méthode:** `GenerateCommand(CommandTemplate, Dictionary<string, string>)`  
**Gravité:** CRITICAL  
**CWE:** CWE-78 (OS Command Injection)

**Code vulnérable:**
```csharp
command = command.Replace(placeholder, value);  // Ligne 24
```

**Vecteur d'attaque:**
```
Template: "Get-ChildItem {path}"
Input: "C:\temp && Del C:\Windows"
Résultat: "Get-ChildItem C:\temp && Del C:\Windows"
```

---

### 2. Escaping PowerShell Insuffisant
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/CommandExecutionService.cs`  
**Lignes:** 169-173  
**Méthode:** `EscapeForPowerShell(string)`  
**Gravité:** CRITICAL  
**CWE:** CWE-78 (OS Command Injection)

**Code vulnérable:**
```csharp
private string EscapeForPowerShell(string command)
{
    return command.Replace("\"", "\"\"");  // Ligne 172 - INCOMPLET
}
```

**Vecteur d'attaque:**
```
Input: `whoami`  → Exécute whoami
Input: $PSVersionTable  → Accès aux variables
```

---

### 3. Path Traversal - ExportToJsonAsync
**Fichier:** `/home/user/TwinShell/src/TwinShell.Core/Services/ConfigurationService.cs`  
**Lignes:** 33-95  
**Méthode:** `ExportToJsonAsync(string, string?, bool)`  
**Gravité:** CRITICAL  
**CWE:** CWE-22 (Path Traversal / Directory Traversal)

**Code vulnérable:**
```csharp
var directory = Path.GetDirectoryName(filePath);  // Ligne 80 - PAS DE VALIDATION
if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);  // Ligne 82
}

await File.WriteAllTextAsync(filePath, json);  // Ligne 87 - ÉCRIT N'IMPORTE OÙ
```

**Vecteur d'attaque:**
```
filePath: "..\\..\\..\\Windows\\System32\\config\\bad.json"
Résultat: Écrasement de fichiers système
```

---

## HIGH - 6 Vulnérabilités

### 4. Escaping Bash Incomplet
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/CommandExecutionService.cs`  
**Lignes:** 178-182  
**Méthode:** `EscapeForbash(string)`  
**Gravité:** HIGH  
**CWE:** CWE-78

**Code vulnérable:**
```csharp
private string EscapeForBash(string command)
{
    return command.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");
    // Ligne 181 - Pas de gestion de ;, |, &
}
```

---

### 5. Validation d'Entrée Insuffisante - Types
**Fichier:** `/home/user/TwinShell/src/TwinShell.Core/Services/CommandGeneratorService.cs`  
**Lignes:** 45-91  
**Méthode:** `ValidateParameters(CommandTemplate, Dictionary<string, string>, out List<string>)`  
**Gravité:** HIGH  
**CWE:** CWE-20 (Improper Input Validation)

**Code vulnérable:**
```csharp
switch (parameter.Type.ToLowerInvariant())
{
    case "int":
    case "integer":
        // Validé ligne 73-76
        break;
    case "bool":
    case "boolean":
        // Validé ligne 80-84
        break;
    // CAS "string" MANQUANT - JAMAIS VALIDÉ
}
```

---

### 6. Stack Trace Exposée
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/CommandExecutionService.cs`  
**Lignes:** 133-141  
**Méthode:** `ExecuteAsync(...) catch block`  
**Gravité:** HIGH  
**CWE:** CWE-209 (Information Exposure Through an Error Message)

**Code vulnérable:**
```csharp
catch (Exception ex)
{
    result.ErrorMessage = $"Failed to execute command: {ex.Message}";
    result.Stderr = ex.ToString();  // Ligne 140 - STACK TRACE COMPLÈTE
}
```

---

### 7. userId Non Validé
**Fichier:** `/home/user/TwinShell/src/TwinShell.Core/Services/ConfigurationService.cs`  
**Lignes:** 33-95  
**Méthode:** `ExportToJsonAsync(string, string?, bool)`  
**Gravité:** HIGH  
**CWE:** CWE-639 (Authorization Bypass Through User-Controlled Key)

**Code vulnérable:**
```csharp
public async Task<(bool Success, string? ErrorMessage)> ExportToJsonAsync(
    string filePath,
    string? userId = null,  // Ligne 35 - PAS DE VALIDATION
    bool includeHistory = true)
{
    var config = new UserConfigurationDto
    {
        UserId = userId  // Ligne 44 - ACCEPTÉ TEL QUEL
    };
}
```

---

### 8. Module Name Mal Échappé - PowerShellGalleryService
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/PowerShellGalleryService.cs`  
**Lignes:** 304-306  
**Méthode:** `EscapeForPowerShell(string)`  
**Gravité:** HIGH  
**CWE:** CWE-78

**Code vulnérable:**
```csharp
private static string EscapeForPowerShell(string input)
{
    return input.Replace("'", "''");  // Ligne 306 - TRÈS BASIQUE
}
```

**Utilisation vulnérable:**
```csharp
var command = $@"
    Find-Module -Name '*{EscapeForPowerShell(query)}*' -ErrorAction SilentlyContinue
";  // Lignes 27-31
```

---

### 9. Données Sensibles Non Chiffrées
**Fichier:** `/home/user/TwinShell/src/TwinShell.Core/Services/SettingsService.cs`  
**Lignes:** 27-32  
**Constructeur:** `SettingsService()`  
**Gravité:** HIGH  
**CWE:** CWE-312 (Cleartext Storage of Sensitive Information)

**Code vulnérable:**
```csharp
_settingsDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "TwinShell");  // Ligne 30 - ACCESSIBLE À TOUS

_settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");  // Ligne 32
```

**Lors de la sauvegarde (Ligne 101):**
```csharp
await File.WriteAllTextAsync(_settingsFilePath, json);  // EN TEXTE CLAIR
```

---

## MEDIUM - 4 Vulnérabilités

### 10. JSON Seed File Non Validé
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/JsonSeedService.cs`  
**Lignes:** 36-40  
**Méthode:** `SeedAsync()`  
**Gravité:** MEDIUM  
**CWE:** CWE-502 (Deserialization of Untrusted Data)

**Code vulnérable:**
```csharp
var json = await File.ReadAllTextAsync(_seedFilePath);  // Ligne 36
var seedData = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true  // Ligne 39 - PAS DE VALIDATION
});

foreach (var action in seedData.Actions)
{
    await _actionRepository.AddAsync(action);  // Ligne 53 - DONNÉES NON VALIDÉES
}
```

---

### 11. Import JSON Sans Intégrité
**Fichier:** `/home/user/TwinShell/src/TwinShell.Core/Services/ConfigurationService.cs`  
**Lignes:** 97-124  
**Méthode:** `ImportFromJsonAsync(string, string?, bool)`  
**Gravité:** MEDIUM  
**CWE:** CWE-494 (Download of Code Without Integrity Check)

**Code vulnérable:**
```csharp
var json = await File.ReadAllTextAsync(filePath);  // Ligne 111
var config = JsonSerializer.Deserialize<UserConfigurationDto>(json, JsonOptions);
// Ligne 112 - PAS DE VÉRIFICATION D'INTÉGRITÉ
```

---

### 12. Processus Enfants Non Nettoyés
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/PackageManagerService.cs`  
**Lignes:** 118-122  
**Méthode:** `ExecuteCommandAsync(string, string, int)`  
**Gravité:** MEDIUM  
**CWE:** CWE-404 (Improper Resource Validation)

**Code vulnérable:**
```csharp
if (!process.WaitForExit((int)timeout.TotalMilliseconds))
{
    process.Kill();  // Ligne 120 - PAS DE Kill(entireProcessTree: true)
    throw new TimeoutException(...);
}
```

**Comparaison correct:**
```csharp
process.Kill(entireProcessTree: true);  // CommandExecutionService ligne 102
```

---

### 13. URIs Non Validées - PowerShell Gallery
**Fichier:** `/home/user/TwinShell/src/TwinShell.Infrastructure/Services/PowerShellGalleryService.cs`  
**Lignes:** 309-324  
**Méthode:** `MapToModule(PowerShellGalleryModuleDto)`  
**Gravité:** MEDIUM  
**CWE:** CWE-601 (URL Redirection to Untrusted Site)

**Code vulnérable:**
```csharp
private static PowerShellModule MapToModule(PowerShellGalleryModuleDto dto)
{
    return new PowerShellModule
    {
        ProjectUri = dto.ProjectUri,  // Ligne 321 - PAS DE VALIDATION
        LicenseUri = dto.LicenseUri   // Ligne 322 - PAS DE VALIDATION
    };
}
```

---

## LOW - 1 Vulnérabilité

### 14. Exceptions Converties en Messages UI
**Fichier:** `/home/user/TwinShell/src/TwinShell.App/ViewModels/BatchViewModel.cs`  
**Lignes:** 67-70, 142  
**Méthode:** `LoadBatchesAsync()`, `ExecuteBatchAsync()`  
**Gravité:** LOW  
**CWE:** CWE-209

**Code vulnérable:**
```csharp
catch (Exception ex)
{
    _notificationService.ShowError($"Failed to load batches: {ex.Message}");  // Ligne 69
}

// Et ligne 142:
_notificationService.ShowError($"Failed to execute batch: {ex.Message}");
```

---

## Résumé par Fichier

### CommandGeneratorService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 1 | 24 | CRITICAL | Injection |
| 5 | 45-91 | HIGH | Validation insuffisante |

### CommandExecutionService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 2 | 172 | CRITICAL | Escaping insuffisant |
| 4 | 181 | HIGH | Escaping incomplet |
| 6 | 140 | HIGH | Information disclosure |

### ConfigurationService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 3 | 87 | CRITICAL | Path Traversal |
| 7 | 44 | HIGH | Authorization |
| 11 | 112 | MEDIUM | Deserialization |

### PowerShellGalleryService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 8 | 306 | HIGH | Escaping |
| 12 | 321-322 | MEDIUM | URI Validation |

### SettingsService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 9 | 101 | HIGH | Encryption |

### JsonSeedService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 10 | 39 | MEDIUM | Deserialization |

### PackageManagerService.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 13 | 120 | MEDIUM | Resource handling |

### BatchViewModel.cs
| ID | Ligne | Gravité | Type |
|-------|-------|---------|------|
| 14 | 69, 142 | LOW | Error disclosure |

---

## Matrice de Correction

| ID | Fichier | Ligne(s) | Effort | Durée |
|----|---------|----------|--------|-------|
| 1 | CommandGeneratorService | 24 | HAUT | 2-3 h |
| 2 | CommandExecutionService | 172 | HAUT | 1-2 h |
| 3 | ConfigurationService | 87 | MOYEN | 1-2 h |
| 4 | CommandExecutionService | 181 | MOYEN | 1 h |
| 5 | CommandGeneratorService | 45-91 | MOYEN | 1-2 h |
| 6 | CommandExecutionService | 140 | BAS | 0.5 h |
| 7 | ConfigurationService | 44 | MOYEN | 1-2 h |
| 8 | PowerShellGalleryService | 306 | BAS | 0.5 h |
| 9 | SettingsService | 101 | HAUT | 3-4 h |
| 10 | JsonSeedService | 39 | MOYEN | 1-2 h |
| 11 | ConfigurationService | 112 | MOYEN | 1-2 h |
| 12 | PowerShellGalleryService | 321-322 | MOYEN | 1-2 h |
| 13 | PackageManagerService | 120 | BAS | 0.5 h |
| 14 | BatchViewModel | 69, 142 | BAS | 0.5 h |

---

**Durée Totale Estimée:** 17-25 heures (Phase 1: 8-10 h, Phase 2: 9-15 h)

