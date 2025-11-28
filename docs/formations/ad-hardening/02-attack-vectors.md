# Module 2 : Vecteurs d'Attaque Active Directory

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre les mécanismes des attaques Kerberos (ASREPRoast, Kerberoasting)
- :material-check: Identifier les comptes vulnérables dans votre environnement
- :material-check: Implémenter les mesures de protection appropriées
- :material-check: Détecter les tentatives d'exploitation

!!! quote "Sun Tzu - L'Art de la Guerre"
    *"Connais ton ennemi et connais-toi toi-même ; eussiez-vous cent guerres à soutenir, cent fois vous serez victorieux."*

    Pour défendre Active Directory, il faut d'abord comprendre comment les attaquants l'attaquent.

---

## 1. ASREPRoast

### 1.1 Mécanisme de l'Attaque

!!! danger "Niveau de Risque : ÉLEVÉ"
    ASREPRoast permet à un attaquant d'obtenir un hash crackable **sans aucun privilège** préalable dans le domaine. Un simple compte utilisateur suffit.

**Fonctionnement normal de Kerberos :**

```
┌─────────────────────────────────────────────────────────────────┐
│              AUTHENTIFICATION KERBEROS NORMALE                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Client]                              [KDC/DC]                │
│      │                                      │                   │
│      │  1. AS-REQ (avec pré-auth)           │                   │
│      │     Timestamp chiffré avec           │                   │
│      │     le hash du mot de passe          │                   │
│      │─────────────────────────────────────►│                   │
│      │                                      │                   │
│      │  2. AS-REP (si pré-auth OK)          │                   │
│      │     TGT chiffré                      │                   │
│      │◄─────────────────────────────────────│                   │
│      │                                      │                   │
│   ✅ Pré-authentification = Preuve d'identité                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Quand la pré-authentification est désactivée :**

```
┌─────────────────────────────────────────────────────────────────┐
│              ATTAQUE ASREPROAST                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Attaquant]                           [KDC/DC]                │
│      │                                      │                   │
│      │  1. AS-REQ (SANS pré-auth)           │                   │
│      │     "Je veux un TGT pour             │                   │
│      │      l'utilisateur vulnerable"       │                   │
│      │─────────────────────────────────────►│                   │
│      │                                      │                   │
│      │  2. AS-REP                           │                   │
│      │     TGT chiffré avec le hash         │                   │
│      │     du mot de passe de l'user        │                   │
│      │◄─────────────────────────────────────│                   │
│      │                                      │                   │
│   ❌ Le KDC répond SANS vérifier l'identité !                   │
│                                                                 │
│   3. Attaquant extrait le hash du AS-REP                        │
│   4. Crack offline avec Hashcat                                 │
│   5. Mot de passe récupéré en clair                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Identifier les Comptes Vulnérables

```powershell
# Méthode 1 : PowerShell natif
Get-ADUser -Filter {DoesNotRequirePreAuth -eq $true} -Properties DoesNotRequirePreAuth |
    Select-Object Name, SamAccountName, DistinguishedName, DoesNotRequirePreAuth

# Méthode 2 : Avec filtre LDAP
Get-ADUser -LDAPFilter "(&(objectCategory=user)(userAccountControl:1.2.840.113556.1.4.803:=4194304))" |
    Select-Object Name, SamAccountName, DistinguishedName

# Méthode 3 : Vérification complète avec contexte
Get-ADUser -Filter {DoesNotRequirePreAuth -eq $true} -Properties * |
    Select-Object Name,
                  SamAccountName,
                  Enabled,
                  PasswordLastSet,
                  LastLogonDate,
                  @{N='MemberOf';E={($_.MemberOf | ForEach-Object { ($_ -split ',')[0] -replace 'CN=' }) -join ', '}}
```

!!! warning "Attention aux Comptes de Service"
    Cette option est parfois activée pour des raisons de "compatibilité" avec des applications legacy. Documentez chaque exception et planifiez leur remédiation.

### 1.3 Remédiation

```powershell
# Désactiver l'option "Do not require Kerberos preauthentication"
Set-ADUser -Identity "utilisateur_vulnerable" -DoesNotRequirePreAuth $false

# Script de remédiation en masse (avec confirmation)
$vulnerableUsers = Get-ADUser -Filter {DoesNotRequirePreAuth -eq $true}

foreach ($user in $vulnerableUsers) {
    Write-Host "Correction de : $($user.SamAccountName)" -ForegroundColor Yellow

    # Demander confirmation
    $confirm = Read-Host "Désactiver DoesNotRequirePreAuth ? (O/N)"
    if ($confirm -eq 'O') {
        Set-ADUser -Identity $user -DoesNotRequirePreAuth $false
        Write-Host "  ✅ Corrigé" -ForegroundColor Green
    } else {
        Write-Host "  ⏭️ Ignoré" -ForegroundColor Gray
    }
}
```

