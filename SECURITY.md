# Security Policy

## Overview

TwinShell est conçu avec la sécurité comme priorité. Ce document décrit les pratiques de sécurité implémentées dans le projet et comment signaler des vulnérabilités.

## Pratiques de Sécurité Implémentées

### 1. Protection Contre les Injections de Commandes

**Protection :** Validation stricte de tous les paramètres utilisateur avant génération de commandes.

**Implémentation :**
- Validation des paramètres via `CommandGeneratorService`
- Sanitization des entrées utilisateur
- Whitelist de caractères autorisés pour les paramètres
- Échappement automatique des caractères spéciaux
- Longueur maximale de 256 caractères par paramètre

**Fichiers concernés :**
- `src/TwinShell.Core/Services/CommandGeneratorService.cs`
- `src/TwinShell.Core/Constants/ValidationConstants.cs`

### 2. Protection Path Traversal

**Protection :** Validation des chemins de fichiers pour empêcher l'accès à des répertoires non autorisés.

**Implémentation :**
- Vérification que les chemins restent dans les répertoires autorisés
- Détection de patterns malveillants (`..`, `~`, UNC paths)
- Rejet des liens symboliques et junction points
- Sandboxing des opérations de fichiers dans des répertoires spécifiques

**Fichiers concernés :**
- `src/TwinShell.Core/Services/ConfigurationService.cs` (lignes 339-401)
- `src/TwinShell.Core/Services/AuditLogService.cs` (lignes 89-125)

### 3. Gestion Sécurisée des Exceptions

**Protection :** Les détails d'exception ne sont jamais exposés aux utilisateurs pour éviter la fuite d'informations sensibles.

**Implémentation :**
- Messages d'erreur génériques pour les utilisateurs
- Logging détaillé uniquement dans les fichiers logs
- Pas d'exposition des stack traces dans l'UI
- Sanitization des informations d'erreur avant affichage

**Pattern utilisé :**
```csharp
catch (Exception ex)
{
    // SECURITY: Don't expose exception details to users
    StatusMessage = _localizationService.GetString(MessageKeys.CommonError);
    // Log details internally only
}
```

### 4. Validation des Entrées Utilisateur

**Protection :** Toutes les entrées sont validées avant traitement.

**Limites de sécurité :**
- Commandes : max 1024 caractères
- Paramètres : max 256 caractères
- Chemins de fichiers : max 260 caractères
- Fichiers d'import : max 10 MB
- Historique d'import : max 10000 entrées
- Favoris d'import : max 1000 entrées

**Fichiers concernés :**
- `src/TwinShell.Core/Constants/ValidationConstants.cs`

### 5. Protection DoS (Denial of Service)

**Protection :** Limites sur les opérations coûteuses pour éviter l'épuisement des ressources.

**Implémentation :**
- Timeout d'exécution : 30s par défaut, max 300s
- Limite d'historique : 100,000 entrées max
- Limite de favoris : 50 max par utilisateur
- Validation de la taille des fichiers JSON importés
- Pagination des grandes collections (50 items par page)

### 6. Concurrence et Race Conditions

**Protection :** Utilisation de SemaphoreSlim pour protéger les sections critiques.

**Implémentation :**
- Locks sur les opérations de filtrage (`MainViewModel`, `HistoryViewModel`)
- Thread-safety dans `ExecutionViewModel` avec `_lock` object
- Disposal pattern correctement implémenté avec IDisposable
- Prévention des memory leaks via disposal des SemaphoreSlim

**Exemple :**
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

### 7. Cryptographie et Stockage Sécurisé

**Protection :** Pas de stockage de mots de passe ou données sensibles en clair.

**Implémentation :**
- Aucun credential n'est stocké dans le code
- Configuration sécurisée via UserSettings
- Pas de hardcoded secrets ou API keys
- Variables d'environnement pour configuration sensible

### 8. Validation JSON

**Protection :** Validation structurelle avant désérialisation pour éviter les attaques par JSON malformé.

**Implémentation :**
- Schéma JSON validé avant parse
- Limites sur les tailles de tableaux (DoS protection)
- Vérification des types de données
- Protection contre les objets JSON excessivement imbriqués

**Fichiers concernés :**
- `src/TwinShell.Core/Services/ConfigurationService.cs` (lignes 406-448)

### 9. PowerShell Gallery Security

**Protection :** Validation et sanitization des requêtes vers PowerShell Gallery.

