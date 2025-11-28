# Formation : PowerShell Avancé & Tooling

## Introduction

**Arrêtez d'écrire des scripts. Commencez à construire des outils.**

La différence entre un administrateur système et un ingénieur DevOps ne réside pas dans les tâches qu'ils accomplissent, mais dans **comment** ils les accomplissent. Un script résout un problème une fois. Un **outil** résout une catégorie de problèmes, de manière fiable, maintenable et réutilisable.

```
┌─────────────────────────────────────────────────────────────────┐
│              ÉVOLUTION DU SCRIPTING POWERSHELL                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   NIVEAU 1 : Script Procédural                                  │
│   ───────────────────────────────                               │
│   • Code linéaire, pas de fonctions                             │
│   • Variables en dur                                            │
│   • Pas de gestion d'erreur                                     │
│   • "Ça marche sur ma machine"                                  │
│                                                                 │
│   NIVEAU 2 : Script avec Fonctions                              │
│   ────────────────────────────────                              │
│   • Découpage en fonctions                                      │
│   • Paramètres basiques                                         │
│   • Quelques try/catch                                          │
│   • Réutilisable... parfois                                     │
│                                                                 │
│   NIVEAU 3 : Outil Industrialisé                     ◄── CIBLE  │
│   ──────────────────────────────                                │
│   • Advanced Functions (CmdletBinding)                          │
│   • Gestion d'erreur complète                                   │
│   • Support du pipeline                                         │
│   • Module packagé avec manifeste                               │
│   • Tests Pester, documentation intégrée                        │
│   • "Ça marche partout, tout le temps"                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

!!! quote "Philosophie DevOps"
    *"Treat your infrastructure code with the same discipline you would treat production application code."*

    Un script PowerShell déployé en production **est** du code de production.

### Pourquoi cette formation ?

Cette formation vous transformera de **scripteur** en **toolmaker**. Vous apprendrez à :

- :material-tools: Construire des fonctions de qualité professionnelle
- :material-shield-check: Gérer les erreurs de manière robuste
- :material-package-variant: Packager vos outils en modules réutilisables
- :material-file-document: Documenter automatiquement vos commandes

---

## Syllabus de la Formation

Cette formation est organisée en **3 modules** :

### Module 1 : Advanced Functions
:material-function: **Le Cœur du Tooling** | :material-clock-outline: ~60 min

- La magie de `[CmdletBinding()]`
- Paramètres avancés (`Mandatory`, `ValidateSet`, `ValueFromPipeline`)
- Support natif de `-Verbose`, `-Debug`, `-WhatIf`
- Construction d'une fonction template production-ready

[:octicons-arrow-right-24: Accéder au Module 1](01-advanced-functions.md)

---

### Module 2 : Gestion des Erreurs
:material-alert-circle: **Robustesse** | :material-clock-outline: ~45 min

- Erreurs terminantes vs non-terminantes
- Pattern `Try / Catch / Finally`
- Stratégie "Fail Fast" avec `$ErrorActionPreference`
- Logging et traçabilité des erreurs

[:octicons-arrow-right-24: Accéder au Module 2](02-error-handling.md)

---

### Module 3 : Conception de Modules
:material-package-variant-closed: **Industrialisation** | :material-clock-outline: ~60 min

- Structure d'un module (`.psm1` / `.psd1`)
- API publique vs fonctions internes
- Versioning et manifeste
- Déploiement et auto-loading

[:octicons-arrow-right-24: Accéder au Module 3](03-module-design.md)

---

## Prérequis

!!! warning "Connaissances requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **PowerShell Fondamentaux** : Variables, boucles, conditions
    - **Fonctions Basiques** : Création de fonctions simples avec `param()`
    - **Pipeline** : Compréhension du chaînage de commandes (`|`)
    - **Cmdlets Natifs** : `Get-*`, `Set-*`, `New-*`, `Remove-*`

### Environnement de Lab

=== "Windows PowerShell 5.1"

    ```powershell
    # Vérifier la version
    $PSVersionTable.PSVersion

    # Version minimale recommandée : 5.1
    # Disponible nativement sur Windows 10/11 et Server 2016+
    ```

=== "PowerShell 7+ (Cross-Platform)"

    ```powershell
    # Installation via winget (Windows)
    winget install Microsoft.PowerShell

    # Installation via apt (Linux)
    sudo apt-get install -y powershell

    # Vérifier la version
    pwsh -Version
    ```

### Outils Recommandés

| Outil | Usage | Installation |
|-------|-------|--------------|
| **VS Code** | Éditeur principal | `winget install Microsoft.VisualStudioCode` |
| **Extension PowerShell** | IntelliSense, debugging | Extension VS Code `ms-vscode.powershell` |
| **Pester** | Framework de tests | `Install-Module Pester -Force` |
| **PSScriptAnalyzer** | Linting et best practices | `Install-Module PSScriptAnalyzer -Force` |

---

## Conventions de Nommage

!!! tip "Verb-Noun : La Convention PowerShell"
    PowerShell impose une convention de nommage stricte : **Verbe-Nom**.

    | Verbe | Usage | Exemple |
    |-------|-------|---------|
    | `Get-` | Récupérer des données | `Get-WorldlineUser` |
    | `Set-` | Modifier des données existantes | `Set-WorldlineUserPassword` |
    | `New-` | Créer une nouvelle ressource | `New-WorldlineUser` |
    | `Remove-` | Supprimer une ressource | `Remove-WorldlineUser` |
    | `Invoke-` | Exécuter une action | `Invoke-WorldlineAudit` |
    | `Test-` | Vérifier une condition (retourne bool) | `Test-WorldlineConnection` |
    | `Export-` | Exporter vers un fichier/format | `Export-WorldlineReport` |
    | `Import-` | Importer depuis un fichier/format | `Import-WorldlineConfig` |

    ```powershell
    # Lister tous les verbes approuvés
    Get-Verb | Sort-Object Verb
    ```

---

## Ressources Complémentaires

### Documentation Officielle

- :material-link: [about_Functions_Advanced](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions_advanced)
- :material-link: [about_Modules](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_modules)
- :material-link: [PowerShell Best Practices](https://docs.microsoft.com/en-us/powershell/scripting/developer/cmdlet/strongly-encouraged-development-guidelines)

### Ressources Communautaires

- :material-book: [IT-Connect - Cours PowerShell](https://www.it-connect.fr/cours/scripting-powershell/)
- :material-book: [PowerShell Practice Primer](https://github.com/vexx32/PSKoans)
- :material-tools: [PSScriptAnalyzer Rules](https://github.com/PowerShell/PSScriptAnalyzer/blob/master/docs/Rules/README.md)

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook Automation Team