### 1.4 Détection

Événements Windows à surveiller :

| Event ID | Description | Signification |
|----------|-------------|---------------|
| **4768** | A Kerberos authentication ticket (TGT) was requested | Normal, mais surveiller les patterns |
| **4768** avec `PreAuthType = 0` | TGT demandé sans pré-auth | **Potentiel ASREPRoast** |

```powershell
# Requête pour détecter les AS-REQ sans pré-auth (sur les DC)
Get-WinEvent -FilterHashtable @{
    LogName = 'Security'
    Id = 4768
} | Where-Object {
    $_.Message -match "Pre-Authentication Type:\s+0"
} | Select-Object TimeCreated, Message -First 10
```

---

## 2. Kerberoasting

### 2.1 Mécanisme de l'Attaque

!!! danger "Niveau de Risque : CRITIQUE"
    Kerberoasting cible les **comptes de service** avec un SPN (Service Principal Name). Un attaquant avec un simple compte utilisateur du domaine peut demander des tickets de service et les cracker offline.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ATTAQUE KERBEROASTING                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Attaquant]              [KDC/DC]              [Service]      │
│   (User du domaine)                              (SQL, IIS...)  │
│      │                         │                     │          │
│      │  1. TGS-REQ             │                     │          │
│      │     "Je veux un ticket  │                     │          │
│      │      pour MSSQLSvc/..." │                     │          │
│      │────────────────────────►│                     │          │
│      │                         │                     │          │
│      │  2. TGS-REP             │                     │          │
│      │     Ticket chiffré avec │                     │          │
│      │     le hash du compte   │                     │          │
│      │     de service          │                     │          │
│      │◄────────────────────────│                     │          │
│      │                         │                     │          │
│   ❌ Le KDC donne le ticket à N'IMPORTE QUEL user du domaine    │
│                                                                 │
│   3. Attaquant extrait le hash du TGS                           │
│   4. Crack offline avec Hashcat                                 │
│   5. Mot de passe du compte de service récupéré                 │
│   6. Souvent = compte avec privilèges élevés !                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Identifier les Comptes Vulnérables

```powershell
# Lister tous les comptes utilisateurs avec un SPN
Get-ADUser -Filter {ServicePrincipalName -like "*"} -Properties ServicePrincipalName, PasswordLastSet, Enabled |
    Select-Object Name,
                  SamAccountName,
                  Enabled,
                  PasswordLastSet,
                  @{N='SPN';E={$_.ServicePrincipalName -join ', '}}

# Version détaillée avec évaluation du risque
Get-ADUser -Filter {ServicePrincipalName -like "*"} -Properties * |
    Select-Object Name,
                  SamAccountName,
                  Enabled,
                  @{N='PasswordAge';E={((Get-Date) - $_.PasswordLastSet).Days}},
                  @{N='IsAdmin';E={$_.MemberOf -match 'Admin'}},
                  @{N='SPNCount';E={$_.ServicePrincipalName.Count}},
                  @{N='SPNs';E={$_.ServicePrincipalName -join '; '}} |
    Sort-Object IsAdmin -Descending
```

!!! warning "Comptes à Haut Risque"
    Les comptes de service les plus dangereux sont ceux qui :

    - Sont membres de groupes privilégiés (Domain Admins, etc.)
    - Ont un mot de passe ancien (non changé depuis des années)
    - Utilisent un chiffrement faible (RC4 au lieu de AES)

### 2.3 Remédiation

#### Option 1 : Mots de Passe Forts et Rotation

```powershell
# Générer un mot de passe fort (25+ caractères)
$password = -join ((65..90) + (97..122) + (48..57) + (33..47) | Get-Random -Count 30 | ForEach-Object {[char]$_})

# Changer le mot de passe du compte de service
Set-ADAccountPassword -Identity "svc_sqlserver" -NewPassword (ConvertTo-SecureString $password -AsPlainText -Force) -Reset

# Documenter le nouveau mot de passe dans un vault sécurisé !
Write-Host "Nouveau mot de passe : $password" -ForegroundColor Yellow
```

#### Option 2 : Forcer AES (au lieu de RC4)