**Implémentation :**
- Validation des noms de modules (regex pattern)
- Limite de résultats de recherche : 50
- Timeout des installations : 300s
- Sanitization des queries de recherche
- Protection contre les noms de modules malicieux

**Fichiers concernés :**
- `src/TwinShell.Infrastructure/Services/PowerShellGalleryService.cs`

### 10. Audit Logging

**Protection :** Traçabilité complète des actions critiques.

**Implémentation :**
- Log de toutes les exécutions de commandes
- Timestamp, commande, platform, exit code, durée
- Export sécurisé des logs en CSV
- Rotation automatique des logs (365 jours par défaut)
- CSV escaping pour prévenir injection

**Fichiers concernés :**
- `src/TwinShell.Core/Services/AuditLogService.cs`
- `src/TwinShell.Persistence/Repositories/AuditLogRepository.cs`

## Vulnérabilités Connues Corrigées

### Sprint Audit de Sécurité (Commit 2c383c1)

**15 vulnérabilités critiques corrigées :**
1. Command injection dans CommandGeneratorService
2. Path traversal dans ConfigurationService
3. Memory leaks (SemaphoreSlim non disposés)
4. Race conditions dans ViewModels
5. Exception information disclosure
6. DoS via large file imports
7. DoS via infinite JSON arrays
8. SQL injection potential dans SearchAsync
9. Unhandled exceptions crashing app
10. Timer resource leaks dans ExecutionViewModel
11. Hardcoded sensitive values
12. Missing input validation
13. Insecure file operations
14. Fragile count checks (replaced with flags)
15. Missing thread synchronization

## Signaler une Vulnérabilité

Si vous découvrez une vulnérabilité de sécurité dans TwinShell, merci de suivre le processus de divulgation responsable :

### Processus de Signalement

1. **NE PAS** créer une issue publique GitHub
2. Envoyer un email à : [SECURITY_EMAIL_TO_BE_CONFIGURED]
3. Inclure :
   - Description détaillée de la vulnérabilité
   - Steps pour reproduire
   - Impact potentiel
   - Version affectée
   - Toute preuve de concept (PoC)

### Temps de Réponse

- **Accusé de réception :** 48 heures
- **Évaluation initiale :** 1 semaine
- **Correctif :** Variable selon la criticité
  - Critique : 1-2 semaines
  - Haute : 2-4 semaines
  - Moyenne : 1-2 mois
  - Basse : Prochaine release majeure

### Crédit

Les chercheurs en sécurité qui signalent des vulnérabilités de manière responsable seront crédités dans les release notes (sauf demande contraire).

## Meilleures Pratiques pour les Contributeurs

### Code Review

Toute PR doit inclure une review de sécurité portant sur :
- Validation des entrées utilisateur
- Protection contre l'injection
- Gestion sécurisée des exceptions
- Pas de hardcoded secrets
- Proper disposal des ressources
- Thread-safety si nécessaire

### Tests de Sécurité

Les tests doivent couvrir :
- Cas limites et valeurs extrêmes
- Entrées malformées
- Caractères spéciaux et injection
- Chemins de fichiers malveillants
- Concurrence et race conditions

### Documentation

Toute fonctionnalité avec implications de sécurité doit être documentée avec :
- Commentaire `// SECURITY:` dans le code
- Description de la protection implémentée
- Références aux standards (OWASP, etc.)

## Standards et Références

TwinShell suit les recommandations de :
- **OWASP Top 10** : Protection contre les vulnérabilités web les plus courantes
- **CWE/SANS Top 25** : Weaknesses logicielles dangereuses
- **Microsoft Security Development Lifecycle (SDL)**
- **.NET Security Best Practices**

## Versions Supportées

| Version | Supportée          | Fin de Support |
| ------- | ------------------ | -------------- |
| 1.0.x   | :white_check_mark: | TBD            |
| < 1.0   | :x:                | Deprecated     |

## Changelog de Sécurité

### Version 1.0 (2025-01)
- ✅ Audit de sécurité complet
- ✅ 15 vulnérabilités critiques corrigées
- ✅ 8 problèmes de stabilité résolus
- ✅ Protection path traversal implémentée
- ✅ Validation complète des entrées
- ✅ Protection DoS via limites
- ✅ Audit logging complet

---

**Dernière mise à jour :** 2025-01-17
**Version du document :** 1.0
