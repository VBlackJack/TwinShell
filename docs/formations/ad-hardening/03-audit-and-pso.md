# Module 3 : Audit et Politiques de Mots de Passe

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Auditer votre environnement AD avec PingCastle
- :material-check: Interpréter les résultats et prioriser les remédiations
- :material-check: Créer des politiques de mots de passe différenciées (PSO/FGPP)
- :material-check: Appliquer des exigences strictes aux comptes privilégiés

---

## 1. Audit avec PingCastle

### 1.1 Présentation de l'Outil

!!! info "PingCastle - L'Outil de Référence"
    **PingCastle** est un outil d'audit Active Directory développé par Vincent Le Toux. Il est :

    - **Gratuit** pour un usage interne (licence commerciale pour les audits clients)
    - **Portable** : Exécutable unique, pas d'installation
    - **Rapide** : Scan complet en quelques minutes
    - **Complet** : Évalue 100+ points de contrôle de sécurité

    C'est l'outil recommandé par l'**ANSSI** pour l'évaluation de la sécurité AD.

### 1.2 Installation et Exécution

```powershell
# Téléchargement (depuis un poste membre du domaine)
# https://www.pingcastle.com/download/

# Exécution basique (génère un rapport HTML)
.\PingCastle.exe --healthcheck

# Exécution avec toutes les vérifications
.\PingCastle.exe --healthcheck --level Full

# Scanner un domaine spécifique
.\PingCastle.exe --healthcheck --server dc01.corp.local

# Mode interactif (menu)
.\PingCastle.exe
```

### 1.3 Comprendre le Score

PingCastle génère un **score de risque** de 0 à 100 :

```
┌─────────────────────────────────────────────────────────────────┐
│                    SCORE PINGCASTLE                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Score Global : Combinaison des 4 catégories                   │
│                                                                 │
│   ┌─────────────┬─────────────┬─────────────┬─────────────┐     │
│   │  Stale      │ Privileged  │   Trust     │  Anomalies  │     │
│   │  Objects    │  Accounts   │             │             │     │
│   ├─────────────┼─────────────┼─────────────┼─────────────┤     │
│   │  Objets     │  Comptes    │  Relations  │  Config     │     │
│   │  obsolètes  │  à hauts    │  de         │  anormale   │     │
│   │             │  privilèges │  confiance  │             │     │
│   └─────────────┴─────────────┴─────────────┴─────────────┘     │
│                                                                 │
│   Interprétation :                                              │
│   • 0-10   : Excellent (rare en production)                     │
│   • 10-30  : Bon niveau de sécurité                             │
│   • 30-50  : Améliorations nécessaires                          │
│   • 50-70  : Risques significatifs                              │
│   • 70-100 : Situation critique                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.4 Catégories de Risques

| Catégorie | Description | Exemples de Problèmes |
|-----------|-------------|----------------------|
| **Stale Objects** | Objets obsolètes ou dormants | Comptes inactifs, ordinateurs non utilisés, GPO orphelines |
| **Privileged Accounts** | Gestion des comptes privilégiés | Trop de Domain Admins, mots de passe anciens, SIDHistory |
| **Trust** | Relations de confiance | Trusts bidirectionnels non sécurisés, SID Filtering désactivé |
| **Anomalies** | Configurations dangereuses | SMBv1 activé, LDAP non signé, comptes sans pré-auth |

### 1.5 Analyse d'un Rapport

```
┌─────────────────────────────────────────────────────────────────┐
│                 EXEMPLE DE RAPPORT PINGCASTLE                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Domain: CORP.LOCAL                                            │
│   Score: 65/100 (Risques significatifs)                         │
│                                                                 │
│   ┌────────────────────────────────────────────────────────┐    │
│   │ CRITIQUES (à traiter immédiatement)                    │    │
│   ├────────────────────────────────────────────────────────┤    │
│   │ • S-ADM-DomainAdmin: 15 membres Domain Admins          │    │
│   │   → Recommandé : < 5                                   │    │
│   │                                                        │    │
│   │ • S-Kerberos-PreAuth: 3 comptes sans pré-auth          │    │
│   │   → Vulnérable à ASREPRoast                            │    │
│   │                                                        │    │
│   │ • S-SMB-v1: SMBv1 activé sur 45 machines               │    │
│   │   → Vulnérable à EternalBlue/WannaCry                  │    │
│   └────────────────────────────────────────────────────────┘    │
│                                                                 │
│   ┌────────────────────────────────────────────────────────┐    │
│   │ IMPORTANTS (à planifier)                               │    │
│   ├────────────────────────────────────────────────────────┤    │
│   │ • S-OldComputer: 234 ordinateurs inactifs > 90 jours   │    │
│   │ • S-OldUser: 156 utilisateurs inactifs > 180 jours     │    │
│   │ • S-Kerberoast: 12 comptes de service avec SPN         │    │
│   └────────────────────────────────────────────────────────┘    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.6 Remédiation Prioritaire

