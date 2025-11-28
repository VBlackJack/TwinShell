# Module 3 : Conception de Modules PowerShell

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre la structure d'un module PowerShell
- :material-check: Créer un fichier module (`.psm1`) et son manifeste (`.psd1`)
- :material-check: Contrôler l'API publique avec `Export-ModuleMember`
- :material-check: Déployer et distribuer vos modules

---

## 1. Pourquoi Créer des Modules ?

### 1.1 Du Script au Module

```
┌─────────────────────────────────────────────────────────────────┐
│              ÉVOLUTION DE L'ORGANISATION DU CODE                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   NIVEAU 1 : Script Unique                                      │
│   ────────────────────────                                      │
│   MonScript.ps1                                                 │
│   • 500+ lignes de code                                         │
│   • Difficile à maintenir                                       │
│   • Impossible à réutiliser                                     │
│                                                                 │
│   NIVEAU 2 : Script avec Dot-Sourcing                           │
│   ───────────────────────────────────                           │
│   Main.ps1                                                      │
│   . .\Functions\Get-User.ps1                                    │
│   . .\Functions\Set-User.ps1                                    │
│   • Mieux organisé                                              │
│   • Dépendances manuelles                                       │
│   • Chemins fragiles                                            │
│                                                                 │
│   NIVEAU 3 : Module                                 ◄── CIBLE   │
│   ───────────────────                                           │
│   WorldlineTools/                                               │
│   ├── WorldlineTools.psd1  (Manifeste)                         │
│   ├── WorldlineTools.psm1  (Code)                              │
│   └── Public/Private/      (Organisation)                       │
│   • Auto-loading                                                │
│   • Versionné                                                   │
│   • Distribuable via PowerShell Gallery                         │
│   • Encapsulation (fonctions privées/publiques)                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Avantages des Modules

| Avantage | Description |
|----------|-------------|
| **Réutilisabilité** | `Import-Module` et c'est prêt |
| **Encapsulation** | Cacher les fonctions internes (helpers) |
| **Versioning** | Gestion des versions via le manifeste |
| **Dépendances** | Déclarer les modules requis |
| **Auto-discovery** | PowerShell trouve les modules automatiquement |
| **Distribution** | Publier sur PowerShell Gallery ou NuGet |

---

## 2. Structure d'un Module

### 2.1 Structure Minimale

```
WorldlineTools/
├── WorldlineTools.psd1    # Manifeste (métadonnées)
└── WorldlineTools.psm1    # Code du module
```

### 2.2 Structure Professionnelle

```
WorldlineTools/
├── WorldlineTools.psd1           # Manifeste
├── WorldlineTools.psm1           # Loader principal
├── Public/                       # Fonctions EXPORTÉES
│   ├── New-WorldlineUser.ps1
│   ├── Get-WorldlineUser.ps1
│   ├── Set-WorldlineUser.ps1
│   └── Remove-WorldlineUser.ps1
├── Private/                      # Fonctions INTERNES
│   ├── Write-Log.ps1
│   ├── Test-Prerequisites.ps1
│   └── ConvertTo-NormalizedName.ps1
├── Classes/                      # Classes PowerShell
│   └── WorldlineUser.ps1
├── Data/                         # Fichiers de données
│   └── config.json
├── Tests/                        # Tests Pester
│   ├── New-WorldlineUser.Tests.ps1
│   └── Module.Tests.ps1
├── docs/                         # Documentation
│   └── README.md
└── en-US/                        # Aide localisée
    └── about_WorldlineTools.help.txt
```

---

## 3. Le Fichier Module (.psm1)

### 3.1 Approche Simple (Tout dans un Fichier)

```powershell
# WorldlineTools.psm1

#region Private Functions

function Write-Log {
    # Fonction interne - ne sera PAS exportée
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Verbose "[$timestamp] [$Level] $Message"
}

function ConvertTo-NormalizedName {
    # Fonction interne
    param([string]$Name)
    $Name -replace "[^a-zA-Z]", "" | ForEach-Object { $_.ToLower() }
}

#endregion

#region Public Functions

function New-WorldlineUser {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$FirstName,

        [Parameter(Mandatory)]
        [string]$LastName
    )

    Write-Log "Création de l'utilisateur $FirstName $LastName"
    $normalizedName = ConvertTo-NormalizedName -Name "$FirstName$LastName"
    # ... reste du code
}

