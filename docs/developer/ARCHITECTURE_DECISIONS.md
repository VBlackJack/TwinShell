# Architecture Decisions Record (ADR)

Ce document enregistre les décisions architecturales importantes prises dans le projet TwinShell.

## Table des Matières

1. [Architecture en Couches](#1-architecture-en-couches)
2. [SQLite comme Base de Données](#2-sqlite-comme-base-de-données)
3. [MVVM avec CommunityToolkit](#3-mvvm-avec-communitytoolkit)
4. [Sécurité : Salt Déterministe](#4-sécurité--salt-déterministe)
5. [Performance : ObservableRangeCollection](#5-performance--observablerangecollection)
6. [Pagination de l'Historique](#6-pagination-de-lhistorique)
7. [Gestion des Ressources : IDisposable Pattern](#7-gestion-des-ressources--idisposable-pattern)
8. [Thread Safety : SemaphoreSlim](#8-thread-safety--semaphoreslim)
9. [Validation Path Traversal](#9-validation-path-traversal)
10. [Magic Numbers vs Constantes](#10-magic-numbers-vs-constantes)
11. [Stratégie d'Intégration CLI Universelle](#11-stratégie-dintégration-cli-universelle)

---

## 1. Architecture en Couches

### Décision

TwinShell utilise une architecture en 4 couches :
- **TwinShell.App** : WPF UI et ViewModels
- **TwinShell.Core** : Logique métier et services
- **TwinShell.Persistence** : EF Core et repositories
- **TwinShell.Infrastructure** : Services transverses

### Contexte

Besoin de séparer les responsabilités pour faciliter la maintenance et les tests.

### Conséquences

**Positives :**
- Testabilité accrue (mocking facile des couches)
- Séparation claire des responsabilités
- Facilite les modifications futures
- Migration possible vers d'autres UI (Avalonia, MAUI)

**Négatives :**
- Plus de complexité initiale
- Plus de fichiers à gérer
- Courbe d'apprentissage pour nouveaux contributeurs

**Alternatives considérées :**
- Monolithe en une seule couche (rejeté : non maintenable)
- Clean Architecture avec plus de couches (rejeté : overkill pour ce projet)

---

## 2. SQLite comme Base de Données

### Décision

Utilisation de SQLite avec Entity Framework Core pour la persistence.

### Contexte

Besoin d'une base de données locale, légère, sans serveur pour stocker les actions, l'historique et les favoris.

### Conséquences

**Positives :**
- Zero configuration (pas de serveur à installer)
- Fichier unique portable
- Excellent pour applications desktop
- EF Core support complet
- Transactions ACID

**Négatives :**
- Pas de concurrence multi-utilisateur (non requis)
- Limite de 281 TB (largement suffisant)

**Alternatives considérées :**
- LiteDB (rejeté : moins mature qu'EF Core)
- JSON files (rejeté : pas de requêtes complexes)
- SQL Server LocalDB (rejeté : trop lourd)

---

## 3. MVVM avec CommunityToolkit

### Décision

Utilisation du pattern MVVM avec CommunityToolkit.Mvvm (source generators).

### Contexte

WPF nécessite une architecture UI découplée. CommunityToolkit offre des source generators pour réduire le boilerplate.

### Conséquences

**Positives :**
- Moins de code boilerplate (`[ObservableProperty]`, `[RelayCommand]`)
- Testabilité des ViewModels
- Séparation UI / logique
- Performance des source generators

**Négatives :**
- Dépendance à CommunityToolkit
- Courbe d'apprentissage des attributs

**Alternatives considérées :**
- MVVM manuel (rejeté : trop de boilerplate)
- Prism (rejeté : trop complexe)
- ReactiveUI (rejeté : paradigme différent)

---

## 4. Sécurité : Salt Déterministe

### Décision

Pas de salt aléatoire pour le hashing. Les IDs sont générés avec `Guid.NewGuid()` suffisamment unique.

### Contexte

TwinShell ne stocke pas de mots de passe utilisateur. Les données (favoris, historique) ne nécessitent pas de cryptographie forte.

### Conséquences

**Positives :**
- Simplicité du code
- Pas de gestion de salt storage
- Performance (pas de random generation)

**Négatives :**
- Si besoin futur d'authentification, devra être repensé

**Justification :**
- Application single-user locale
- Pas de données sensibles nécessitant encryption
- IDs GUID suffisants pour unicité

**Note de sécurité :** Si authentification multi-utilisateur ajoutée, utiliser PBKDF2 ou Argon2 avec salt aléatoire.

---

## 5. Performance : ObservableRangeCollection

### Décision

Utilisation d'`ObservableRangeCollection<T>` avec `ReplaceRange()` au lieu de recreéer les collections.

### Contexte

Recréer une `ObservableCollection` déclenche de multiples notifications UI, causant des lags.

### Conséquences

**Positives :**
- Single UI notification au lieu de N
- Amélioration significative des performances de filtrage
- UI plus responsive

**Négatives :**
- Code custom à maintenir (classe `ObservableRangeCollection`)

**Mesures :**
- Avant : N notifications (N items)
- Après : 1 notification (Reset)
- Gain : 90%+ de réduction des notifications UI

**Fichiers concernés :**
- `src/TwinShell.App/Collections/ObservableRangeCollection.cs`
- `src/TwinShell.App/ViewModels/MainViewModel.cs:218`
- `src/TwinShell.App/ViewModels/HistoryViewModel.cs:213`

---

## 6. Pagination de l'Historique

### Décision

Implémentation de la pagination avec 50 items par page au lieu de charger 1000 entrées d'un coup.

### Contexte

Charger 1000 entrées d'historique crée des lags UI et consomme beaucoup de mémoire.

### Conséquences

**Positives :**
- Mémoire réduite (~95%)
- Temps de chargement initial divisé par 20
- UI plus responsive
- Scalabilité améliorée

**Négatives :**
- Complexité ajoutée (pagination UI)
- Nécessite des contrôles de navigation

**Configuration :**
- `PageSize` : 50 items (constante `ValidationConstants.DefaultHistoryPageSize`)
- `DefaultHistoryLoadCount` : 1000 items max en mémoire

**Fichiers concernés :**
- `src/TwinShell.App/ViewModels/HistoryViewModel.cs`
- `src/TwinShell.Core/Constants/ValidationConstants.cs`

---

## 7. Gestion des Ressources : IDisposable Pattern

### Décision

Implémentation systématique du pattern IDisposable pour les ViewModels utilisant des ressources non managées (SemaphoreSlim, Timers).

### Contexte

Memory leaks identifiés lors de l'audit de sécurité : SemaphoreSlim et Timers non disposés.

### Conséquences

**Positives :**
- Pas de memory leaks
- Cleanup approprié des ressources
- Best practice .NET respectée

**Pattern utilisé :**
```csharp
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (!_disposed)
    {
        if (disposing)
        {
            _semaphore?.Dispose();
            _timer?.Dispose();
        }
        _disposed = true;
    }
}
```

**Fichiers concernés :**
- `src/TwinShell.App/ViewModels/MainViewModel.cs`
- `src/TwinShell.App/ViewModels/HistoryViewModel.cs`
- `src/TwinShell.App/ViewModels/ExecutionViewModel.cs`

---

## 8. Thread Safety : SemaphoreSlim

### Décision

Utilisation de `SemaphoreSlim` pour protéger les sections critiques dans les ViewModels.

### Contexte

Race conditions identifiées dans les méthodes de filtrage appelées depuis multiples property changed handlers.

### Conséquences

**Positives :**
- Thread-safety garantie
- Pas de race conditions
- Performance (SemaphoreSlim plus léger que Mutex)

**Pattern utilisé :**
```csharp
private readonly SemaphoreSlim _filterSemaphore = new SemaphoreSlim(1, 1);

await _filterSemaphore.WaitAsync();
try
{
    // Critical section
}
finally
{
    _filterSemaphore.Release();
}
```

**Alternatives considérées :**
- `lock` statement (rejeté : ne supporte pas async/await)
- `Mutex` (rejeté : trop lourd)
- `ReaderWriterLockSlim` (rejeté : pas nécessaire)

**Fichiers concernés :**
- `src/TwinShell.App/ViewModels/MainViewModel.cs:24`
- `src/TwinShell.App/ViewModels/HistoryViewModel.cs:16`
- `src/TwinShell.App/ViewModels/ExecutionViewModel.cs:20`

---

## 9. Validation Path Traversal

### Décision

Validation stricte des chemins de fichiers pour prévenir path traversal attacks.

### Contexte

Export/import de fichiers JSON et CSV ouvrent des vecteurs d'attaque potentiels.

### Méthode de validation :

1. Reject patterns malveillants (`..`, `~`, UNC paths)
2. Canonicalize path (`Path.GetFullPath`)
3. Vérifier que le path est dans les répertoires autorisés
4. Reject symbolic links et junction points
5. Valider l'extension de fichier

**Répertoires autorisés :**
- `%USERPROFILE%\Documents`
- `%USERPROFILE%\Desktop`
- `%LOCALAPPDATA%\TwinShell\Exports`

**Conséquences :**

**Positives :**
- Protection contre path traversal
- Sandbox des opérations de fichiers
- Conforme aux security best practices

**Négatives :**
- Utilisateur ne peut pas exporter vers des lecteurs réseau
- Nécessite permissions sur répertoires user

**Fichiers concernés :**
- `src/TwinShell.Core/Services/ConfigurationService.cs:339-401`
- `src/TwinShell.Core/Services/AuditLogService.cs:89-125`

---

## 10. Magic Numbers vs Constantes

### Décision

Toutes les valeurs numériques "magiques" sont extraites dans des constantes nommées.

### Contexte

Maintenance difficile avec des valeurs hardcodées (250, 1000, 50, etc.).

### Constantes créées :

```csharp
// ValidationConstants.cs
public const int DefaultHistoryLoadCount = 1000;
public const int DefaultHistoryPageSize = 50;
public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
public const int MaxFavoritesInImport = 1000;
public const int MaxHistoryInImport = 10000;

// UIConstants.cs
public const int MaxFavoritesCount = 50;

// TimeoutConstants.cs
public const int MaxCommandTimeoutSeconds = 300;
```

**Conséquences :**

**Positives :**
- Lisibilité améliorée
- Maintenance facilitée (change une fois, appliqué partout)
- Documentation inline via noms explicites
- Moins d'erreurs (typos impossibles)

**Négatives :**
- Plus de fichiers de constantes
- Besoin de connaître l'emplacement des constantes

**Fichiers concernés :**
- `src/TwinShell.Core/Constants/ValidationConstants.cs`
- `src/TwinShell.Core/Constants/UIConstants.cs`
- `src/TwinShell.Core/Constants/TimeoutConstants.cs`

---

## 11. Stratégie d'Intégration CLI Universelle

### Décision

Extension du support au-delà des cmdlets PowerShell natifs pour inclure les exécutables CLI standards de l'industrie (AWS CLI, Kubectl, OpenSSL, Terraform).
Utilisation du flag `Platform: 2` (Both/Cross-Platform) pour ces outils.

### Contexte

L'administration système moderne (DevOps) repose majoritairement sur des outils en ligne de commande cross-platform, et non plus uniquement sur WMI/CIM/PowerShell. TwinShell doit évoluer pour rester pertinent.

### Conséquences

**Positives :**
- **Écosystème infini** : Capacité d'intégrer n'importe quel outil (GCP, Azure, Git, Docker).
- **Portabilité** : Les commandes JSON sont identiques sur Windows et Linux (WSL/Native).
- **Standardisation** : Utilisation des arguments standards POSIX au lieu des paramètres nommés PowerShell complexes.

**Négatives :**
- **Dépendances externes** : L'utilisateur doit avoir les outils installés (ex: `aws` doit être dans le PATH).
- **Validation plus difficile** : TwinShell ne peut pas facilement valider la syntaxe d'outils tiers comme il le fait pour PowerShell.

**Implémentation :**
- Le `CommandGeneratorService` traite ces commandes comme des chaînes d'arguments standards.
- Le `Seed` (JSON) inclut désormais des templates unifiés pour Windows et Linux.

**Outils intégrés (v1.2.0) :**
- ☁️ **Cloud** : AWS CLI, gcloud (GCP), Azure CLI
- 🚢 **Containers** : kubectl, Docker
- 🔨 **IaC** : Terraform, Ansible
- 🔐 **Security** : OpenSSL, Nmap

---

## Principes Généraux

### SOLID Principles

- **Single Responsibility** : Chaque classe a une responsabilité unique
- **Open/Closed** : Extensions via interfaces, modifications minimales
- **Liskov Substitution** : Repositories interchangeables via interfaces
- **Interface Segregation** : Interfaces spécifiques (pas de "fat interfaces")
- **Dependency Inversion** : DI container pour injection de dépendances

### Security by Design

- Input validation systématique
- Principe du moindre privilège
- Defense in depth (multiple layers)
- Fail securely (erreurs ne leakent pas d'info)
- Audit logging pour actions critiques

### Performance First

- AsNoTracking pour queries read-only
- Pagination pour grandes collections
- Batch operations (ReplaceRange)
- Caching quand approprié
- Profiling régulier

---

**Dernière mise à jour :** 2025-11-26
**Version du document :** 1.1