!!! tip "Ordre de Remédiation Recommandé"

    1. **Critique - Semaine 1** :
       - Désactiver SMBv1
       - Activer pré-authentification Kerberos
       - Réduire le nombre de Domain Admins

    2. **Important - Mois 1** :
       - Nettoyer les objets obsolètes
       - Migrer les comptes de service vers gMSA
       - Activer LDAP Signing

    3. **Moyen - Trimestre 1** :
       - Implémenter le Tiering Model
       - Déployer LAPS
       - Configurer les PSO

---

## 2. Fine-Grained Password Policies (FGPP/PSO)

### 2.1 Pourquoi les PSO ?

!!! info "Limitation de la Default Domain Policy"
    Par défaut, Active Directory ne permet qu'**une seule politique de mot de passe** par domaine (via la Default Domain Policy).

    Problème : Les exigences sont différentes selon les populations :

    - **Utilisateurs standard** : Complexité moyenne
    - **Administrateurs Tier 0** : Complexité maximale
    - **Comptes de service** : Mot de passe très long, jamais expiré

    **Solution** : Les **PSO (Password Settings Objects)** permettent des politiques différenciées **sans créer plusieurs domaines**.

### 2.2 Prérequis

```powershell
# Vérifier le niveau fonctionnel du domaine (minimum 2008)
(Get-ADDomain).DomainMode

# Les PSO sont stockés dans le conteneur System
Get-ADObject -SearchBase "CN=Password Settings Container,CN=System,DC=corp,DC=local" -Filter *
```

### 2.3 Création d'une PSO pour les Administrateurs Tier 0

```powershell
# PSO pour les comptes Tier 0 - Exigences maximales
New-ADFineGrainedPasswordPolicy -Name "PSO-Tier0-Admins" `
    -DisplayName "Politique Tier 0 - Administrateurs" `
    -Description "Politique de mot de passe renforcée pour les comptes Tier 0" `
    -Precedence 10 `
    -MinPasswordLength 16 `
    -PasswordHistoryCount 24 `
    -ComplexityEnabled $true `
    -ReversibleEncryptionEnabled $false `
    -MaxPasswordAge "90.00:00:00" `
    -MinPasswordAge "1.00:00:00" `
    -LockoutThreshold 3 `
    -LockoutDuration "00:30:00" `
    -LockoutObservationWindow "00:30:00" `
    -ProtectedFromAccidentalDeletion $true
```

**Explication des paramètres :**

| Paramètre | Valeur | Justification |
|-----------|--------|---------------|
| `Precedence` | 10 | Plus le nombre est bas, plus la priorité est haute |
| `MinPasswordLength` | 16 | Résistance au crack offline |
| `PasswordHistoryCount` | 24 | Empêche la réutilisation sur 2 ans |
| `MaxPasswordAge` | 90 jours | Renouvellement trimestriel |
| `LockoutThreshold` | 3 | Verrouillage après 3 échecs |
| `LockoutDuration` | 30 min | Temps de verrouillage |

### 2.4 Création d'une PSO pour les Comptes de Service

```powershell
# PSO pour les comptes de service (non-gMSA)
New-ADFineGrainedPasswordPolicy -Name "PSO-ServiceAccounts" `
    -DisplayName "Politique Comptes de Service" `
    -Description "Mots de passe longs, pas d'expiration (rotation manuelle planifiée)" `
    -Precedence 20 `
    -MinPasswordLength 25 `
    -PasswordHistoryCount 12 `
    -ComplexityEnabled $true `
    -ReversibleEncryptionEnabled $false `
    -MaxPasswordAge "0.00:00:00" `
    -MinPasswordAge "0.00:00:00" `
    -LockoutThreshold 0 `
    -ProtectedFromAccidentalDeletion $true
```