function Get-WorldlineUser {
    [CmdletBinding()]
    param([string]$Username)

    Write-Log "Recherche de $Username"
    # ... code
}

#endregion

# Exporter UNIQUEMENT les fonctions publiques
Export-ModuleMember -Function New-WorldlineUser, Get-WorldlineUser
```

### 3.2 Approche Modulaire (Fichiers Séparés)

```powershell
# WorldlineTools.psm1 - Loader

$ModuleRoot = $PSScriptRoot

# Charger les classes en premier (si nécessaire)
$classFiles = Get-ChildItem -Path "$ModuleRoot\Classes\*.ps1" -ErrorAction SilentlyContinue
foreach ($file in $classFiles) {
    try {
        . $file.FullName
        Write-Verbose "Classe chargée : $($file.Name)"
    }
    catch {
        Write-Error "Échec du chargement de la classe $($file.Name) : $_"
    }
}

# Charger les fonctions privées
$privateFiles = Get-ChildItem -Path "$ModuleRoot\Private\*.ps1" -ErrorAction SilentlyContinue
foreach ($file in $privateFiles) {
    try {
        . $file.FullName
        Write-Verbose "Fonction privée chargée : $($file.Name)"
    }
    catch {
        Write-Error "Échec du chargement de $($file.Name) : $_"
    }
}

# Charger les fonctions publiques
$publicFiles = Get-ChildItem -Path "$ModuleRoot\Public\*.ps1" -ErrorAction SilentlyContinue
foreach ($file in $publicFiles) {
    try {
        . $file.FullName
        Write-Verbose "Fonction publique chargée : $($file.Name)"
    }
    catch {
        Write-Error "Échec du chargement de $($file.Name) : $_"
    }
}

# Exporter uniquement les fonctions publiques
$publicFunctions = $publicFiles | ForEach-Object { $_.BaseName }
Export-ModuleMember -Function $publicFunctions
```

---

## 4. Le Manifeste (.psd1)

### 4.1 Générer un Manifeste

```powershell
# Créer un manifeste avec les paramètres de base
New-ModuleManifest -Path ".\WorldlineTools\WorldlineTools.psd1" `
    -RootModule "WorldlineTools.psm1" `
    -ModuleVersion "1.0.0" `
    -Author "ShellBook Automation Team" `
    -CompanyName "Worldline" `
    -Description "Outils d'administration Worldline pour Active Directory et Azure AD" `
    -PowerShellVersion "5.1" `
    -FunctionsToExport @('New-WorldlineUser', 'Get-WorldlineUser', 'Set-WorldlineUser', 'Remove-WorldlineUser') `
    -CmdletsToExport @() `
    -VariablesToExport @() `
    -AliasesToExport @() `
    -Tags @('ActiveDirectory', 'Worldline', 'UserManagement') `
    -ProjectUri "https://github.com/worldline/WorldlineTools" `
    -LicenseUri "https://github.com/worldline/WorldlineTools/blob/main/LICENSE"
```

### 4.2 Anatomie du Manifeste

```powershell
# WorldlineTools.psd1
@{
    # Identité du module
    RootModule        = 'WorldlineTools.psm1'
    ModuleVersion     = '1.2.0'
    GUID              = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'

    # Informations sur l'auteur
    Author            = 'ShellBook Automation Team'
    CompanyName       = 'Worldline'
    Copyright         = '(c) 2025 Worldline. Tous droits réservés.'
    Description       = 'Outils d administration Worldline pour Active Directory'

    # Prérequis
    PowerShellVersion = '5.1'
    # CLRVersion      = '4.0'
    # DotNetFrameworkVersion = '4.5'

    # Modules requis (dépendances)
    RequiredModules   = @(
        @{ ModuleName = 'ActiveDirectory'; ModuleVersion = '1.0.0.0' }
    )

    # Assemblies .NET requis
    # RequiredAssemblies = @('System.Web.dll')

    # Scripts à exécuter au chargement
    # ScriptsToProcess = @('.\Initialize.ps1')

    # Nested modules (sous-modules)
    # NestedModules = @('.\SubModule\SubModule.psm1')

    # Éléments à exporter (IMPORTANT pour la sécurité)
    FunctionsToExport = @(
        'New-WorldlineUser',
        'Get-WorldlineUser',
        'Set-WorldlineUser',
        'Remove-WorldlineUser',
        'Test-WorldlineConnection'
    )
    CmdletsToExport   = @()  # Pas de cmdlets compilés
    VariablesToExport = @()  # Ne pas exporter de variables
    AliasesToExport   = @()  # Pas d'alias

    # Métadonnées pour PowerShell Gallery
    PrivateData       = @{
        PSData = @{
            Tags         = @('ActiveDirectory', 'Worldline', 'UserManagement', 'Automation')
            LicenseUri   = 'https://github.com/worldline/WorldlineTools/blob/main/LICENSE'
            ProjectUri   = 'https://github.com/worldline/WorldlineTools'
            IconUri      = 'https://www.worldline.com/favicon.ico'
            ReleaseNotes = @'
## Version 1.2.0
- Ajout de Test-WorldlineConnection
- Correction du bug #42 sur New-WorldlineUser
- Amélioration des performances de Get-WorldlineUser
'@
        }
    }
}
```

