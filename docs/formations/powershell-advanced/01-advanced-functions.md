# Module 1 : Advanced Functions

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre la différence entre une fonction basique et une Advanced Function
- :material-check: Utiliser `[CmdletBinding()]` pour transformer vos fonctions en cmdlets
- :material-check: Définir des paramètres robustes avec validation
- :material-check: Supporter le pipeline PowerShell nativement

---

## 1. La Ligne Magique : `[CmdletBinding()]`

### 1.1 Fonction Basique vs Advanced Function

**Fonction basique (niveau débutant) :**

```powershell
function Get-UserInfo {
    param($Username)

    Write-Host "Recherche de l'utilisateur $Username..."
    Get-ADUser -Identity $Username
}
```

**Advanced Function (niveau professionnel) :**

```powershell
function Get-UserInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Username
    )

    Write-Verbose "Recherche de l'utilisateur $Username..."
    Get-ADUser -Identity $Username
}
```

!!! info "Qu'apporte `[CmdletBinding()]` ?"
    Cette simple ligne transforme votre fonction en une **cmdlet** à part entière, avec :

    | Fonctionnalité | Sans CmdletBinding | Avec CmdletBinding |
    |----------------|--------------------|--------------------|
    | `-Verbose` | Non disponible | Automatique |
    | `-Debug` | Non disponible | Automatique |
    | `-ErrorAction` | Non disponible | Automatique |
    | `-WhatIf` / `-Confirm` | Non disponible | Activable |
    | Validation des paramètres | Manuelle | Déclarative |
    | Pipeline support | Limité | Complet |

### 1.2 Les Paramètres Communs

Avec `[CmdletBinding()]`, votre fonction hérite automatiquement des **Common Parameters** :

```powershell
# Tous ces appels fonctionnent automatiquement !
Get-UserInfo -Username "jdupont" -Verbose
Get-UserInfo -Username "jdupont" -Debug
Get-UserInfo -Username "jdupont" -ErrorAction Stop
Get-UserInfo -Username "jdupont" -OutVariable result
```

---

## 2. Définition des Paramètres

### 2.1 Syntaxe Complète

```powershell
function Verb-Noun {
    [CmdletBinding()]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Description du paramètre"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$ParameterName
    )

    # Corps de la fonction
}
```

### 2.2 Attributs de Paramètres

| Attribut | Description | Exemple |
|----------|-------------|---------|
| `Mandatory` | Paramètre obligatoire | `Mandatory = $true` |
| `Position` | Position dans l'appel (sans nommer) | `Position = 0` |
| `ValueFromPipeline` | Accepte l'objet entier du pipe | `ValueFromPipeline = $true` |
| `ValueFromPipelineByPropertyName` | Accepte une propriété spécifique | Mappe la propriété "Name" |
| `HelpMessage` | Message d'aide si omis | Affiché à l'utilisateur |
| `ParameterSetName` | Groupe de paramètres exclusifs | Pour des modes différents |

### 2.3 Validateurs de Paramètres

```powershell
param(
    # Ne doit pas être null ou vide
    [ValidateNotNullOrEmpty()]
    [string]$Username,

    # Doit être dans une liste définie
    [ValidateSet("Production", "Staging", "Development")]
    [string]$Environment,

    # Doit correspondre à un pattern regex
    [ValidatePattern("^[a-zA-Z0-9._%+-]+@worldline\.com$")]
    [string]$Email,

    # Doit être dans une plage numérique
    [ValidateRange(1, 100)]
    [int]$Percentage,

    # Doit avoir une longueur spécifique
    [ValidateLength(8, 64)]
    [string]$Password,

    # Validation par script personnalisé
    [ValidateScript({
        if (Test-Path $_) { $true }
        else { throw "Le chemin '$_' n'existe pas." }
    })]
    [string]$FilePath,

    # Nombre d'éléments dans un tableau
    [ValidateCount(1, 10)]
    [string[]]$ServerList
)
```

!!! tip "Fail Fast"
    Les validateurs s'exécutent **avant** le corps de la fonction. Si la validation échoue, la fonction ne s'exécute jamais. C'est le principe du "Fail Fast" : détecter les erreurs le plus tôt possible.

---

## 3. Support du Pipeline

### 3.1 Comprendre le Pipeline

Le pipeline PowerShell passe des **objets** d'une commande à l'autre :

```powershell
# Chaque utilisateur est passé un par un à la fonction suivante
Get-ADUser -Filter * | ForEach-Object { ... }
```

Pour supporter le pipeline, votre fonction doit implémenter les blocs `Begin`, `Process` et `End` :