!!! warning "Comptes de Service et Expiration"
    Pour les comptes de service legacy (non-gMSA), on désactive l'expiration (`MaxPasswordAge = 0`) pour éviter les interruptions de service.

    **Compensation obligatoire** : Planifier une rotation manuelle annuelle et documenter la procédure.

### 2.5 Application des PSO

```powershell
# Créer un groupe pour les comptes Tier 0
New-ADGroup -Name "Tier0-PasswordPolicy" `
    -GroupScope Global `
    -GroupCategory Security `
    -Path "OU=Groups,OU=Tier0,DC=corp,DC=local" `
    -Description "Membres soumis à la PSO Tier 0"

# Ajouter les administrateurs au groupe
Add-ADGroupMember -Identity "Tier0-PasswordPolicy" -Members "admin-t0-alice", "admin-t0-bob"

# Appliquer la PSO au groupe
Add-ADFineGrainedPasswordPolicySubject -Identity "PSO-Tier0-Admins" `
    -Subjects "Tier0-PasswordPolicy"

# Vérifier l'application
Get-ADFineGrainedPasswordPolicySubject -Identity "PSO-Tier0-Admins"
```

### 2.6 Vérification et Diagnostic

```powershell
# Voir quelle PSO s'applique à un utilisateur
Get-ADUserResultantPasswordPolicy -Identity "admin-t0-alice"

# Lister toutes les PSO du domaine
Get-ADFineGrainedPasswordPolicy -Filter * |
    Select-Object Name, Precedence, MinPasswordLength, MaxPasswordAge,
                  @{N='AppliesTo';E={(Get-ADFineGrainedPasswordPolicySubject -Identity $_.Name).Name -join ', '}} |
    Sort-Object Precedence

# Vérifier qu'aucun utilisateur privilégié n'échappe à une PSO
Get-ADGroupMember -Identity "Domain Admins" -Recursive |
    ForEach-Object {
        $pso = Get-ADUserResultantPasswordPolicy -Identity $_.SamAccountName
        [PSCustomObject]@{
            User = $_.SamAccountName
            PSO = if ($pso) { $pso.Name } else { "DEFAULT DOMAIN POLICY" }
        }
    }
```

---

## 3. Tableau Comparatif des PSO

| Population | Min Length | Max Age | Lockout | Complexité |
|------------|------------|---------|---------|------------|
| **Utilisateurs Standard** | 12 | 365 jours | 5 / 15 min | Oui |
| **Administrateurs Tier 0** | 16 | 90 jours | 3 / 30 min | Oui |
| **Administrateurs Tier 1** | 14 | 180 jours | 3 / 30 min | Oui |
| **Comptes de Service** | 25 | Jamais* | Désactivé | Oui |
| **gMSA** | 240 (auto) | 30 jours (auto) | N/A | Auto |

*Avec rotation manuelle documentée

---

## 4. Autres Outils de Durcissement

### 4.1 LAPS (Local Administrator Password Solution)

!!! tip "LAPS - Gestion des Mots de Passe Locaux"
    **LAPS** génère des mots de passe uniques pour le compte Administrateur local de chaque machine et les stocke dans AD.

    Avantages :
    - Mot de passe différent par machine
    - Rotation automatique
    - Pas de Pass-the-Hash entre machines

```powershell
# Installation de LAPS (Windows LAPS natif depuis Server 2019+)
# Étendre le schéma AD (une seule fois)
Update-LapsADSchema

# Activer LAPS sur une OU
Set-LapsADComputerSelfPermission -Identity "OU=Workstations,OU=Tier2,DC=corp,DC=local"

# Configurer via GPO :
# Computer Configuration > Administrative Templates > System > LAPS
# - Configure password backup directory : Active Directory
# - Password Settings : Length 20, Complexity High, Age 30 days

# Récupérer le mot de passe d'une machine
Get-LapsADPassword -Identity "PC-JDUPONT" -AsPlainText
```

### 4.2 AdminSDHolder et Protected Users

```powershell
# Vérifier les membres du groupe Protected Users
Get-ADGroupMember -Identity "Protected Users"

# Ajouter un compte Tier 0 au groupe Protected Users
Add-ADGroupMember -Identity "Protected Users" -Members "admin-t0-alice"

# Effets du groupe Protected Users :
# - Pas de délégation Kerberos
# - Pas de cache de credentials
# - Pas de NTLM
# - TGT de 4 heures max
```

### 4.3 Surveillance Continue

```powershell
# Script de monitoring quotidien
$report = @()