### 4.3 Champs Importants

| Champ | Description | Impact |
|-------|-------------|--------|
| `RootModule` | Fichier .psm1 principal | Obligatoire |
| `ModuleVersion` | Version SemVer | Gestion des mises à jour |
| `FunctionsToExport` | Liste des fonctions publiques | **Sécurité** : masque les helpers |
| `RequiredModules` | Dépendances | Installation automatique |
| `PowerShellVersion` | Version PS minimale | Compatibilité |
| `PrivateData.PSData` | Métadonnées Gallery | Publication |

!!! danger "Sécurité : `FunctionsToExport`"
    Si vous mettez `'*'` dans `FunctionsToExport`, TOUTES vos fonctions seront visibles, y compris les fonctions privées/helpers.

    **Toujours** lister explicitement les fonctions à exporter.

---

## 5. Contrôle de l'API Publique

### 5.1 Export-ModuleMember

```powershell
# À la fin du fichier .psm1

# Méthode 1 : Liste explicite
Export-ModuleMember -Function @(
    'New-WorldlineUser',
    'Get-WorldlineUser',
    'Set-WorldlineUser',
    'Remove-WorldlineUser'
)

# Méthode 2 : Pattern (attention aux fonctions privées !)
# Export-ModuleMember -Function *-Worldline*

# Exporter aussi des alias
Export-ModuleMember -Function 'New-WorldlineUser' -Alias 'nwu'

# Exporter des variables (rare, à éviter généralement)
$ModuleConfig = @{ LogPath = "C:\Logs" }
Export-ModuleMember -Variable 'ModuleConfig'
```

### 5.2 Priorité d'Export

```
┌─────────────────────────────────────────────────────────────────┐
│                  PRIORITÉ DES EXPORTS                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. FunctionsToExport dans .psd1        ◄── PRIORITÉ HAUTE    │
│      Si défini, SEULES ces fonctions sont exportées             │
│                                                                 │
│   2. Export-ModuleMember dans .psm1      ◄── PRIORITÉ BASSE    │
│      Utilisé si FunctionsToExport = '*' ou non défini           │
│                                                                 │
│   Recommandation :                                              │
│   • Définir FunctionsToExport explicitement dans .psd1          │
│   • Utiliser aussi Export-ModuleMember pour la clarté           │
│   • Les deux doivent correspondre                               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. Emplacement et Auto-Loading

### 6.1 Chemins de Modules

!!! tip "Où Stocker vos Modules ?"
    PowerShell recherche automatiquement les modules dans les chemins définis par `$env:PSModulePath` :

    ```powershell
    # Afficher les chemins de recherche
    $env:PSModulePath -split ';'
    ```

    | Chemin | Portée | Usage |
    |--------|--------|-------|
    | `$HOME\Documents\PowerShell\Modules` | Utilisateur | Modules personnels |
    | `C:\Program Files\PowerShell\Modules` | Machine (tous users) | Modules partagés |
    | `C:\Windows\System32\WindowsPowerShell\v1.0\Modules` | Système | Modules Windows |

### 6.2 Installation Manuelle

```powershell
# Chemin utilisateur (Windows PowerShell 5.1)
$userModulePath = "$HOME\Documents\WindowsPowerShell\Modules"

