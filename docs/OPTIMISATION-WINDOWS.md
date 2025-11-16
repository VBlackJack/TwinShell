# Guide d'optimisation Windows - TwinShell

Guide complet pour optimiser et nettoyer votre installation Windows à l'aide de TwinShell.

## Table des matières

1. [Debloating Windows](#debloating-windows)
2. [Confidentialité Windows](#confidentialité-windows)
3. [Précautions et recommandations](#précautions-et-recommandations)
4. [Rollback et récupération](#rollback-et-récupération)
5. [FAQ et troubleshooting](#faq-et-troubleshooting)

---

## Debloating Windows

Le debloating consiste à supprimer les applications préinstallées et composants Windows non essentiels pour:
- Libérer de l'espace disque
- Améliorer les performances système
- Réduire la consommation de ressources
- Améliorer la confidentialité

### ⚠️ AVERTISSEMENT IMPORTANT

**Les actions de debloating peuvent être irréversibles!**

Avant de procéder:
1. **Créez un point de restauration système**
2. **Sauvegardez vos données importantes**
3. **Documentez vos actions pour pouvoir les annuler si nécessaire**
4. **Testez d'abord sur une machine virtuelle ou de test**

### Catégories d'actions disponibles

TwinShell propose 22 actions de debloating organisées en 4 catégories:

#### 1. 🧹 Bloatware Tiers (3 actions)

Actions pour supprimer les applications tierces préinstallées.

| Action ID | Description | Level | Notes |
|-----------|-------------|-------|-------|
| WIN-DEBLOAT-001 | Supprimer tous les bloatwares tiers | Run | Candy Crush, Spotify, Disney+, Netflix |
| WIN-DEBLOAT-002 | Supprimer les extensions média | Run | HEIF, VP9, WebP, AV1 codecs |
| WIN-DEBLOAT-003 | Lister les applications tierces | Info | Commande d'information uniquement |

**Recommandation:** Commencez par WIN-DEBLOAT-003 pour identifier les apps installées avant de supprimer.

#### 2. 📱 Applications Microsoft (5 actions)

Actions pour supprimer les applications Microsoft non essentielles.

| Action ID | Description | Level | Apps concernées |
|-----------|-------------|-------|-----------------|
| WIN-DEBLOAT-101 | Supprimer les apps Microsoft inutiles | Run | 38+ apps (3D Builder, Alarms, BingNews, etc.) |
| WIN-DEBLOAT-102 | Supprimer uniquement les apps de jeux | Run | Solitaire, Candy Crush, Xbox Game Bar |
| WIN-DEBLOAT-103 | Supprimer les apps de communication | Run | Skype, People, Mail, Calendar |
| WIN-DEBLOAT-104 | Supprimer les apps météo/actualités | Run | Météo, Actualités, Sports, Finance |
| WIN-DEBLOAT-105 | Liste personnalisée d'apps | Run | Spécifier votre propre liste |

**Recommandation:** Utilisez WIN-DEBLOAT-102/103/104 pour une suppression ciblée, ou WIN-DEBLOAT-101 pour un nettoyage complet.

#### 3. ⚠️ Composants Système (6 actions) - TOUTES DANGEROUS

**ATTENTION:** Ces actions sont irréversibles et peuvent affecter les fonctionnalités Windows!

| Action ID | Description | Impact | Conséquences |
|-----------|-------------|--------|--------------|
| WIN-DEBLOAT-201 | Désinstaller Microsoft Store | 🔴 Critique | Impossible d'installer des apps du Store |
| WIN-DEBLOAT-202 | Désinstaller OneDrive (complet) | 🔴 Critique | Perte de la synchro cloud, nettoyage registre |
| WIN-DEBLOAT-203 | Désinstaller Microsoft Edge | 🔴 Critique | Certaines fonctions Windows 11 affectées |
| WIN-DEBLOAT-204 | Désinstaller Copilot | 🟡 Modéré | Perte de l'assistant IA Windows |
| WIN-DEBLOAT-205 | Supprimer Xbox (tous composants) | 🟡 Modéré | Perte Game Bar et services Xbox |
| WIN-DEBLOAT-206 | Supprimer Widgets | 🟡 Modéré | Retire les Widgets de la barre des tâches |

**Recommandation:**
- **NE PAS UTILISER** sans sauvegarde complète du système
- **Tester d'abord** sur une VM ou machine de test
- **Créer un point de restauration** avant chaque action
- **Documenter** chaque action effectuée

#### 4. ⚙️ Fonctionnalités Windows (4 actions)

Actions pour désactiver des fonctionnalités Windows optionnelles.

| Action ID | Description | Level | Impact |
|-----------|-------------|-------|--------|
| WIN-DEBLOAT-301 | Désactiver Consumer Features | Run | Bloque l'installation auto d'apps suggérées |
| WIN-DEBLOAT-302 | Désactiver Recall | Run | Désactive l'enregistrement d'activité IA |
| WIN-DEBLOAT-303 | Désactiver Internet Explorer | Run | Désactive IE11 (legacy) |
| WIN-DEBLOAT-304 | Désactiver Hyper-V | Run | Désactive la virtualisation |

**Recommandation:** WIN-DEBLOAT-301 est fortement recommandé pour éviter la réinstallation de bloatware.

#### 5. 🌐 Optimisation Edge (4 actions)

Actions pour optimiser Microsoft Edge (si vous le conservez).

| Action ID | Description | Bénéfice |
|-----------|-------------|----------|
| WIN-DEBLOAT-401 | Désactiver les recommandations Edge | Moins de distractions |
| WIN-DEBLOAT-402 | Désactiver le shopping assistant | Pas de notifications de coupons |
| WIN-DEBLOAT-403 | Désactiver la télémétrie Edge | Amélioration de la confidentialité |
| WIN-DEBLOAT-404 | Désactiver le crypto wallet | Désactive les fonctionnalités Web3 |

**Recommandation:** Appliquez toutes les actions Edge si vous utilisez Edge mais souhaitez une expérience plus "propre".

---

## Confidentialité Windows

La confidentialité Windows permet de contrôler la collecte de données, la télémétrie et les permissions des applications. Ces actions sont essentielles pour la conformité RGPD.

### 🔒 Conformité RGPD et protection des données

**TwinShell Sprint 7 - Confidentialité Windows** offre 28 actions organisées en 4 catégories pour une protection maximale de vos données personnelles conformément au RGPD (Règlement Général sur la Protection des Données).

### ⚠️ AVERTISSEMENT IMPORTANT - RGPD

Ces actions désactivent la collecte de données par Microsoft et des applications tierces. Elles sont particulièrement importantes pour:
- **Entreprises européennes** soumises au RGPD
- **Utilisateurs soucieux de leur vie privée**
- **Organisations manipulant des données sensibles**
- **Conformité aux réglementations de protection des données**

### Catégories d'actions de confidentialité

TwinShell propose 28 actions de confidentialité organisées en 4 catégories :

#### 1. 🔐 Permissions Applications (10 actions - WIN-PRIVACY-001 à 010)

Contrôle granulaire des permissions d'accès des applications Windows.

| Action ID | Description | Scope | Impact RGPD |
|-----------|-------------|-------|-------------|
| WIN-PRIVACY-001 | Désactiver l'accès localisation | CurrentUser/AllUsers | RGPD Art. 6 - Données de localisation |
| WIN-PRIVACY-002 | Désactiver l'accès caméra | CurrentUser/AllUsers | RGPD Art. 25 - Protection dès la conception |
| WIN-PRIVACY-003 | Désactiver l'accès microphone | CurrentUser/AllUsers | Protection contre écoute non autorisée |
| WIN-PRIVACY-004 | Désactiver l'accès système de fichiers | CurrentUser/AllUsers | Protection des documents sensibles |
| WIN-PRIVACY-005 | Désactiver l'accès contacts | CurrentUser/AllUsers | Protection des données personnelles de tiers |
| WIN-PRIVACY-006 | Désactiver l'accès calendrier | CurrentUser/AllUsers | Protection de la vie privée professionnelle |
| WIN-PRIVACY-007 | Désactiver l'accès emails | CurrentUser/AllUsers | Confidentialité des communications |
| WIN-PRIVACY-008 | Désactiver l'accès notifications | CurrentUser/AllUsers | Réduction du tracking |
| WIN-PRIVACY-009 | **Désactiver TOUTES les permissions** | Run | ⚠️ Configuration maximale - Level Dangerous |
| WIN-PRIVACY-010 | Restaurer les permissions par défaut | Run | Action de rollback |

**Recommandation:**
- Utilisez les actions 001-008 individuellement pour un contrôle précis
- WIN-PRIVACY-009 pour une sécurité maximale (désactive tout)
- WIN-PRIVACY-010 pour annuler si trop restrictif
- Le paramètre `Scope` permet de choisir entre CurrentUser (utilisateur actuel) ou AllUsers (tous les utilisateurs)

#### 2. ☁️ Synchronisation Cloud (6 actions - WIN-PRIVACY-101 à 106)

Contrôle de la synchronisation avec le cloud Microsoft.

| Action ID | Description | Level | Données concernées |
|-----------|-------------|-------|-------------------|
| WIN-PRIVACY-101 | Désactiver toute synchronisation cloud | Dangerous | TOUTES (paramètres, mots de passe, thèmes, navigateur) |
| WIN-PRIVACY-102 | Désactiver sync des paramètres | Run | Préférences Windows, accessibilité |
| WIN-PRIVACY-103 | Désactiver sync des thèmes | Run | Fonds d'écran, personnalisation |
| WIN-PRIVACY-104 | Désactiver sync des mots de passe | Dangerous | Identifiants, credentials |
| WIN-PRIVACY-105 | Désactiver sync du navigateur | Run | Favoris, historique, onglets Edge |
| WIN-PRIVACY-106 | Restaurer la synchronisation | Run | Action de rollback |

**Impact RGPD:**
- **Article 44 RGPD:** Empêche le transfert de données vers les serveurs Microsoft (potentiellement hors UE)
- **Article 5 RGPD:** Limitation de la collecte de données au strict nécessaire
- WIN-PRIVACY-101 recommandée pour les entreprises soumises au RGPD strict

**Recommandation:**
- WIN-PRIVACY-101 pour désactiver toute synchronisation (entreprises RGPD)
- Actions 102-105 pour un contrôle granulaire
- WIN-PRIVACY-104 particulièrement critique pour la sécurité

#### 3. 📊 Télémétrie et Tracking (8 actions - WIN-PRIVACY-201 à 208)

Désactivation de la télémétrie Windows et des services de tracking.

| Action ID | Description | Level | Clés registre modifiées |
|-----------|-------------|-------|------------------------|
| WIN-PRIVACY-201 | Désactiver Activity Feed | Run | 3+ clés (chronologie Windows) |
| WIN-PRIVACY-202 | Désactiver Game DVR | Run | 4+ clés (Xbox, enregistrement) |
| WIN-PRIVACY-203 | Désactiver notifications publicitaires | Run | 7+ clés (suggestions, ads) |
| WIN-PRIVACY-204 | Désactiver suivi de localisation | Run | 5+ clés (GPS, capteurs) |
| WIN-PRIVACY-205 | **Configuration minimale télémétrie** | Dangerous | **50+ clés + tâches planifiées** |
| WIN-PRIVACY-206 | Désactiver reconnaissance vocale cloud | Run | 8+ clés (Cortana, dictée) |
| WIN-PRIVACY-207 | Désactiver services biométriques | Dangerous | 3+ clés (Windows Hello, empreintes) |
| WIN-PRIVACY-208 | Désactiver caméra écran verrouillage | Run | 1 clé (sécurité lockscreen) |

**Détails WIN-PRIVACY-205 (Télémétrie minimale):**

Cette action est **LA PLUS IMPORTANTE** pour la conformité RGPD stricte:
- Configure la télémétrie au niveau **Security (0)** - le minimum absolu
- Désactive **50+ clés de registre** dans HKLM et HKCU
- Désactive **6+ tâches planifiées** de collecte de données:
  - Microsoft Compatibility Appraiser
  - ProgramDataUpdater
  - Customer Experience Improvement Program
  - Disk Diagnostic Data Collector
  - Et plus...
- **Niveau recommandé pour toutes les entreprises européennes**

**Impact RGPD:**
- **Article 5 RGPD:** Minimisation des données
- **Article 25 RGPD:** Protection dès la conception
- **Article 32 RGPD:** Sécurité du traitement

**Recommandation:**
- **WIN-PRIVACY-205 est OBLIGATOIRE** pour conformité RGPD entreprise
- WIN-PRIVACY-201, 203, 204, 206 fortement recommandées
- WIN-PRIVACY-207 si données biométriques sensibles (Article 9 RGPD)

#### 4. 🔧 Télémétrie Applications Tierces (4 actions - WIN-PRIVACY-301 à 304)

Désactivation de la télémétrie des applications tierces courantes.

| Action ID | Description | Applications concernées | Services désactivés |
|-----------|-------------|------------------------|---------------------|
| WIN-PRIVACY-301 | Désactiver télémétrie Adobe | Creative Cloud, Acrobat | AdobeUpdateService, AGMService, Analytics |
| WIN-PRIVACY-302 | Désactiver télémétrie VS Code | Visual Studio Code | Telemetry, Crash Reporter |
| WIN-PRIVACY-303 | Désactiver télémétrie Google | Chrome, Google Update | gupdate, gupdatem, MetricsReporting |
| WIN-PRIVACY-304 | Désactiver télémétrie Nvidia | GeForce Experience, pilotes | NvTelemetryContainer, tâches planifiées |

**Détails par application:**

**Adobe (WIN-PRIVACY-301):**
- Désactive Adobe Analytics
- Stoppe AdobeUpdateService, AGMService, AGSService
- Configure OptOut pour SuiteCloud
- Réduit utilisation réseau et CPU

**VS Code (WIN-PRIVACY-302):**
- Modifie `settings.json` utilisateur
- `telemetry.telemetryLevel: off`
- `telemetry.enableTelemetry: false`
- `telemetry.enableCrashReporter: false`

**Google Chrome (WIN-PRIVACY-303):**
- Désactive MetricsReporting
- Stoppe services gupdate/gupdatem
- Bloque ChromeCleanup reporting
- Désactive UserFeedback

**Nvidia (WIN-PRIVACY-304):**
- Stoppe NvTelemetryContainer
- Désactive tâches planifiées (CrashReport, DriverUpdateCheck)
- Configure `SendTelemetryData: 0`
- N'affecte PAS les performances graphiques

**Recommandation:**
- Appliquez les actions pour les applications que vous avez installées
- Ces actions améliorent aussi les performances (moins de services en arrière-plan)

### 🎯 Batch Prédéfini: "🔒 Confidentialité maximale"

TwinShell inclut un batch prédéfini combinant les actions les plus critiques:

**Actions incluses (8 commandes):**
1. WIN-PRIVACY-009 - Désactiver toutes les permissions applications
2. WIN-PRIVACY-101 - Désactiver toute synchronisation cloud
3. WIN-PRIVACY-205 - Configuration minimale de télémétrie (Security)
4. WIN-PRIVACY-207 - Désactiver services biométriques
5. WIN-PRIVACY-201 - Désactiver Activity Feed
6. WIN-PRIVACY-203 - Désactiver notifications publicitaires
7. WIN-PRIVACY-204 - Désactiver suivi de localisation
8. WIN-PRIVACY-206 - Désactiver reconnaissance vocale cloud

**Utilisation:**
```powershell
# Via TwinShell - Sélectionner le batch "🔒 Confidentialité maximale"
# Exécution séquentielle avec mode StopOnError
```

**Recommandation:**
- **Entreprises RGPD:** Exécutez ce batch sur tous les postes Windows
- **Utilisateurs avancés:** Configuration optimale pour vie privée maximale
- **Attention:** Certaines fonctionnalités seront désactivées (Windows Hello, synchronisation, etc.)

### 📋 Clés de registre modifiées (Liste complète)

Pour transparence et conformité RGPD, voici la liste complète des clés modifiées:

#### Permissions Applications (001-009)
```
HKCU/HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\
├── location
├── webcam
├── microphone
├── documentsLibrary
├── broadFileSystemAccess
├── contacts
├── appointments
├── email
├── userNotificationListener
├── phoneCall
├── radios
└── chat
```

#### Synchronisation Cloud (101-106)
```
HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\
├── SyncPolicy
└── Groups\
    ├── Personalization
    ├── BrowserSettings
    ├── Credentials
    ├── Accessibility
    └── Windows
```

#### Télémétrie Windows (201-208)
```
HKLM:\SOFTWARE\Policies\Microsoft\Windows\
├── System (ActivityFeed, PublishUserActivities)
├── GameDVR (AllowGameDVR)
├── LocationAndSensors (DisableLocation)
├── DataCollection (AllowTelemetry, MaxTelemetryAllowed)
├── InputPersonalization
├── Biometrics
└── Personalization (NoLockScreenCamera)

HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager\
├── SilentInstalledAppsEnabled
├── SystemPaneSuggestionsEnabled
├── SubscribedContent-*
└── (7+ clés publicitaires)
```

#### Applications Tierces (301-304)
```
Adobe:
HKLM:\SOFTWARE\Policies\Adobe\Adobe Acrobat\DC\FeatureLockDown
HKLM:\SOFTWARE\Adobe\Adobe Desktop Common\ADS
HKLM:\SOFTWARE\Adobe\SuiteCloud

Google Chrome:
HKLM:\SOFTWARE\Policies\Google\Chrome
HKLM:\SOFTWARE\Policies\Google\Update

Nvidia:
HKLM:\SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client
HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup
```

### 🔄 Export de configuration avant/après

Pour conformité RGPD, documentez vos changements:

**Avant exécution:**
```powershell
# Export complet du registre de confidentialité
$date = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\TwinShell_Backup\Privacy_$date"
New-Item -Path $backupPath -ItemType Directory -Force

# Export clés permissions
reg export "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager" "$backupPath\permissions.reg" /y

# Export clés synchronisation
reg export "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync" "$backupPath\sync.reg" /y

# Export clés télémétrie
reg export "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" "$backupPath\telemetry.reg" /y

# Liste des tâches planifiées actives
Get-ScheduledTask | Where-Object {$_.State -eq 'Ready'} | Export-Csv "$backupPath\scheduled_tasks.csv"

Write-Host "Backup créé dans: $backupPath"
```

**Après exécution:**
```powershell
# Vérification des modifications
$verifyPath = "C:\TwinShell_Backup\Privacy_Verify_$date"
New-Item -Path $verifyPath -ItemType Directory -Force

# Export post-configuration
reg export "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager" "$verifyPath\permissions_after.reg" /y
reg export "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync" "$verifyPath\sync_after.reg" /y
reg export "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" "$verifyPath\telemetry_after.reg" /y

# Comparaison avant/après
Write-Host "Comparez les fichiers .reg dans $backupPath et $verifyPath"
```

### 📊 Tests de conformité RGPD

**Tests recommandés après configuration:**

1. **Vérifier télémétrie désactivée:**
```powershell
Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -Name AllowTelemetry
# Résultat attendu: 0
```

2. **Vérifier services arrêtés:**
```powershell
Get-Service | Where-Object {$_.Name -like "*Telemetry*" -or $_.Name -like "*DiagTrack*"}
# Résultat attendu: Stopped/Disabled
```

3. **Vérifier tâches planifiées désactivées:**
```powershell
Get-ScheduledTask | Where-Object {$_.TaskName -like "*Compatibility Appraiser*"}
# Résultat attendu: Disabled
```

4. **Vérifier permissions applications:**
```powershell
Get-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location" -Name Value
# Résultat attendu: "Deny"
```

### 🏢 Recommandations par type d'organisation

#### Entreprises soumises au RGPD strict
**Actions OBLIGATOIRES:**
- WIN-PRIVACY-009 (Toutes permissions)
- WIN-PRIVACY-101 (Toute synchronisation)
- WIN-PRIVACY-205 (Télémétrie minimale)
- WIN-PRIVACY-207 (Biométrie - si données sensibles Art. 9)

**Actions RECOMMANDÉES:**
- WIN-PRIVACY-201, 203, 204, 206 (Tracking)
- WIN-PRIVACY-301-304 (Télémétrie apps tierces)

**Batch à utiliser:** "🔒 Confidentialité maximale"

#### Utilisateurs professionnels
**Actions RECOMMANDÉES:**
- WIN-PRIVACY-001, 002, 003 (Localisation, caméra, micro)
- WIN-PRIVACY-101 ou 104 (Sync cloud ou mots de passe)
- WIN-PRIVACY-205 (Télémétrie minimale)
- WIN-PRIVACY-203 (Publicités)

#### Utilisateurs personnels soucieux de leur vie privée
**Actions SUGGÉRÉES:**
- WIN-PRIVACY-001 (Localisation)
- WIN-PRIVACY-203 (Publicités)
- WIN-PRIVACY-204 (Tracking localisation)
- WIN-PRIVACY-206 (Reconnaissance vocale)
- Actions 301-304 selon apps installées

### ⚠️ Impacts et limitations

**Fonctionnalités désactivées par les actions de confidentialité:**

| Action | Fonctionnalités affectées | Alternatives |
|--------|--------------------------|--------------|
| WIN-PRIVACY-001 | GPS, localisation dans apps | Activer manuellement si besoin |
| WIN-PRIVACY-002 | Webcam pour toutes les apps | Activer pour apps spécifiques |
| WIN-PRIVACY-003 | Micro pour toutes les apps | Activer pour Teams, Zoom, etc. |
| WIN-PRIVACY-009 | TOUTES les permissions | Réactiver individuellement |
| WIN-PRIVACY-101 | Sync entre appareils | Utiliser OneDrive manuel |
| WIN-PRIVACY-104 | Sync mots de passe | Utiliser gestionnaire de mots de passe |
| WIN-PRIVACY-205 | Certaines fonctionnalités Windows Update | Fonctionne quand même |
| WIN-PRIVACY-206 | Cortana, dictée cloud | Dictée locale disponible |
| WIN-PRIVACY-207 | Windows Hello | Utiliser PIN ou mot de passe |

**Compatible avec:**
- Windows 10 (build 1809+)
- Windows 11 (toutes versions)
- Environnements Active Directory
- GPO existantes (les actions TwinShell peuvent être remplacées par GPO)

### 🔐 Conformité réglementaire

**RGPD (UE):**
- Article 5: Minimisation des données ✅
- Article 6: Licéité du traitement ✅
- Article 25: Protection dès la conception ✅
- Article 32: Sécurité du traitement ✅
- Article 44: Transfert de données hors UE ✅

**Autres réglementations:**
- **CCPA (Californie):** Contrôle des données personnelles
- **PIPEDA (Canada):** Protection des renseignements personnels
- **DPA 2018 (UK):** UK Data Protection Act

**Audit et traçabilité:**
- Toutes les actions sont loggées
- Export de configuration possible
- Comparaison avant/après disponible
- Conformité aux exigences d'audit RGPD

---

## Précautions et recommandations

### Avant de commencer

#### 1. Créer un point de restauration système

```powershell
# Via TwinShell ou manuellement
Enable-ComputerRestore -Drive "C:\"
Checkpoint-Computer -Description "Avant debloating TwinShell" -RestorePointType "MODIFY_SETTINGS"
```

#### 2. Sauvegarder le registre

Les actions de debloating modifient le registre Windows. Sauvegardez-le:

```powershell
# Exporter le registre complet
reg export HKLM C:\Backup\HKLM_backup.reg /y
reg export HKCU C:\Backup\HKCU_backup.reg /y
```

#### 3. Documenter votre configuration

Avant de supprimer des apps, listez-les:

```powershell
# Utiliser WIN-DEBLOAT-003
Get-AppxPackage | Select-Object Name,Version,Publisher | Export-Csv C:\Backup\installed_apps.csv
```

### Ordre recommandé d'exécution

Pour un debloating complet et sécurisé:

1. **Information** → WIN-DEBLOAT-003 (lister les apps tierces)
2. **Bloatware tiers** → WIN-DEBLOAT-001 (supprimer bloatware)
3. **Apps Microsoft** → WIN-DEBLOAT-101/102/103/104 (au choix)
4. **Fonctionnalités** → WIN-DEBLOAT-301 (désactiver Consumer Features)
5. **Optimisation Edge** → WIN-DEBLOAT-401/402/403/404 (si Edge conservé)
6. **Composants système** → WIN-DEBLOAT-201-206 (**DANGER - uniquement si nécessaire**)

### Profils d'utilisation recommandés

#### Profil "Sécurisé" (Recommandé pour la plupart des utilisateurs)

Actions à exécuter:
- WIN-DEBLOAT-001 (Bloatware tiers)
- WIN-DEBLOAT-102 (Apps de jeux)
- WIN-DEBLOAT-104 (Apps météo/actualités)
- WIN-DEBLOAT-301 (Consumer Features)
- WIN-DEBLOAT-401/402/403 (Optimisation Edge)

Gain estimé: 2-5 GB d'espace, amélioration modérée des performances.

#### Profil "Avancé" (Pour utilisateurs expérimentés)

Actions supplémentaires:
- WIN-DEBLOAT-101 (Toutes les apps Microsoft)
- WIN-DEBLOAT-302 (Recall)
- WIN-DEBLOAT-303 (Internet Explorer)
- WIN-DEBLOAT-206 (Widgets)

Gain estimé: 5-10 GB d'espace, amélioration significative des performances.

#### Profil "Extrême" (⚠️ DANGER - Experts uniquement)

Actions supplémentaires:
- WIN-DEBLOAT-202 (OneDrive)
- WIN-DEBLOAT-204 (Copilot)
- WIN-DEBLOAT-205 (Xbox)

**NE PAS UTILISER:**
- WIN-DEBLOAT-201 (Microsoft Store) - Très difficile à restaurer
- WIN-DEBLOAT-203 (Microsoft Edge) - Peut casser Windows 11

Gain estimé: 10-15 GB d'espace, performances maximales, **mais risque élevé**.

---

## Rollback et récupération

### Méthodes de récupération par ordre de préférence

#### 1. Restauration système (Recommandé)

Si vous avez créé un point de restauration:

```powershell
# Lister les points de restauration
Get-ComputerRestorePoint

# Restaurer (via l'interface graphique)
rstrui.exe
```

#### 2. Réinstallation via Microsoft Store

Pour les apps Microsoft supprimées:

```powershell
# Réinstaller une app spécifique (si Store disponible)
Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*AppName*"} | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml"}
```

#### 3. Réinstallation de composants critiques

##### Microsoft Store

```powershell
# Via PowerShell (nécessite connexion Internet)
wsreset.exe
Get-AppxPackage *WindowsStore* -AllUsers | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml"}
```

##### OneDrive

```powershell
# Télécharger et réinstaller OneDrive
Start-Process "https://go.microsoft.com/fwlink/?linkid=844652"
```

##### Microsoft Edge

```powershell
# Télécharger Edge depuis le site officiel
Start-Process "https://www.microsoft.com/edge"
```

#### 4. Annulation de modifications registre

Si vous avez sauvegardé le registre:

```powershell
# Restaurer une clé spécifique
reg import C:\Backup\HKLM_backup.reg
reg import C:\Backup\HKCU_backup.reg
```

#### 5. Réparation Windows (Dernier recours)

Si le système est instable:

```powershell
# Scan et réparation des fichiers système
sfc /scannow
DISM /Online /Cleanup-Image /RestoreHealth
```

Ou via les Paramètres Windows:
- Paramètres → Mise à jour et sécurité → Récupération → Réinitialiser ce PC

---

## FAQ et troubleshooting

### Questions fréquentes

#### Q: Quelles actions sont réversibles?

**R:** Les actions de niveau "Info" et "Run" sont généralement réversibles:
- Les apps peuvent être réinstallées via le Microsoft Store
- Les paramètres de registre peuvent être restaurés
- Les fonctionnalités Windows peuvent être réactivées

Les actions "Dangerous" (niveau 2) sont **difficilement** réversibles.

#### Q: Puis-je réinstaller les apps supprimées?

**R:** Oui, si le Microsoft Store est encore installé:
- Ouvrez le Microsoft Store
- Recherchez l'application
- Cliquez sur "Installer"

**Attention:** WIN-DEBLOAT-201 supprime le Store lui-même!

#### Q: Les actions de debloating affectent-elles les mises à jour Windows?

**R:** Non, les mises à jour Windows fonctionnent normalement. Cependant:
- Certaines apps supprimées peuvent être réinstallées lors de mises à jour majeures
- Utilisez WIN-DEBLOAT-301 (Consumer Features) pour éviter cela

#### Q: Quelle est la différence entre "supprimer" et "désactiver"?

**R:**
- **Supprimer** (Remove-AppxPackage): Désinstalle complètement l'application
- **Désactiver** (registre): L'application reste installée mais inactive

Les désactivations sont plus faciles à annuler.

#### Q: Combien d'espace disque puis-je libérer?

**R:** Dépend du profil choisi:
- Profil "Sécurisé": 2-5 GB
- Profil "Avancé": 5-10 GB
- Profil "Extrême": 10-15 GB

#### Q: Le debloating améliore-t-il vraiment les performances?

**R:** Oui, mais l'impact dépend de votre matériel:
- **Disque:** Moins d'apps = plus d'espace libre
- **RAM:** Moins de services en arrière-plan
- **CPU:** Moins de processus actifs
- **Réseau:** Moins de télémétrie et synchronisation

Sur un PC moderne, l'amélioration est modérée. Sur un PC ancien, elle peut être significative.

### Problèmes courants

#### Problème: "Accès refusé" lors de l'exécution

**Solution:**
```powershell
# Exécuter PowerShell en tant qu'administrateur
# Clic droit sur l'icône PowerShell → "Exécuter en tant qu'administrateur"
```

#### Problème: L'application n'a pas été supprimée

**Causes possibles:**
1. L'app est en cours d'exécution
2. L'app est protégée par le système
3. Permissions insuffisantes

**Solution:**
```powershell
# 1. Fermer tous les processus liés
Get-Process | Where-Object {$_.Name -like "*AppName*"} | Stop-Process -Force

# 2. Essayer avec -AllUsers
Get-AppxPackage *AppName* -AllUsers | Remove-AppxPackage

# 3. Utiliser l'option provisioned (empêche la réinstallation)
Get-AppxProvisionedPackage -Online | Where-Object {$_.DisplayName -like "*AppName*"} | Remove-AppxProvisionedPackage -Online
```

#### Problème: Le Microsoft Store ne fonctionne plus

**Solution:**
```powershell
# Réinitialiser le cache du Store
wsreset.exe

# Réenregistrer le Store
Get-AppxPackage *WindowsStore* | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml"}
```

#### Problème: OneDrive continue de démarrer

**Solution:**
```powershell
# Désactiver OneDrive au démarrage
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v OneDrive /f
reg delete "HKLM\Software\Microsoft\Windows\CurrentVersion\Run" /v OneDrive /f

# Désactiver OneDrive dans l'Explorateur
reg add "HKCR\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}" /v System.IsPinnedToNameSpaceTree /t REG_DWORD /d 0 /f
```

#### Problème: Windows 11 devient instable après suppression d'Edge

**Solution:**
⚠️ **C'est pourquoi WIN-DEBLOAT-203 est DANGEROUS!**

Windows 11 utilise Edge pour certaines fonctionnalités système. Options:
1. Restaurer le point de restauration système
2. Réinstaller Edge depuis le site officiel
3. Utiliser les actions WIN-DEBLOAT-401-404 au lieu de supprimer Edge

#### Problème: Les widgets reviennent après chaque mise à jour

**Solution:**
```powershell
# Désactiver via GPO (plus persistant)
reg add "HKLM\Software\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f

# Désactiver le service
Get-Service -Name "WebExperienceHostPlugin" | Set-Service -StartupType Disabled
```

### Obtenir de l'aide

Si vous rencontrez des problèmes:

1. **Vérifiez les logs d'erreur:**
   - Event Viewer → Windows Logs → Application
   - Recherchez les erreurs liées à l'app supprimée

2. **Consultez la documentation Microsoft:**
   - https://learn.microsoft.com/en-us/powershell/module/appx/

3. **Créez une issue sur GitHub:**
   - https://github.com/VBlackJack/TwinShell/issues
   - Incluez:
     - Action exécutée (WIN-DEBLOAT-XXX)
     - Message d'erreur complet
     - Version de Windows (Win 10/11, build)
     - Logs d'erreur si disponibles

---

## Ressources supplémentaires

### Scripts de référence

Les scripts PowerShell utilisés par TwinShell sont disponibles dans:
- `/data/seed/initial-actions.json` - Définitions des actions
- Documentation Microsoft sur Remove-AppxPackage
- Documentation Microsoft sur les GPO Windows

### Outils complémentaires

Pour un debloating plus avancé, considérez:
- **Chris Titus Tech Windows Utility** - Interface graphique pour debloating
- **O&O ShutUp10++** - Désactivation de fonctionnalités Windows
- **BleachBit** - Nettoyage de fichiers temporaires

**Note:** TwinShell se concentre sur les actions via PowerShell pour une traçabilité maximale.

### Bonnes pratiques

1. **Testez toujours sur une VM ou machine de test d'abord**
2. **Documentez chaque action effectuée**
3. **Créez un point de restauration avant chaque session de debloating**
4. **Sauvegardez vos données importantes**
5. **Lisez les notes de chaque action avant de l'exécuter**
6. **Ne supprimez pas ce que vous ne comprenez pas**
7. **Privilégiez la désactivation à la suppression quand c'est possible**

---

## Changelog

### Sprint 7 - Novembre 2025
- **Ajout de 28 actions de confidentialité Windows** (WIN-PRIVACY-001 à 304)
- 4 catégories: Permissions apps, Synchronisation cloud, Télémétrie Windows, Télémétrie apps tierces
- **Conformité RGPD:** Documentation complète des articles RGPD applicables
- **Batch prédéfini:** "🔒 Confidentialité maximale" (8 actions critiques)
- **50+ clés registre modifiées** pour télémétrie minimale (WIN-PRIVACY-205)
- Tests de conformité RGPD inclus
- Export de configuration avant/après pour audit
- Support Windows 10/11, Active Directory, GPO

### Sprint 6 - Novembre 2025
- Ajout de 22 actions de debloating Windows
- 4 catégories: Bloatware, Apps Microsoft, Composants système, Fonctionnalités
- Actions de niveau Dangerous pour composants critiques
- Documentation complète avec FAQ et troubleshooting

---

**Dernière mise à jour:** Novembre 2025 - Sprint 7
**Version:** 2.0
**Auteur:** TwinShell Team