# Vérifier les nouveaux Domain Admins
$domainAdmins = Get-ADGroupMember -Identity "Domain Admins" -Recursive
$report += "Domain Admins: $($domainAdmins.Count) membres"

# Vérifier les comptes sans pré-auth Kerberos
$noPreAuth = Get-ADUser -Filter {DoesNotRequirePreAuth -eq $true}
$report += "Comptes sans pré-auth: $($noPreAuth.Count)"

# Vérifier les comptes de service avec SPN
$kerberoastable = Get-ADUser -Filter {ServicePrincipalName -like "*"} -Properties ServicePrincipalName
$report += "Comptes Kerberoastable: $($kerberoastable.Count)"

# Afficher le rapport
$report | ForEach-Object { Write-Host $_ }
```

---

## 5. Checklist de Durcissement AD

### Phase 1 : Actions Immédiates (Semaine 1)

- [ ] Exécuter PingCastle et analyser le rapport
- [ ] Réduire le nombre de Domain Admins (< 5)
- [ ] Activer la pré-authentification Kerberos pour tous les comptes
- [ ] Désactiver SMBv1 sur tous les systèmes
- [ ] Ajouter les admins critiques au groupe "Protected Users"

### Phase 2 : Durcissement (Mois 1)

- [ ] Créer et appliquer les PSO (Tier 0, Tier 1, Services)
- [ ] Déployer LAPS sur les postes de travail
- [ ] Migrer les comptes de service vers gMSA
- [ ] Créer la structure OU pour le Tiering

### Phase 3 : Architecture (Trimestre 1)

- [ ] Implémenter les GPO de restriction de connexion inter-Tiers
- [ ] Déployer les PAW ou Jump Servers pour Tier 0
- [ ] Activer le LDAP Signing et Channel Binding
- [ ] Configurer l'audit avancé (Event IDs 4768, 4769, 4624)

### Phase 4 : Maintenance Continue

- [ ] Exécuter PingCastle mensuellement
- [ ] Nettoyer les objets obsolètes trimestriellement
- [ ] Changer le mot de passe krbtgt semestriellement
- [ ] Former les équipes annuellement

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Quelle est la différence entre la Default Domain Policy et une PSO ?"
    **Réponse :**

    - **Default Domain Policy** : S'applique à **tous** les utilisateurs du domaine. Une seule politique par domaine. Définie via GPO.

    - **PSO (Password Settings Object)** : S'applique à des **groupes ou utilisateurs spécifiques**. Plusieurs PSO possibles avec des priorités (Precedence). Définie via ADAC ou PowerShell.

    Les PSO permettent d'avoir des exigences différentes selon les populations sans créer plusieurs domaines.

??? question "Question 2 : Pourquoi le score PingCastle de 0 est-il rare en production ?"
    **Réponse :** Un score de 0 signifie une configuration "parfaite" selon tous les critères PingCastle. En production :

    - Les applications legacy nécessitent souvent des exceptions
    - L'historique du domaine laisse des objets obsolètes
    - Les trusts inter-forêts ajoutent de la complexité
    - Certaines fonctionnalités requises sont considérées risquées

    Un score de 10-30 est un objectif réaliste pour un environnement de production bien géré.

??? question "Question 3 : Pourquoi désactiver le lockout pour les comptes de service ?"
    **Réponse :** Les comptes de service s'authentifient automatiquement de nombreuses fois. Un attaquant pourrait :

    1. Déclencher volontairement des échecs d'authentification
    2. Verrouiller le compte de service
    3. Provoquer un **déni de service** sur l'application

    Alternative recommandée : Utiliser des **gMSA** qui n'ont pas ce problème (authentification automatique gérée par AD).

---

## Félicitations !

Vous avez terminé la formation **Durcissement Active Directory**. Vous maîtrisez maintenant :

- :material-check-circle: L'architecture de sécurité en couches (Tiering Model)
- :material-check-circle: Les principales attaques AD et leurs défenses
- :material-check-circle: L'audit avec PingCastle
- :material-check-circle: Les politiques de mots de passe différenciées (PSO)

[:octicons-arrow-left-24: Retour au Module 2 : Vecteurs d'Attaque](02-attack-vectors.md)

[:octicons-home: Retour à l'index de la formation](index.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Avancé
