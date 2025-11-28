# Module 2 : Gestion des Erreurs

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Distinguer les erreurs terminantes des erreurs non-terminantes
- :material-check: Implémenter le pattern `Try / Catch / Finally`
- :material-check: Appliquer la stratégie "Fail Fast"
- :material-check: Logger les erreurs proprement sans interrompre l'exécution

---

## 1. Types d'Erreurs en PowerShell

### 1.1 Le Problème

PowerShell a un comportement par défaut **permissif** : quand une erreur survient, le script **continue** souvent son exécution. C'est dangereux en production.

```powershell
# Ce script continue même si le fichier n'existe pas !
Get-Content "fichier_inexistant.txt"
Write-Host "Cette ligne s'exécute quand même..."
```

### 1.2 Erreurs Terminantes vs Non-Terminantes

```
┌─────────────────────────────────────────────────────────────────┐
│                    TYPES D'ERREURS POWERSHELL                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ERREUR NON-TERMINANTE (Non-Terminating)                       │
│   ────────────────────────────────────────                      │
│   • Comportement par DÉFAUT de la plupart des cmdlets           │
│   • Le script CONTINUE après l'erreur                           │
│   • Erreur écrite dans $Error, affichée en rouge                │
│   • Exemples : fichier non trouvé, permission refusée           │
│                                                                 │
│   Comportement :                                                │
│   Get-Item "inexistant.txt"  # Erreur affichée                  │
│   Write-Host "Continue..."    # ← S'exécute quand même !        │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ERREUR TERMINANTE (Terminating)                               │
│   ───────────────────────────────                               │
│   • ARRÊTE l'exécution immédiatement                            │
│   • Peut être interceptée par Try/Catch                         │
│   • Générée par : throw, erreurs de syntaxe, -ErrorAction Stop  │
│   • Exemples : division par zéro, cmdlet avec -ErrorAction Stop │
│                                                                 │
│   Comportement :                                                │
│   throw "Erreur critique"     # Arrêt immédiat                  │
│   Write-Host "Jamais atteint" # ← Ne s'exécute PAS              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Démonstration

```powershell
# Erreur NON-terminante (continue)
Write-Host "=== Test 1 : Non-Terminating ===" -ForegroundColor Cyan
Get-Item "C:\fichier_qui_nexiste_pas.txt"
Write-Host "Cette ligne s'affiche malgré l'erreur`n"

# Erreur TERMINANTE (arrête)
Write-Host "=== Test 2 : Terminating ===" -ForegroundColor Cyan
throw "Ceci est une erreur terminante"
Write-Host "Cette ligne ne s'affiche JAMAIS"
```

---

## 2. Le Pattern Try / Catch / Finally

### 2.1 Syntaxe de Base

```powershell
try {
    # Code susceptible de générer une erreur
    # Les erreurs TERMINANTES sont interceptées
}
catch {
    # Code exécuté SI une erreur survient
    # $_ contient l'objet erreur
}
finally {
    # Code TOUJOURS exécuté (erreur ou pas)
    # Idéal pour le nettoyage (fermer connexions, etc.)
}
```

### 2.2 Convertir les Erreurs en Terminantes

!!! danger "Point Crucial"
    Par défaut, `Try/Catch` **n'intercepte pas** les erreurs non-terminantes ! Il faut forcer la conversion avec `-ErrorAction Stop`.

```powershell
# ❌ MAUVAIS : L'erreur n'est PAS interceptée
try {
    Get-Item "inexistant.txt"  # Erreur non-terminante
}
catch {
    Write-Host "Catch jamais atteint !"
}