# Chemin utilisateur (PowerShell 7+)
$userModulePath = "$HOME\Documents\PowerShell\Modules"

# Créer le dossier du module
$modulePath = Join-Path $userModulePath "WorldlineTools"
New-Item -ItemType Directory -Path $modulePath -Force

# Copier les fichiers
Copy-Item -Path ".\WorldlineTools\*" -Destination $modulePath -Recurse

# Vérifier
Get-Module -ListAvailable -Name WorldlineTools
```

### 6.3 Auto-Loading

PowerShell charge automatiquement un module lorsque vous appelez une de ses fonctions :

```powershell
# Le module est chargé automatiquement au premier appel
New-WorldlineUser -FirstName "Jean" -LastName "Dupont"

# Équivalent à :
Import-Module WorldlineTools
New-WorldlineUser -FirstName "Jean" -LastName "Dupont"
```

!!! info "Condition pour l'Auto-Loading"
    L'auto-loading fonctionne **uniquement** si :

    1. Le module est dans un chemin de `$env:PSModulePath`
    2. Le nom du dossier = nom du module = nom du fichier `.psd1`

---

## 7. Lab : Créer un Module Complet

### 7.1 Structure du Projet

```powershell
# Créer la structure
$moduleName = "WorldlineTools"
$modulePath = "$HOME\Documents\PowerShell\Modules\$moduleName"

New-Item -ItemType Directory -Path $modulePath -Force
New-Item -ItemType Directory -Path "$modulePath\Public" -Force
New-Item -ItemType Directory -Path "$modulePath\Private" -Force
New-Item -ItemType Directory -Path "$modulePath\Tests" -Force
```

### 7.2 Fonction Privée : Private/Write-Log.ps1

```powershell
# Private/Write-Log.ps1
function Write-Log {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Message,

        [ValidateSet("INFO", "WARNING", "ERROR", "DEBUG")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"

    switch ($Level) {
        "ERROR"   { Write-Verbose $logMessage; Write-Error $Message }
        "WARNING" { Write-Verbose $logMessage; Write-Warning $Message }
        default   { Write-Verbose $logMessage }
    }
}
```

### 7.3 Fonction Publique : Public/Test-WorldlineConnection.ps1

```powershell
# Public/Test-WorldlineConnection.ps1
function Test-WorldlineConnection {
    <#
    .SYNOPSIS
        Teste la connectivité aux services Worldline.

    .EXAMPLE
        Test-WorldlineConnection -Service ActiveDirectory
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory)]
        [ValidateSet("ActiveDirectory", "AzureAD", "Exchange")]
        [string]$Service
    )

    Write-Log "Test de connexion au service $Service" -Level INFO

    $result = [PSCustomObject]@{
        Service   = $Service
        Status    = "Unknown"
        Timestamp = Get-Date
        Message   = $null
    }

    switch ($Service) {
        "ActiveDirectory" {
            try {
                $domain = Get-ADDomain -ErrorAction Stop
                $result.Status = "Connected"
                $result.Message = "Connecté au domaine $($domain.DNSRoot)"
            }
            catch {
                $result.Status = "Failed"
                $result.Message = $_.Exception.Message
            }
        }
        "AzureAD" {
            try {
                $context = Get-AzContext -ErrorAction Stop
                if ($context) {
                    $result.Status = "Connected"
                    $result.Message = "Connecté à $($context.Account.Id)"
                }
                else {
                    $result.Status = "NotConnected"
                    $result.Message = "Utilisez Connect-AzAccount"
                }
            }
            catch {
                $result.Status = "Failed"
                $result.Message = $_.Exception.Message
            }
        }
        "Exchange" {
            # Implémentation Exchange...
            $result.Status = "NotImplemented"
        }
    }

    Write-Log "Résultat : $($result.Status)" -Level INFO
    return $result
}
```

### 7.4 Module Loader : WorldlineTools.psm1

```powershell
# WorldlineTools.psm1
$ModuleRoot = $PSScriptRoot

# Dot-source des fichiers privés
Get-ChildItem -Path "$ModuleRoot\Private\*.ps1" -ErrorAction SilentlyContinue |
    ForEach-Object { . $_.FullName }