```
┌─────────────────────────────────────────────────────────────────┐
│                    CYCLE DE VIE DU PIPELINE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                      BEGIN                              │   │
│   │  Exécuté UNE SEULE FOIS au début                       │   │
│   │  → Initialisation, connexions, variables               │   │
│   └─────────────────────────────────────────────────────────┘   │
│                           │                                     │
│                           ▼                                     │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                      PROCESS                            │   │
│   │  Exécuté POUR CHAQUE OBJET du pipeline                 │   │
│   │  → Traitement de $_ ou du paramètre pipeline           │   │
│   │                                                         │   │
│   │  Objet 1 → PROCESS                                     │   │
│   │  Objet 2 → PROCESS                                     │   │
│   │  Objet 3 → PROCESS                                     │   │
│   │  ...                                                    │   │
│   └─────────────────────────────────────────────────────────┘   │
│                           │                                     │
│                           ▼                                     │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                       END                               │   │
│   │  Exécuté UNE SEULE FOIS à la fin                       │   │
│   │  → Nettoyage, fermeture connexions, rapport final      │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Exemple avec Pipeline

```powershell
function Get-UserStatus {
    [CmdletBinding()]
    param(
        [Parameter(
            Mandatory = $true,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true
        )]
        [string]$SamAccountName
    )

    begin {
        Write-Verbose "Initialisation - Connexion à Active Directory"
        $results = @()
    }

    process {
        Write-Verbose "Traitement de l'utilisateur : $SamAccountName"

        try {
            $user = Get-ADUser -Identity $SamAccountName -Properties Enabled, LastLogonDate
            $results += [PSCustomObject]@{
                Username      = $SamAccountName
                Enabled       = $user.Enabled
                LastLogonDate = $user.LastLogonDate
                Status        = if ($user.Enabled) { "Actif" } else { "Désactivé" }
            }
        }
        catch {
            Write-Warning "Utilisateur non trouvé : $SamAccountName"
        }
    }

    end {
        Write-Verbose "Fin du traitement - $($results.Count) utilisateurs traités"
        return $results
    }
}

# Utilisation via pipeline
"jdupont", "mmartin", "adurand" | Get-UserStatus -Verbose

# Ou avec des objets AD
Get-ADGroupMember -Identity "IT-Team" | Get-UserStatus
```

---

## 4. SupportsShouldProcess : WhatIf et Confirm

### 4.1 Activation

Pour les fonctions qui **modifient** des données, activez `SupportsShouldProcess` :

```powershell
function Remove-OldUsers {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory = $true)]
        [int]$InactiveDays = 90
    )

    $oldUsers = Get-ADUser -Filter * -Properties LastLogonDate |
        Where-Object { $_.LastLogonDate -lt (Get-Date).AddDays(-$InactiveDays) }

    foreach ($user in $oldUsers) {
        # $PSCmdlet.ShouldProcess vérifie -WhatIf et -Confirm
        if ($PSCmdlet.ShouldProcess($user.SamAccountName, "Désactiver le compte")) {
            Disable-ADAccount -Identity $user.SamAccountName
            Write-Verbose "Compte désactivé : $($user.SamAccountName)"
        }
    }
}
```

### 4.2 Utilisation

```powershell
# Mode simulation : montre ce qui SERAIT fait
Remove-OldUsers -InactiveDays 90 -WhatIf

# Mode confirmation : demande pour chaque action
Remove-OldUsers -InactiveDays 90 -Confirm