```powershell
# Activer uniquement AES pour le compte de service
Set-ADUser -Identity "svc_sqlserver" -KerberosEncryptionType AES128, AES256

# Vérifier le type de chiffrement
Get-ADUser -Identity "svc_sqlserver" -Properties msDS-SupportedEncryptionTypes |
    Select-Object Name, @{N='EncryptionTypes';E={$_.'msDS-SupportedEncryptionTypes'}}

# Mapping des valeurs :
# 0x1 = DES-CBC-CRC
# 0x2 = DES-CBC-MD5
# 0x4 = RC4-HMAC
# 0x8 = AES128-CTS-HMAC-SHA1-96
# 0x10 = AES256-CTS-HMAC-SHA1-96
```

#### Option 3 : gMSA (Group Managed Service Accounts)

!!! tip "Solution Recommandée : gMSA"
    Les **gMSA** sont des comptes de service dont le mot de passe est :

    - Généré automatiquement (240 caractères)
    - Changé automatiquement tous les 30 jours
    - Géré par Active Directory (pas d'intervention humaine)
    - **Impossible à cracker** (mot de passe trop complexe)

```powershell
# Prérequis : Créer la clé racine KDS (une seule fois par forêt)
# ATTENTION : En production, ne pas utiliser -EffectiveImmediately
Add-KdsRootKey -EffectiveTime ((Get-Date).AddHours(-10))

# Créer un gMSA
New-ADServiceAccount -Name "gmsa_sqlserver" `
    -DNSHostName "gmsa_sqlserver.corp.local" `
    -PrincipalsAllowedToRetrieveManagedPassword "SQL-Servers" `
    -KerberosEncryptionType AES128, AES256

# Installer le gMSA sur le serveur cible
Install-ADServiceAccount -Identity "gmsa_sqlserver"

# Tester l'installation
Test-ADServiceAccount -Identity "gmsa_sqlserver"
# Doit retourner True
```

### 2.4 Détection

| Event ID | Description | Signification |
|----------|-------------|---------------|
| **4769** | A Kerberos service ticket was requested | Normal |
| **4769** avec chiffrement RC4 | TGS avec RC4 (faible) | **Potentiel Kerberoasting** |
| Multiple **4769** pour différents SPNs | Enumération de SPNs | **Probable Kerberoasting** |

```powershell
# Détecter les demandes TGS avec RC4 (0x17 = RC4)
Get-WinEvent -FilterHashtable @{
    LogName = 'Security'
    Id = 4769
} | Where-Object {
    $_.Message -match "Ticket Encryption Type:\s+0x17"
} | Select-Object TimeCreated,
    @{N='User';E={($_.Message -split '\n' | Select-String 'Account Name:')[0] -replace '.*:\s+'}},
    @{N='Service';E={($_.Message -split '\n' | Select-String 'Service Name:')[0] -replace '.*:\s+'}}
```

---

## 3. Pass-the-Hash (PtH)

### 3.1 Mécanisme de l'Attaque

!!! danger "Niveau de Risque : CRITIQUE"
    Pass-the-Hash permet de s'authentifier avec le **hash NTLM** d'un utilisateur **sans connaître le mot de passe en clair**.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ATTAQUE PASS-THE-HASH                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. Compromission d'un poste (phishing, exploit...)            │
│                           │                                     │
│                           ▼                                     │
│   2. Extraction des credentials en mémoire (Mimikatz)           │
│      sekurlsa::logonpasswords                                   │
│                           │                                     │
│      Output:              │                                     │
│      User: admin-tier0    │                                     │
│      NTLM: aad3b435b51404eeaad3b435b51404ee:31d6cfe0d16ae931b7  │
│                           │                                     │
│                           ▼                                     │
│   3. Utilisation du hash pour s'authentifier                    │
│      sekurlsa::pth /user:admin-tier0 /domain:corp.local         │
│                    /ntlm:31d6cfe0d16ae931b7...                  │
│                           │                                     │
│                           ▼                                     │
│   4. Connexion aux ressources comme admin-tier0                 │
│      → Contrôleur de domaine accessible !                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Défenses

```powershell
# 1. Activer Credential Guard (Windows 10/11 Enterprise)
# Via GPO ou Intune - Isole les credentials dans un environnement virtualisé

# 2. Protected Users Group
# Les membres ne peuvent pas utiliser NTLM, ni avoir leurs credentials mis en cache
Add-ADGroupMember -Identity "Protected Users" -Members "admin-tier0"

# 3. Désactiver le cache des credentials (GPO)
# Computer Configuration > Windows Settings > Security Settings >
# Local Policies > Security Options >
# "Interactive logon: Number of previous logons to cache" = 0

# 4. Activer le mode "Restricted Admin" pour RDP
# (Ne transmet pas les credentials au serveur distant)
```

---

## 4. Pass-the-Ticket (PtT)

### 4.1 Mécanisme de l'Attaque

!!! danger "Niveau de Risque : CRITIQUE"
    Pass-the-Ticket utilise des **tickets Kerberos volés** (TGT ou TGS) pour usurper l'identité d'un utilisateur.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ATTAQUE PASS-THE-TICKET                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. Extraction des tickets Kerberos de la mémoire              │
│      sekurlsa::tickets /export                                  │
│                           │                                     │
│      Output:              │                                     │
│      [0;3e7]-2-0-40e10000-admin@krbtgt~CORP.LOCAL.kirbi         │
│      (TGT de admin)       │                                     │
│                           ▼                                     │
│   2. Injection du ticket dans une nouvelle session              │
│      kerberos::ptt [0;3e7]-2-0-40e10000-admin@krbtgt~...kirbi   │
│                           │                                     │
│                           ▼                                     │
│   3. L'attaquant possède maintenant l'identité Kerberos         │
│      de l'utilisateur jusqu'à expiration du ticket              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Golden Ticket

!!! danger "L'Attaque Ultime : Golden Ticket"
    Avec le hash du compte **krbtgt**, un attaquant peut forger des TGT **valides pour n'importe qui**, y compris des comptes inexistants avec des privilèges arbitraires.

    Le Golden Ticket reste valide jusqu'au **double changement** du mot de passe krbtgt.

```powershell
# Changer le mot de passe krbtgt (à faire 2 fois, espacé de 10+ heures)
# Script officiel Microsoft : Reset-KrbtgtKeyInteractive.ps1

# Vérifier la date du dernier changement
Get-ADUser -Identity "krbtgt" -Properties PasswordLastSet |
    Select-Object Name, PasswordLastSet
```

### 4.3 Défenses

```powershell
# 1. Réduire la durée de vie des TGT (défaut = 10 heures)
# GPO : Computer Configuration > Windows Settings > Security Settings >
# Account Policies > Kerberos Policy > Maximum lifetime for user ticket = 4 heures

# 2. Rotation régulière du mot de passe krbtgt
# Microsoft recommande tous les 180 jours minimum

# 3. Surveiller les événements de création de tickets anormaux
# Event ID 4768 avec des SID inhabituels ou des durées de vie anormales
```

---

## 5. Tableau Récapitulatif des Défenses

| Attaque | Défense Principale | Défense Complémentaire | Détection |
|---------|-------------------|------------------------|-----------|
| **ASREPRoast** | Activer pré-auth Kerberos | Mots de passe forts | Event 4768 PreAuthType=0 |
| **Kerberoasting** | gMSA | AES + rotation mots de passe | Event 4769 RC4 |
| **Pass-the-Hash** | Credential Guard | Protected Users, Tiering | Event 4624 LogonType 3 |
| **Pass-the-Ticket** | Réduire TTL tickets | Rotation krbtgt | Event 4768 anormal |
| **Golden Ticket** | Rotation krbtgt x2 | Monitoring SIEM | SID anormaux dans tickets |

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Quelle est la différence entre ASREPRoast et Kerberoasting ?"
    **Réponse :**

    - **ASREPRoast** cible les comptes avec l'option "Do not require Kerberos preauthentication" activée. L'attaquant obtient un AS-REP chiffré avec le hash du mot de passe de l'utilisateur.

    - **Kerberoasting** cible les comptes de service avec un SPN. L'attaquant, authentifié comme n'importe quel utilisateur du domaine, demande des TGS chiffrés avec le hash du compte de service.

??? question "Question 2 : Pourquoi les gMSA sont-ils immunisés contre le Kerberoasting ?"
    **Réponse :** Les gMSA utilisent des mots de passe de **240 caractères** générés et changés automatiquement par AD. Même si un attaquant obtient un ticket de service, le crack offline prendrait des milliards d'années, rendant l'attaque inutile.

??? question "Question 3 : Pourquoi faut-il changer le mot de passe krbtgt DEUX fois ?"
    **Réponse :** Le compte krbtgt conserve le hash actuel ET le hash précédent pour assurer la continuité de service (les tickets existants restent valides). Un Golden Ticket forgé avec l'ancien hash resterait donc valide après un seul changement. Il faut deux changements (espacés de ~10h pour laisser les anciens tickets expirer) pour invalider complètement un Golden Ticket.

---

## Prochaine Étape

Vous connaissez maintenant les principales attaques AD. Apprenez à auditer et durcir votre environnement.

[:octicons-arrow-right-24: Module 3 : Audit et Politiques de Mots de Passe](03-audit-and-pso.md)

---

**Temps estimé :** 75 minutes
**Niveau :** Avancé