# Dot-source des fichiers publics
$publicFunctions = Get-ChildItem -Path "$ModuleRoot\Public\*.ps1" -ErrorAction SilentlyContinue
$publicFunctions | ForEach-Object { . $_.FullName }

# Exporter les fonctions publiques
Export-ModuleMember -Function ($publicFunctions.BaseName)
```

### 7.5 Manifeste : WorldlineTools.psd1

```powershell
# Générer le manifeste
New-ModuleManifest -Path "$modulePath\WorldlineTools.psd1" `
    -RootModule "WorldlineTools.psm1" `
    -ModuleVersion "1.0.0" `
    -Author "VotreNom" `
    -Description "Outils Worldline pour l'administration IT" `
    -PowerShellVersion "5.1" `
    -FunctionsToExport @('Test-WorldlineConnection') `
    -Tags @('Worldline', 'IT', 'Administration')
```

### 7.6 Test du Module

```powershell
# Recharger le module (force)
Remove-Module WorldlineTools -ErrorAction SilentlyContinue
Import-Module WorldlineTools -Force -Verbose

# Vérifier les fonctions exportées
Get-Command -Module WorldlineTools

# Tester
Test-WorldlineConnection -Service ActiveDirectory -Verbose

# La fonction privée n'est PAS accessible
Write-Log "Test"  # Erreur : commande non trouvée
```

---

## 8. Distribution et Publication

### 8.1 PowerShell Gallery

```powershell
# Prérequis : Clé API de PowerShell Gallery
# https://www.powershellgallery.com/account/apikeys

# Tester le module avant publication
Test-ModuleManifest -Path ".\WorldlineTools\WorldlineTools.psd1"

# Publier
Publish-Module -Path ".\WorldlineTools" -NuGetApiKey "VOTRE_CLE_API"
```

### 8.2 Repository Privé (NuGet)

```powershell
# Enregistrer un repository privé
Register-PSRepository -Name "WorldlineInternal" `
    -SourceLocation "https://nuget.worldline.internal/api/v2" `
    -PublishLocation "https://nuget.worldline.internal/api/v2/package" `
    -InstallationPolicy Trusted

# Publier sur le repo privé
Publish-Module -Path ".\WorldlineTools" `
    -Repository "WorldlineInternal" `
    -NuGetApiKey "VOTRE_CLE"

# Installer depuis le repo privé
Install-Module -Name WorldlineTools -Repository "WorldlineInternal"
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Quelle est la différence entre `.psm1` et `.psd1` ?"
    **Réponse :**

    - `.psm1` (Module Script) : Contient le **code** PowerShell (fonctions, classes, logique).

    - `.psd1` (Module Manifest) : Contient les **métadonnées** du module (version, auteur, dépendances, fonctions à exporter). C'est un fichier de configuration, pas de code.

    Le manifeste référence le module script via la propriété `RootModule`.

??? question "Question 2 : Pourquoi spécifier explicitement `FunctionsToExport` au lieu de `'*'` ?"
    **Réponse :**

    1. **Sécurité** : Empêche l'exposition accidentelle de fonctions internes/helpers
    2. **Performance** : PowerShell n'a pas à découvrir les exports dynamiquement
    3. **Documentation** : Liste claire de l'API publique
    4. **Auto-completion** : Améliore IntelliSense en limitant les suggestions

??? question "Question 3 : Comment faire pour qu'un module soit auto-chargé ?"
    **Réponse :** Le module doit être dans un des chemins de `$env:PSModulePath`, avec la structure :

    ```
    .../Modules/NomDuModule/NomDuModule.psd1
    ```

    Le nom du dossier doit correspondre au nom du module. PowerShell chargera automatiquement le module lorsqu'une de ses fonctions exportées sera appelée.

---

## Félicitations !

Vous avez terminé la formation **PowerShell Avancé & Tooling**. Vous maîtrisez maintenant :

- :material-check-circle: Les Advanced Functions avec `[CmdletBinding()]`
- :material-check-circle: La gestion d'erreurs robuste (Fail Fast)
- :material-check-circle: La création de modules professionnels

Vous êtes prêt à construire des **outils** de qualité production !

[:octicons-arrow-left-24: Retour au Module 2 : Gestion des Erreurs](02-error-handling.md)

[:octicons-home: Retour à l'index de la formation](index.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Avancé