# Mode automatique (attention en production !)
Remove-OldUsers -InactiveDays 90 -Confirm:$false
```

**Sortie WhatIf :**

```
What if: Performing the operation "Désactiver le compte" on target "jdupont".
What if: Performing the operation "Désactiver le compte" on target "ancien_user".
```

---

## 5. Template Production-Ready

### 5.1 Fonction Complète : `New-WorldlineUser`

```powershell
function New-WorldlineUser {
    <#
    .SYNOPSIS
        Crée un nouvel utilisateur dans Active Directory selon les standards Worldline.

    .DESCRIPTION
        Cette fonction crée un utilisateur AD avec les attributs standards Worldline :
        - Génération automatique du SamAccountName
        - Placement dans l'OU appropriée selon le département
        - Application des groupes par défaut
        - Envoi d'un email de bienvenue (optionnel)

    .PARAMETER FirstName
        Prénom de l'utilisateur. Obligatoire.

    .PARAMETER LastName
        Nom de famille de l'utilisateur. Obligatoire.

    .PARAMETER Department
        Département de l'utilisateur. Doit être une valeur valide.

    .PARAMETER Manager
        SamAccountName du manager. Optionnel.

    .PARAMETER SendWelcomeEmail
        Si spécifié, envoie un email de bienvenue à l'utilisateur.

    .EXAMPLE
        New-WorldlineUser -FirstName "Jean" -LastName "Dupont" -Department "IT"

        Crée l'utilisateur Jean Dupont dans le département IT.

    .EXAMPLE
        Import-Csv users.csv | New-WorldlineUser -Verbose

        Crée tous les utilisateurs depuis un fichier CSV via le pipeline.

    .NOTES
        Auteur  : ShellBook Automation Team
        Version : 1.0
        Date    : 2025-01-28
    #>

    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    [OutputType([Microsoft.ActiveDirectory.Management.ADUser])]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Prénom de l'utilisateur"
        )]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern("^[a-zA-ZÀ-ÿ\s\-']+$")]
        [Alias("Prenom", "GivenName")]
        [string]$FirstName,

        [Parameter(
            Mandatory = $true,
            Position = 1,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Nom de famille de l'utilisateur"
        )]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern("^[a-zA-ZÀ-ÿ\s\-']+$")]
        [Alias("Nom", "Surname")]
        [string]$LastName,

        [Parameter(
            Mandatory = $true,
            ValueFromPipelineByPropertyName = $true
        )]
        [ValidateSet("IT", "HR", "Finance", "Sales", "Operations", "Legal")]
        [string]$Department,

        [Parameter(ValueFromPipelineByPropertyName = $true)]
        [ValidateScript({
            if ([string]::IsNullOrEmpty($_)) { return $true }
            try {
                Get-ADUser -Identity $_ -ErrorAction Stop | Out-Null
                return $true
            }
            catch {
                throw "Le manager '$_' n'existe pas dans Active Directory."
            }
        })]
        [string]$Manager,

        [Parameter()]
        [switch]$SendWelcomeEmail
    )

    begin {
        Write-Verbose "=== Début de New-WorldlineUser ==="
        Write-Verbose "Vérification des prérequis..."

        # Vérifier le module AD
        if (-not (Get-Module -Name ActiveDirectory -ErrorAction SilentlyContinue)) {
            try {
                Import-Module ActiveDirectory -ErrorAction Stop
                Write-Verbose "Module ActiveDirectory chargé."
            }
            catch {
                throw "Impossible de charger le module ActiveDirectory. Erreur : $_"
            }
        }

        # Configuration
        $domainDN = (Get-ADDomain).DistinguishedName
        $basePath = "OU=Users,OU=Worldline,$domainDN"
        $defaultPassword = ConvertTo-SecureString "Welcome2024!" -AsPlainText -Force
        $createdUsers = @()
    }

    process {
        Write-Verbose "Traitement : $FirstName $LastName ($Department)"

        # Générer le SamAccountName (première lettre prénom + nom, max 20 chars)
        $samBase = ($FirstName.Substring(0, 1) + $LastName) -replace "[^a-zA-Z]", ""
        $samAccountName = $samBase.ToLower().Substring(0, [Math]::Min(20, $samBase.Length))

        # Vérifier si l'utilisateur existe déjà
        $existingUser = Get-ADUser -Filter "SamAccountName -eq '$samAccountName'" -ErrorAction SilentlyContinue
        if ($existingUser) {
            Write-Warning "L'utilisateur '$samAccountName' existe déjà. Ajout d'un suffixe..."
            $counter = 1
            do {
                $samAccountName = "$($samBase.ToLower().Substring(0, [Math]::Min(17, $samBase.Length)))$counter"
                $counter++
            } while (Get-ADUser -Filter "SamAccountName -eq '$samAccountName'" -ErrorAction SilentlyContinue)
        }

        # Construire l'UPN et le DisplayName
        $upn = "$samAccountName@worldline.com"
        $displayName = "$FirstName $LastName"

        # Définir l'OU cible selon le département
        $targetOU = "OU=$Department,$basePath"

        # Créer l'utilisateur
        if ($PSCmdlet.ShouldProcess($displayName, "Créer l'utilisateur AD")) {
            try {
                $userParams = @{
                    Name              = $displayName
                    GivenName         = $FirstName
                    Surname           = $LastName
                    SamAccountName    = $samAccountName
                    UserPrincipalName = $upn
                    DisplayName       = $displayName
                    Department        = $Department
                    Path              = $targetOU
                    AccountPassword   = $defaultPassword
                    Enabled           = $true
                    ChangePasswordAtLogon = $true
                }

                if (-not [string]::IsNullOrEmpty($Manager)) {
                    $userParams.Manager = $Manager
                }

                $newUser = New-ADUser @userParams -PassThru -ErrorAction Stop
                Write-Verbose "Utilisateur créé : $samAccountName"

                # Ajouter aux groupes par défaut
                $defaultGroups = @("Domain Users", "Worldline-AllUsers")
                foreach ($group in $defaultGroups) {
                    try {
                        Add-ADGroupMember -Identity $group -Members $samAccountName -ErrorAction Stop
                        Write-Verbose "  Ajouté au groupe : $group"
                    }
                    catch {
                        Write-Warning "  Impossible d'ajouter au groupe '$group' : $_"
                    }
                }

                # Envoyer l'email de bienvenue si demandé
                if ($SendWelcomeEmail) {
                    Write-Verbose "  Envoi de l'email de bienvenue..."
                    # Ici, implémenter l'envoi d'email
                    # Send-MailMessage -To $upn -Subject "Bienvenue chez Worldline" -Body "..."
                }

                $createdUsers += $newUser
                Write-Output $newUser
            }
            catch {
                Write-Error "Échec de création de '$displayName' : $_"
            }
        }
    }

    end {
        Write-Verbose "=== Fin de New-WorldlineUser ==="
        Write-Verbose "Utilisateurs créés : $($createdUsers.Count)"
    }
}
```

### 5.2 Utilisation du Template

```powershell
# Import du module (si dans un module)
Import-Module WorldlineTools