# ✅ BON : L'erreur EST interceptée
try {
    Get-Item "inexistant.txt" -ErrorAction Stop  # Converti en terminante
}
catch {
    Write-Host "Erreur interceptée : $($_.Exception.Message)"
}
```

### 2.3 Accéder aux Informations d'Erreur

Dans le bloc `catch`, l'objet `$_` (ou `$PSItem`) contient l'erreur :

```powershell
try {
    Get-ADUser -Identity "utilisateur_inexistant" -ErrorAction Stop
}
catch {
    # Informations disponibles
    Write-Host "Message         : $($_.Exception.Message)"
    Write-Host "Type d'erreur   : $($_.Exception.GetType().FullName)"
    Write-Host "Catégorie       : $($_.CategoryInfo.Category)"
    Write-Host "Cmdlet fautive  : $($_.InvocationInfo.MyCommand)"
    Write-Host "Ligne           : $($_.InvocationInfo.ScriptLineNumber)"
    Write-Host "Stack Trace     : $($_.ScriptStackTrace)"
}
```

### 2.4 Catch par Type d'Exception

Vous pouvez intercepter des types d'erreurs spécifiques :

```powershell
try {
    $connection = Connect-Database -Server "sql01" -ErrorAction Stop
    $data = Invoke-SqlQuery -Query "SELECT * FROM Users" -ErrorAction Stop
}
catch [System.Net.Sockets.SocketException] {
    # Erreur réseau spécifique
    Write-Error "Impossible de se connecter au serveur : $($_.Exception.Message)"
    Send-Alert -Message "Serveur SQL inaccessible"
}
catch [System.Data.SqlClient.SqlException] {
    # Erreur SQL spécifique
    Write-Error "Erreur SQL : $($_.Exception.Message)"
    Write-Log -Level Error -Message "Query failed: $($_.Exception.Number)"
}
catch [Microsoft.ActiveDirectory.Management.ADIdentityNotFoundException] {
    # Utilisateur AD non trouvé
    Write-Warning "Utilisateur non trouvé dans Active Directory"
}
catch {
    # Catch-all pour toutes les autres erreurs
    Write-Error "Erreur inattendue : $($_.Exception.Message)"
    throw  # Relancer l'erreur pour le niveau supérieur
}
finally {
    # Nettoyage garanti
    if ($connection) {
        $connection.Close()
        Write-Verbose "Connexion fermée."
    }
}
```

---

## 3. Stratégie "Fail Fast"

### 3.1 Le Principe

!!! info "Fail Fast = Échouer Rapidement"
    Plutôt que de laisser un script continuer avec des données corrompues ou un état incohérent, il vaut mieux **arrêter immédiatement** dès qu'une erreur survient.

    Avantages :
    - Évite les corruptions de données
    - Facilite le debugging (l'erreur est proche de la cause)
    - Comportement prévisible et déterministe

### 3.2 Implémentation avec `$ErrorActionPreference`

```powershell
function Invoke-CriticalOperation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName
    )

    # Activer le mode "Fail Fast" pour TOUTE la fonction
    $ErrorActionPreference = 'Stop'

    try {
        Write-Verbose "Connexion à $ServerName..."
        $session = New-PSSession -ComputerName $ServerName

        Write-Verbose "Exécution de la maintenance..."
        Invoke-Command -Session $session -ScriptBlock {
            Stop-Service -Name "MonService"
            # Si cette ligne échoue, tout s'arrête immédiatement
            Copy-Item "C:\Source\*" -Destination "C:\Backup\" -Recurse
            Start-Service -Name "MonService"
        }

        Write-Verbose "Opération terminée avec succès."
    }
    catch {
        Write-Error "Échec de l'opération sur $ServerName : $($_.Exception.Message)"
        throw  # Propager l'erreur
    }
    finally {
        if ($session) {
            Remove-PSSession -Session $session
            Write-Verbose "Session fermée."
        }
    }
}
```

### 3.3 Valeurs de `$ErrorActionPreference`

| Valeur | Comportement | Usage |
|--------|--------------|-------|
| `Continue` | Affiche l'erreur, continue (défaut) | Scripts interactifs |
| `Stop` | Convertit TOUTES les erreurs en terminantes | **Production recommandé** |
| `SilentlyContinue` | Ignore les erreurs, continue | Cas très spécifiques |
| `Inquire` | Demande à l'utilisateur | Debugging interactif |
| `Ignore` | Ignore complètement (même pas dans $Error) | Rarement utile |

---

## 4. Logging Propre des Erreurs

### 4.1 Fonction de Logging Réutilisable

```powershell
function Write-Log {
    <#
    .SYNOPSIS
        Écrit un message formaté dans un fichier de log et optionnellement dans la console.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Message,

        [Parameter()]
        [ValidateSet("INFO", "WARNING", "ERROR", "DEBUG")]
        [string]$Level = "INFO",

        [Parameter()]
        [string]$LogPath = "$env:TEMP\script_$(Get-Date -Format 'yyyyMMdd').log",

        [Parameter()]
        [switch]$NoConsole
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"

    # Écrire dans le fichier
    Add-Content -Path $LogPath -Value $logEntry -Encoding UTF8

    # Écrire dans la console selon le niveau
    if (-not $NoConsole) {
        switch ($Level) {
            "ERROR"   { Write-Host $logEntry -ForegroundColor Red }
            "WARNING" { Write-Host $logEntry -ForegroundColor Yellow }
            "DEBUG"   { Write-Host $logEntry -ForegroundColor Gray }
            default   { Write-Host $logEntry -ForegroundColor White }
        }
    }
}
```

### 4.2 Pattern Complet avec Logging

```powershell
function Sync-ADUsersFromHR {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$CsvPath,

        [Parameter()]
        [string]$LogPath = "C:\Logs\HR_Sync_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    )

    # Configuration Fail Fast
    $ErrorActionPreference = 'Stop'

    # Compteurs
    $stats = @{
        Total     = 0
        Created   = 0
        Updated   = 0
        Errors    = 0
    }

    Write-Log "=== Début de la synchronisation HR ===" -Level INFO -LogPath $LogPath
    Write-Log "Fichier source : $CsvPath" -Level INFO -LogPath $LogPath

    try {
        # Vérifier le fichier
        if (-not (Test-Path $CsvPath)) {
            throw "Le fichier CSV n'existe pas : $CsvPath"
        }

        # Charger les données
        $users = Import-Csv -Path $CsvPath -Delimiter ";" -Encoding UTF8
        $stats.Total = $users.Count
        Write-Log "Utilisateurs à traiter : $($stats.Total)" -Level INFO -LogPath $LogPath

        foreach ($user in $users) {
            try {
                $samAccountName = ($user.Prenom.Substring(0,1) + $user.Nom).ToLower()

                # Vérifier si l'utilisateur existe
                $adUser = Get-ADUser -Filter "SamAccountName -eq '$samAccountName'" -ErrorAction SilentlyContinue

                if ($adUser) {
                    # Mise à jour
                    Set-ADUser -Identity $samAccountName `
                        -Department $user.Departement `
                        -Title $user.Poste `
                        -ErrorAction Stop

                    $stats.Updated++
                    Write-Log "Mis à jour : $samAccountName" -Level INFO -LogPath $LogPath
                }
                else {
                    # Création
                    New-ADUser -Name "$($user.Prenom) $($user.Nom)" `
                        -GivenName $user.Prenom `
                        -Surname $user.Nom `
                        -SamAccountName $samAccountName `
                        -Department $user.Departement `
                        -Enabled $true `
                        -ErrorAction Stop

                    $stats.Created++
                    Write-Log "Créé : $samAccountName" -Level INFO -LogPath $LogPath
                }
            }
            catch [Microsoft.ActiveDirectory.Management.ADException] {
                # Erreur AD spécifique - on log mais on continue
                $stats.Errors++
                Write-Log "Erreur AD pour $($user.Nom) : $($_.Exception.Message)" -Level ERROR -LogPath $LogPath
                # Ne pas throw - continuer avec l'utilisateur suivant
            }
            catch {
                # Autre erreur - on log et continue
                $stats.Errors++
                Write-Log "Erreur pour $($user.Nom) : $($_.Exception.Message)" -Level ERROR -LogPath $LogPath
            }
        }
    }
    catch {
        # Erreur fatale (fichier inexistant, problème de connexion AD global...)
        Write-Log "ERREUR FATALE : $($_.Exception.Message)" -Level ERROR -LogPath $LogPath
        Write-Log "Stack Trace : $($_.ScriptStackTrace)" -Level DEBUG -LogPath $LogPath
        throw  # Relancer pour l'appelant
    }
    finally {
        # Rapport final
        Write-Log "=== Fin de la synchronisation ===" -Level INFO -LogPath $LogPath
        Write-Log "Total traité : $($stats.Total)" -Level INFO -LogPath $LogPath
        Write-Log "Créés        : $($stats.Created)" -Level INFO -LogPath $LogPath
        Write-Log "Mis à jour   : $($stats.Updated)" -Level INFO -LogPath $LogPath
        Write-Log "Erreurs      : $($stats.Errors)" -Level INFO -LogPath $LogPath

        # Retourner les statistiques
        [PSCustomObject]$stats
    }
}
```

---

## 5. Relancer ou Encapsuler les Erreurs

### 5.1 Relancer une Erreur

```powershell
try {
    Do-Something -ErrorAction Stop
}
catch {
    # Logger l'erreur
    Write-Log "Erreur dans Do-Something : $($_.Exception.Message)" -Level ERROR

    # Relancer l'erreur originale (conserve le stack trace)
    throw
}
```

### 5.2 Encapsuler avec un Message Personnalisé

```powershell
try {
    Do-Something -ErrorAction Stop
}
catch {
    # Créer une nouvelle erreur avec contexte
    $errorMessage = "Échec de l'opération X sur le serveur Y. Erreur originale : $($_.Exception.Message)"
    throw [System.Exception]::new($errorMessage, $_.Exception)
}
```

### 5.3 Erreurs Personnalisées

```powershell
# Définir une classe d'exception personnalisée
class WorldlineConfigException : System.Exception {
    [string]$ConfigFile

    WorldlineConfigException([string]$message, [string]$configFile) : base($message) {
        $this.ConfigFile = $configFile
    }
}

# Utilisation
function Get-WorldlineConfig {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw [WorldlineConfigException]::new(
            "Fichier de configuration introuvable",
            $Path
        )
    }

    Get-Content $Path | ConvertFrom-Json
}

# Catch spécifique
try {
    $config = Get-WorldlineConfig -Path "C:\Config\app.json"
}
catch [WorldlineConfigException] {
    Write-Error "Configuration manquante : $($_.Exception.ConfigFile)"
}
```

---

## 6. Lab : Gestion d'Erreur AD Complète

### 6.1 Scénario

Créer une fonction qui vérifie la connectivité AD et retourne un status propre sans crasher la console.

```powershell
function Test-ADConnectivity {
    <#
    .SYNOPSIS
        Teste la connectivité à Active Directory et retourne un rapport de status.

    .EXAMPLE
        Test-ADConnectivity -DomainController "DC01"
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter()]
        [string]$DomainController = $env:LOGONSERVER -replace '\\\\',''
    )

    $result = [PSCustomObject]@{
        DomainController = $DomainController
        Timestamp        = Get-Date
        DNSResolution    = $false
        PortLDAP         = $false
        PortLDAPS        = $false
        Authentication   = $false
        ErrorMessage     = $null
        Status           = "Unknown"
    }

    Write-Verbose "Test de connectivité AD vers $DomainController"

    try {
        # Test 1 : Résolution DNS
        Write-Verbose "  [1/4] Test DNS..."
        try {
            $dns = Resolve-DnsName -Name $DomainController -ErrorAction Stop
            $result.DNSResolution = $true
            Write-Verbose "    ✓ DNS OK : $($dns.IPAddress)"
        }
        catch {
            throw "Échec DNS : $($_.Exception.Message)"
        }

        # Test 2 : Port LDAP (389)
        Write-Verbose "  [2/4] Test port LDAP (389)..."
        try {
            $tcpLDAP = Test-NetConnection -ComputerName $DomainController -Port 389 -WarningAction SilentlyContinue
            if ($tcpLDAP.TcpTestSucceeded) {
                $result.PortLDAP = $true
                Write-Verbose "    ✓ LDAP OK"
            }
            else {
                throw "Port LDAP 389 fermé"
            }
        }
        catch {
            Write-Warning "    ✗ LDAP : $($_.Exception.Message)"
            # Continuer les tests
        }

        # Test 3 : Port LDAPS (636)
        Write-Verbose "  [3/4] Test port LDAPS (636)..."
        try {
            $tcpLDAPS = Test-NetConnection -ComputerName $DomainController -Port 636 -WarningAction SilentlyContinue
            if ($tcpLDAPS.TcpTestSucceeded) {
                $result.PortLDAPS = $true
                Write-Verbose "    ✓ LDAPS OK"
            }
            else {
                Write-Verbose "    ✗ LDAPS non disponible (optionnel)"
            }
        }
        catch {
            Write-Verbose "    ✗ LDAPS : $($_.Exception.Message)"
        }

        # Test 4 : Authentification AD
        Write-Verbose "  [4/4] Test authentification AD..."
        try {
            $domain = Get-ADDomain -Server $DomainController -ErrorAction Stop
            $result.Authentication = $true
            Write-Verbose "    ✓ Auth OK - Domaine : $($domain.DNSRoot)"
        }
        catch [Microsoft.ActiveDirectory.Management.ADServerDownException] {
            throw "Serveur AD inaccessible : $($_.Exception.Message)"
        }
        catch [System.Security.Authentication.AuthenticationException] {
            throw "Échec d'authentification : vérifiez vos credentials"
        }
        catch {
            throw "Erreur AD : $($_.Exception.Message)"
        }

        # Si on arrive ici, tout est OK
        $result.Status = "Healthy"
        Write-Verbose "Résultat : HEALTHY"
    }
    catch {
        $result.ErrorMessage = $_.Exception.Message
        $result.Status = "Unhealthy"
        Write-Verbose "Résultat : UNHEALTHY - $($_.Exception.Message)"
    }

    return $result
}

# Utilisation
$status = Test-ADConnectivity -Verbose

# Affichage formaté
$status | Format-List

# Vérification programmatique
if ($status.Status -eq "Healthy") {
    Write-Host "AD est accessible" -ForegroundColor Green
}
else {
    Write-Host "Problème AD : $($status.ErrorMessage)" -ForegroundColor Red
}
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Pourquoi `Try/Catch` n'intercepte pas une erreur `Get-Item` par défaut ?"
    **Réponse :** Par défaut, `Get-Item` génère une erreur **non-terminante**. `Try/Catch` ne capture que les erreurs **terminantes**.

    Solution : Ajouter `-ErrorAction Stop` pour convertir l'erreur en terminante.

??? question "Question 2 : Quelle est la différence entre `throw` et `Write-Error` ?"
    **Réponse :**

    - `throw` : Génère une erreur **terminante**. Arrête l'exécution (sauf si interceptée par catch). Utilisé pour les erreurs critiques.

    - `Write-Error` : Génère une erreur **non-terminante** par défaut. L'exécution continue. Utilisé pour signaler un problème sans arrêter le script.

??? question "Question 3 : Quand utiliser le bloc `finally` ?"
    **Réponse :** Le bloc `finally` s'exécute **toujours**, qu'il y ait eu une erreur ou non. Il est idéal pour :

    - Fermer des connexions (base de données, sessions distantes)
    - Libérer des ressources (fichiers, locks)
    - Écrire des logs de fin d'exécution
    - Remettre l'état initial (restaurer `$ErrorActionPreference`)

---

## Prochaine Étape

Vous savez gérer les erreurs proprement. Apprenez maintenant à packager vos fonctions en modules réutilisables.

[:octicons-arrow-right-24: Module 3 : Conception de Modules](03-module-design.md)

---

**Temps estimé :** 45 minutes
**Niveau :** Intermédiaire
