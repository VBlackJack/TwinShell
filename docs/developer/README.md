# Documentation Developpeur - TwinShell

**Documentation technique complete pour contribuer a TwinShell**

---

## Table des Matieres

- [Architecture et Conception](#architecture-et-conception)
- [Rapports de Sprints](#rapports-de-sprints)
- [Securite](#securite)
- [Performance](#performance)
- [Qualite de Code](#qualite-de-code)
- [Tests](#tests)
- [Guides Techniques](#guides-techniques)

---

## Architecture et Conception

### Architecture Generale

- **[ARCHITECTURE_DECISIONS.md](ARCHITECTURE_DECISIONS.md)** - Decisions architecturales et justifications
- **[TECHNICAL_DEBT.md](TECHNICAL_DEBT.md)** - Dette technique identifiee

### Structure du Projet

```
TwinShell/
├── src/
│   ├── TwinShell.App/          # Application WPF (UI, ViewModels)
│   ├── TwinShell.Core/          # Logique metier (Models, Services)
│   ├── TwinShell.Persistence/   # EF Core + SQLite
│   └── TwinShell.Infrastructure/# Services transverses
├── tests/
│   ├── TwinShell.Core.Tests/
│   └── TwinShell.Persistence.Tests/
├── data/seed/
│   └── initial-actions.json     # Donnees de seed
└── docs/                        # Documentation
```

### Stack Technique

| Composant | Technologie | Version |
|-----------|-------------|---------|
| Framework | .NET | 8.0 |
| UI | WPF (Windows Presentation Foundation) | - |
| Persistence | SQLite + Entity Framework Core | 8.x |
| Architecture | MVVM avec CommunityToolkit.Mvvm | - |
| Tests | xUnit + FluentAssertions | - |

---

## Rapports de Sprints

### Sprint 1 - MVP (Janvier 2025)
- Referentiel d'actions avec templates de commandes
- Recherche et filtrage avances
- Generateur de commandes avec parametres dynamiques
- Copie vers presse-papiers

### Sprint 2 - Personnalisation & Historique
- **[S2-MIGRATION-GUIDE.md](S2-MIGRATION-GUIDE.md)** - Guide de migration vers Sprint 2
- Historique des commandes avec recherche et filtrage
- Systeme de favoris (max 50)
- Export/Import de configuration JSON
- Widget des commandes recentes

### Sprint 3 - UI/UX & Customization
- **[SPRINT3_USER_GUIDE.md](SPRINT3_USER_GUIDE.md)** - Guide utilisateur Sprint 3
- Themes clair/sombre avec mode systeme (WCAG AAA)
- Categories personnalisees avec icones et couleurs
- Animations et transitions fluides
- Notifications toast
- Raccourcis clavier complets

### Sprint 8 - Performance
- **[SPRINT-8-PERFORMANCE-GUIDE.md](SPRINT-8-PERFORMANCE-GUIDE.md)** - Guide de performance
- Pagination dans HistoryViewModel
- ObservableRangeCollection
- Optimisations LINQ
- Reduction CPU (60%)

### Sprint 9 - Windows Integration
- **[SPRINT-9-FINAL-REPORT.md](SPRINT-9-FINAL-REPORT.md)** - Rapport final Sprint 9
- Integration avancee Windows
- Scripts d'optimisation

---

## Securite

### Audit de Securite (Janvier 2025)

**15 vulnerabilites critiques corrigees :**

- **[SECURITY.md](SECURITY.md)** - Politique de securite complete
- **[SECURITY_EXECUTIVE_SUMMARY.md](SECURITY_EXECUTIVE_SUMMARY.md)** - Resume executif securite
- **[SECURITY_FIXES.md](SECURITY_FIXES.md)** - Liste des corrections de securite
- **[SECURITY_VULNERABILITIES_MAP.md](SECURITY_VULNERABILITIES_MAP.md)** - Carte des vulnerabilites

### Vulnerabilites Corrigees

| Categorie | Status | Description |
|-----------|--------|-------------|
| Injection de commandes | CORRIGE | Protection validee |
| Path traversal | CORRIGE | Validation stricte des chemins |
| Memory leaks | CORRIGE | SemaphoreSlim disposes |
| Race conditions | CORRIGE | ViewModels securises |
| Information disclosure | CORRIGE | Exceptions masquees |
| DoS | CORRIGE | Limites sur fichiers et collections |
| Validation entrees | CORRIGE | Validation complete |
| Thread-safety | CORRIGE | Amelioree |

---

## Performance

### Guides d'Optimisation

- **[PERFORMANCE_QUICK_START.md](PERFORMANCE_QUICK_START.md)** - Guide de demarrage rapide
- **[SPRINT-8-PERFORMANCE-GUIDE.md](SPRINT-8-PERFORMANCE-GUIDE.md)** - Guide de performance detaille
- **[OPTIMISATION-WINDOWS.md](OPTIMISATION-WINDOWS.md)** - Guide d'optimisation Windows
- **[PACKAGE-MANAGERS.md](PACKAGE-MANAGERS.md)** - Gestionnaires de paquets Windows

### Ameliorations Cles

| Optimisation | Impact | Description |
|--------------|--------|-------------|
| Pagination | Memoire | Charge 50 entrees au lieu de 1000 |
| ObservableRangeCollection | UI | Single notification |
| Filtrage optimise | CPU | Filtre avant chargement |
| LINQ optimise | CPU | Enumerations uniques |
| Timer interval | CPU | 250ms au lieu de 100ms (-60% CPU) |

---

## Qualite de Code

### Analyses et Reviews

- **[CODE_STYLE_ANALYSIS.md](CODE_STYLE_ANALYSIS.md)** - Analyse du style de code
- **[CODE_ISSUES_SUMMARY.md](CODE_ISSUES_SUMMARY.md)** - Resume des problemes de code
- **[RECOMMENDED_REFACTORINGS.md](RECOMMENDED_REFACTORINGS.md)** - Refactorings recommandes

### Stabilite

**8 problemes de stabilite resolus :**

- Gestion appropriee des ressources (IDisposable pattern)
- Locks pour sections critiques
- Timer cleanup dans ExecutionViewModel
- Fragile checks remplaces par flags explicites

---

## Tests

### Couverture de Tests

| Composant | Tests | Couverture |
|-----------|-------|------------|
| **SettingsService** | 14 tests | 100% pass |
| **SearchService** | 50+ tests | ~90% |
| **TextNormalizer** | 35+ tests | ~95% |

### Execution des Tests

```bash
# Tous les tests
dotnet test

# Tests specifiques
dotnet test --filter "FullyQualifiedName~SearchServiceTests"

# Avec couverture
dotnet test --collect:"XPlat Code Coverage"
```

---

## Guides Techniques

### Documentation Disponible

| Guide | Description | Fichier |
|-------|-------------|---------|
| Recherche | Documentation complete de la recherche | **[SEARCH_FUNCTIONALITY.md](SEARCH_FUNCTIONALITY.md)** |
| Migration S2 | Guide de migration Sprint 2 | **[S2-MIGRATION-GUIDE.md](S2-MIGRATION-GUIDE.md)** |
| Migration | Notes de migration entre versions | **[MIGRATION_NOTES.md](MIGRATION_NOTES.md)** |
| Cross-Platform | Strategie cross-platform | **[CROSS-PLATFORM-STRATEGY.md](CROSS-PLATFORM-STRATEGY.md)** |

---

## Contribuer au Projet

### Guide de Contribution

1. **Forkez** le projet
2. Creez une **branche** pour votre fonctionnalite
3. **Commitez** vos changements
4. **Pushez** vers la branche
5. Ouvrez une **Pull Request**

### Standards de Code

| Standard | Regle |
|----------|-------|
| Architecture | MVVM avec CommunityToolkit.Mvvm |
| Naming | PascalCase pour classes/methodes, camelCase pour variables |
| Documentation | XML comments sur toutes les APIs publiques |
| Tests | Couverture minimale de 80% |
| Commits | Messages clairs et descriptifs (en anglais) |

### Ressources Utiles

- [Guide de Demarrage Rapide](../QuickStart.md)
- [Guide Utilisateur](../UserGuide.md)
- [FAQ](../FAQ.md)

---

## Ressources Externes

### Documentation Microsoft

- [.NET 8 Documentation](https://learn.microsoft.com/fr-fr/dotnet/core/whats-new/dotnet-8)
- [WPF Documentation](https://learn.microsoft.com/fr-fr/dotnet/desktop/wpf/)
- [Entity Framework Core](https://learn.microsoft.com/fr-fr/ef/core/)
- [PowerShell Documentation](https://learn.microsoft.com/fr-fr/powershell/)

### Communaute

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [SQLite](https://www.sqlite.org/docs.html)

---

## Contact

**Questions techniques ?** Ouvrez une issue sur [GitHub Issues](https://github.com/VBlackJack/TwinShell/issues)

**Discussions ?** Rejoignez [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)

---

*Derniere mise a jour : 2025-11-28*
*Version : 1.3.0*