# Création simple
New-WorldlineUser -FirstName "Jean" -LastName "Dupont" -Department "IT" -Verbose

# Création avec manager
New-WorldlineUser -FirstName "Marie" -LastName "Martin" -Department "HR" -Manager "pdurand"

# Mode simulation
New-WorldlineUser -FirstName "Test" -LastName "User" -Department "IT" -WhatIf

# Création en masse via CSV
Import-Csv "C:\Data\new_users.csv" | New-WorldlineUser -SendWelcomeEmail -Verbose

# Aide intégrée
Get-Help New-WorldlineUser -Full
Get-Help New-WorldlineUser -Examples
```

---

## 6. Bonnes Pratiques

### 6.1 Checklist Advanced Function

- [ ] `[CmdletBinding()]` en première ligne du bloc `param`
- [ ] Paramètres typés (`[string]`, `[int]`, `[switch]`)
- [ ] Validateurs appropriés (`ValidateNotNullOrEmpty`, `ValidateSet`, etc.)
- [ ] Blocs `Begin`/`Process`/`End` si support pipeline
- [ ] `SupportsShouldProcess` pour les actions destructives
- [ ] Documentation Comment-Based Help (`.SYNOPSIS`, `.EXAMPLE`)
- [ ] `Write-Verbose` pour le debugging (pas `Write-Host`)
- [ ] `Write-Warning` pour les alertes non bloquantes
- [ ] `Write-Error` ou `throw` pour les erreurs

### 6.2 Anti-Patterns à Éviter

```powershell
# ❌ MAUVAIS : Write-Host pour les informations
Write-Host "Traitement en cours..."

# ✅ BON : Write-Verbose (contrôlable par l'appelant)
Write-Verbose "Traitement en cours..."

# ❌ MAUVAIS : Paramètre sans validation
param($Username)

# ✅ BON : Paramètre validé
param(
    [ValidateNotNullOrEmpty()]
    [string]$Username
)

# ❌ MAUVAIS : Modifier des données sans ShouldProcess
Remove-Item $Path

# ✅ BON : Vérifier avec ShouldProcess
if ($PSCmdlet.ShouldProcess($Path, "Supprimer")) {
    Remove-Item $Path
}
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Quelle est la différence entre `ValueFromPipeline` et `ValueFromPipelineByPropertyName` ?"
    **Réponse :**

    - `ValueFromPipeline = $true` : Le paramètre accepte l'**objet entier** du pipeline. Utile quand vous attendez un type d'objet spécifique.

    - `ValueFromPipelineByPropertyName = $true` : Le paramètre accepte une **propriété spécifique** de l'objet, dont le nom correspond au nom du paramètre (ou à un alias).

    Exemple : Si votre paramètre s'appelle `SamAccountName`, PowerShell mappera automatiquement la propriété `SamAccountName` des objets passés via le pipeline.

??? question "Question 2 : Pourquoi utiliser `Write-Verbose` plutôt que `Write-Host` ?"
    **Réponse :**

    - `Write-Host` : Affiche **toujours** du texte, impossible à désactiver, ne peut pas être redirigé, pollue la sortie.

    - `Write-Verbose` : Affiché **uniquement** si l'appelant utilise `-Verbose`. Permet un debugging conditionnel et respecte les pratiques de scripting propre.

    En production, les scripts doivent être silencieux par défaut. Le verbose est une option pour le debugging.

??? question "Question 3 : Quand utiliser `ConfirmImpact = 'High'` ?"
    **Réponse :** Quand votre fonction effectue des opérations **dangereuses ou irréversibles** :

    - Suppression de données
    - Modification de configuration critique
    - Actions affectant de nombreux objets

    Avec `ConfirmImpact = 'High'`, PowerShell demandera automatiquement confirmation même si l'utilisateur n'a pas spécifié `-Confirm`.

---

## Prochaine Étape

Vous savez construire des fonctions avancées. Apprenez maintenant à les rendre robustes face aux erreurs.

[:octicons-arrow-right-24: Module 2 : Gestion des Erreurs](02-error-handling.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Intermédiaire
