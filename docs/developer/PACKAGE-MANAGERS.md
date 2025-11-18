# Guide de Gestion des Packages - TwinShell

## Table des matières

1. [Introduction](#introduction)
2. [Prérequis](#prérequis)
3. [Winget - Windows Package Manager](#winget---windows-package-manager)
4. [Chocolatey - Package Manager](#chocolatey---package-manager)
5. [Actions disponibles](#actions-disponibles)
6. [Utilisation de l'interface de recherche](#utilisation-de-linterface-de-recherche)
7. [Exemples d'utilisation](#exemples-dutilisation)
8. [Dépannage](#dépannage)
9. [Bonnes pratiques](#bonnes-pratiques)

## Introduction

TwinShell intègre la gestion de packages Windows via deux gestionnaires principaux :

- **Winget** : Le gestionnaire de packages officiel de Microsoft
- **Chocolatey** : Un gestionnaire de packages communautaire populaire

Ces outils permettent d'installer, mettre à jour, rechercher et gérer des applications Windows en ligne de commande, offrant une expérience similaire à `apt` sur Linux ou `brew` sur macOS.

## Prérequis

### Winget

**Prérequis système :**
- Windows 10 version 1809 (build 17763) ou supérieure
- Windows 11 (toutes versions)

**Installation :**
Winget est préinstallé sur Windows 11 et les versions récentes de Windows 10. Si ce n'est pas le cas :

1. Installer depuis le Microsoft Store : [App Installer](https://www.microsoft.com/p/app-installer/9nblggh4nns1)
2. Ou télécharger depuis GitHub : [Winget Releases](https://github.com/microsoft/winget-cli/releases)

**Vérification :**
```powershell
winget --version
```

### Chocolatey

**Prérequis système :**
- Windows 7+ / Windows Server 2003+
- PowerShell v2+ (recommandé v5.1+)
- .NET Framework 4+ (recommandé 4.8)

**Installation :**

Exécuter en tant qu'administrateur dans PowerShell :

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

**Vérification :**
```powershell
choco --version
```

## Winget - Windows Package Manager

### Fonctionnalités principales

Winget permet de :
- Installer des applications depuis les sources Microsoft
- Mettre à jour automatiquement les applications
- Désinstaller proprement les logiciels
- Rechercher dans un catalogue de plus de 10 000 applications

### Sources de packages

Par défaut, Winget utilise :
- **msstore** : Microsoft Store
- **winget** : Dépôt communautaire Microsoft

### Commandes principales

| Commande | Description |
|----------|-------------|
| `winget search <terme>` | Recherche de packages |
| `winget install <id>` | Installation d'un package |
| `winget upgrade <id>` | Mise à jour d'un package |
| `winget upgrade --all` | Mise à jour de tous les packages |
| `winget uninstall <id>` | Désinstallation |
| `winget list` | Liste des packages installés |
| `winget show <id>` | Détails d'un package |
| `winget source list` | Liste des sources |

## Chocolatey - Package Manager

### Fonctionnalités principales

Chocolatey offre :
- Plus de 9 000 packages dans le dépôt communautaire
- Support de dépôts privés pour les entreprises
- Gestion des dépendances automatique
- Scripts d'installation personnalisables

### Sources de packages

- **chocolatey** : Dépôt communautaire principal (https://community.chocolatey.org/packages)
- Support de sources NuGet personnalisées

### Commandes principales

| Commande | Description |
|----------|-------------|
| `choco search <terme>` | Recherche de packages |
| `choco install <id> -y` | Installation d'un package |
| `choco upgrade <id> -y` | Mise à jour d'un package |
| `choco upgrade all -y` | Mise à jour de tous les packages |
| `choco uninstall <id> -y` | Désinstallation |
| `choco list --local-only` | Liste des packages installés |
| `choco info <id>` | Détails d'un package |
| `choco source list` | Liste des sources |

## Actions disponibles

TwinShell propose 15 actions préconfigurées pour gérer vos packages :

### Actions Winget (8)

| ID | Action | Niveau | Description |
|----|--------|--------|-------------|
| WIN-PKG-001 | Installer une application | Run | Installe un package via Winget |
| WIN-PKG-002 | Lister les applications | Run | Liste tous les packages installés |
| WIN-PKG-003 | Mettre à jour une application | Run | Met à jour un package spécifique |
| WIN-PKG-004 | Mettre à jour toutes les applications | Run | Met à jour tous les packages |
| WIN-PKG-005 | Désinstaller une application | Dangerous | Supprime un package |
| WIN-PKG-006 | Rechercher un package | Run | Recherche dans le catalogue |
| WIN-PKG-007 | Afficher les détails | Run | Affiche les informations détaillées |
| WIN-PKG-008 | Ajouter une source | Run | Ajoute une source personnalisée |

### Actions Chocolatey (7)

| ID | Action | Niveau | Description |
|----|--------|--------|-------------|
| WIN-PKG-101 | Installer un package | Run | Installe un package via Chocolatey |
| WIN-PKG-102 | Lister les packages | Run | Liste tous les packages installés |
| WIN-PKG-103 | Mettre à jour un package | Run | Met à jour un package spécifique |
| WIN-PKG-104 | Mettre à jour tous les packages | Run | Met à jour tous les packages |
| WIN-PKG-105 | Désinstaller un package | Dangerous | Supprime un package |
| WIN-PKG-106 | Rechercher un package | Run | Recherche dans le catalogue |
| WIN-PKG-107 | Ajouter une source | Run | Ajoute une source personnalisée |

## Utilisation de l'interface de recherche

TwinShell intègre un service de recherche de packages (`PackageManagerService`) qui permet :

### Recherche de packages

```csharp
// Recherche Winget
var wingetResults = await packageManagerService.SearchWingetPackagesAsync("vscode");

// Recherche Chocolatey
var chocoResults = await packageManagerService.SearchChocolateyPackagesAsync("chrome");
```

### Obtention des détails

```csharp
// Détails Winget
var packageInfo = await packageManagerService.GetWingetPackageInfoAsync("Microsoft.VisualStudioCode");

// Détails Chocolatey
var packageInfo = await packageManagerService.GetChocolateyPackageInfoAsync("googlechrome");
```

### Liste des packages installés

```csharp
// Packages Winget installés
var installedWinget = await packageManagerService.ListWingetInstalledPackagesAsync();

// Packages Chocolatey installés
var installedChoco = await packageManagerService.ListChocolateyInstalledPackagesAsync();
```

### Vérification de disponibilité

```csharp
// Vérifier si Winget est disponible
bool isWingetAvailable = await packageManagerService.IsWingetAvailableAsync();

// Vérifier si Chocolatey est disponible
bool isChocoAvailable = await packageManagerService.IsChocolateyAvailableAsync();
```

## Exemples d'utilisation

### Scénario 1 : Installation d'un environnement de développement

```powershell
# Via Winget
winget install Microsoft.VisualStudioCode --silent
winget install Git.Git --silent
winget install Microsoft.PowerToys --silent
winget install Microsoft.WindowsTerminal --silent

# Via Chocolatey
choco install vscode -y
choco install git -y
choco install nodejs -y
choco install docker-desktop -y
```

### Scénario 2 : Maintenance régulière

```powershell
# Mise à jour de tous les packages
winget upgrade --all --silent
choco upgrade all -y
```

### Scénario 3 : Recherche et installation

```powershell
# Recherche
winget search "visual studio"
choco search firefox

# Installation après vérification
winget show Microsoft.VisualStudio.2022.Community
winget install Microsoft.VisualStudio.2022.Community --silent
```

### Scénario 4 : Gestion d'entreprise avec sources personnalisées

```powershell
# Ajouter une source Winget privée
winget source add --name "CompanyRepo" "https://packages.company.com/winget"

# Ajouter une source Chocolatey privée
choco source add --name="CompanyNuGet" --source="https://packages.company.com/nuget"

# Installer depuis la source personnalisée
winget install CompanyApp --source "CompanyRepo"
choco install company-tool -y --source="CompanyNuGet"
```

## Dépannage

### Winget

**Problème : "winget n'est pas reconnu"**
- Solution : Installer ou réinstaller App Installer depuis le Microsoft Store
- Vérifier que `C:\Users\<User>\AppData\Local\Microsoft\WindowsApps` est dans le PATH

**Problème : Erreur de source**
- Solution : `winget source reset --force`

**Problème : Mise à jour bloquée**
- Solution : Vérifier les mises à jour Windows, Winget dépend de composants système

### Chocolatey

**Problème : "choco n'est pas reconnu"**
- Solution : Réinstaller Chocolatey en mode administrateur
- Vérifier le PATH : `C:\ProgramData\chocolatey\bin`

**Problème : Erreur de permission**
- Solution : Toujours exécuter PowerShell en mode administrateur pour Chocolatey

**Problème : Installation qui échoue**
- Solution :
  ```powershell
  choco install <package> -y --force
  # Ou nettoyer le cache
  choco cache clean
  ```

### Problèmes communs

**Conflit entre Winget et Chocolatey**
- Les deux peuvent coexister, mais attention aux doublons
- Utiliser `winget list` et `choco list --local-only` pour vérifier

**Packages obsolètes**
- Certains packages peuvent ne plus être maintenus
- Vérifier la date de dernière mise à jour avant installation

## Bonnes pratiques

### Sécurité

1. **Vérifier les sources** : Toujours vérifier l'éditeur et la source d'un package
2. **Éviter les sources non fiables** : Ne pas ajouter de sources sans vérification
3. **Mises à jour régulières** : Maintenir les packages à jour pour les correctifs de sécurité
4. **Utiliser --silent avec précaution** : Vérifier ce qui sera installé avant

### Performance

1. **Nettoyer régulièrement** :
   ```powershell
   winget cache clean
   choco cache clean
   ```

2. **Désinstaller les packages inutilisés** :
   ```powershell
   winget list  # Identifier les packages
   winget uninstall <package>
   ```

### Automatisation

1. **Scripts de setup** : Créer des scripts pour installer un environnement complet
   ```powershell
   # setup-dev-env.ps1
   $packages = @(
       "Microsoft.VisualStudioCode",
       "Git.Git",
       "Microsoft.PowerToys"
   )

   foreach ($package in $packages) {
       winget install $package --silent --accept-package-agreements
   }
   ```

2. **Tâches planifiées** : Automatiser les mises à jour
   ```powershell
   # Créer une tâche pour mise à jour quotidienne
   $action = New-ScheduledTaskAction -Execute "winget" -Argument "upgrade --all --silent"
   $trigger = New-ScheduledTaskTrigger -Daily -At 3am
   Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "WingetDailyUpdate"
   ```

### Documentation

1. **Documenter les installations** : Garder trace des packages installés
2. **Versionner les scripts** : Utiliser Git pour vos scripts d'installation
3. **Partager les configurations** : Créer des fichiers de configuration d'équipe

## Ressources supplémentaires

### Documentation officielle

- [Winget Documentation](https://learn.microsoft.com/en-us/windows/package-manager/winget/)
- [Chocolatey Documentation](https://docs.chocolatey.org/)
- [Winget Package Repository](https://github.com/microsoft/winget-pkgs)
- [Chocolatey Community Packages](https://community.chocolatey.org/packages)

### Communauté

- [Winget GitHub](https://github.com/microsoft/winget-cli)
- [Chocolatey GitHub](https://github.com/chocolatey/choco)
- [Reddit - r/Winget](https://www.reddit.com/r/winget/)
- [Reddit - r/Chocolatey](https://www.reddit.com/r/chocolatey/)

### Outils complémentaires

- [WingetUI](https://github.com/martinet101/WingetUI) - Interface graphique pour Winget
- [Chocolatey GUI](https://community.chocolatey.org/packages/ChocolateyGUI) - Interface graphique pour Chocolatey
- [winget-pkgs-automation](https://github.com/microsoft/winget-pkgs-automation) - Outils d'automatisation

---

**Dernière mise à jour** : Sprint 5 - Novembre 2025
**Version TwinShell** : Compatible avec toutes versions
**Auteur** : Équipe TwinShell
