# 🔧 Documentation Développeur - TwinShell

**Documentation technique complète pour contribuer à TwinShell**

---

## 📚 Table des Matières

- [Architecture et Conception](#architecture-et-conception)
- [Rapports de Sprints](#rapports-de-sprints)
- [Sécurité](#sécurité)
- [Performance](#performance)
- [Qualité de Code](#qualité-de-code)
- [Tests](#tests)
- [Migration et Déploiement](#migration-et-déploiement)
- [Guides Techniques](#guides-techniques)

---

## Architecture et Conception

### Architecture Générale

- **[ARCHITECTURE_DECISIONS.md](ARCHITECTURE_DECISIONS.md)** - Décisions architecturales et justifications
- **[ANALYSIS_EXECUTIVE_SUMMARY.md](ANALYSIS_EXECUTIVE_SUMMARY.md)** - Résumé exécutif de l'analyse du projet
- **[COMPARATIVE_ANALYSIS.md](COMPARATIVE_ANALYSIS.md)** - Analyse comparative des solutions techniques

### Structure du Projet

```
TwinShell/
├── src/
│   ├── TwinShell.App/          # Application WPF (UI, ViewModels)
│   ├── TwinShell.Core/          # Logique métier (Models, Services)
│   ├── TwinShell.Persistence/   # EF Core + SQLite
│   └── TwinShell.Infrastructure/# Services transverses
├── tests/
│   ├── TwinShell.Core.Tests/
│   └── TwinShell.Persistence.Tests/
├── data/seed/
│   └── initial-actions.json     # Données de seed
└── docs/                        # Documentation
```

### Stack Technique

- **.NET 8.0** - Framework de développement
- **WPF** (Windows Presentation Foundation) - Interface utilisateur
- **SQLite** + **Entity Framework Core** - Persistence
- **MVVM** avec **CommunityToolkit.Mvvm** - Architecture
- **xUnit** + **FluentAssertions** - Tests unitaires

---

## Rapports de Sprints

### Sprint 1 - MVP (Janvier 2025)
- Référentiel d'actions avec templates de commandes
- Recherche et filtrage avancés
- Générateur de commandes avec paramètres dynamiques
- Copie vers presse-papiers

### Sprint 2 - Personnalisation & Historique (Janvier 2025)
- **[S2-MIGRATION-GUIDE.md](S2-MIGRATION-GUIDE.md)** - Guide de migration vers Sprint 2
- Historique des commandes avec recherche et filtrage
- Système de favoris (max 50)
- Export/Import de configuration JSON
- Widget des commandes récentes

### Sprint 3 - UI/UX & Customization (Janvier 2025)
- **[SPRINT3_SUMMARY.md](SPRINT3_SUMMARY.md)** - Résumé technique du Sprint 3
- **[SPRINT3_FINAL_REPORT.md](SPRINT3_FINAL_REPORT.md)** - Rapport final complet (98% achevé)
- **[SPRINT3_USER_GUIDE.md](SPRINT3_USER_GUIDE.md)** - Guide utilisateur Sprint 3
- Thèmes clair/sombre avec mode système (WCAG AAA)
- Catégories personnalisées avec icônes et couleurs
- Animations et transitions fluides
- Notifications toast
- Raccourcis clavier complets

### Sprint 4 - Recherche Avancée (Janvier 2025)
- **[SPRINT-4-FINAL-REPORT.md](SPRINT-4-FINAL-REPORT.md)** - Rapport final Sprint 4
- **[S4-I1-IMPLEMENTATION-REPORT.md](S4-I1-IMPLEMENTATION-REPORT.md)** - Rapport d'implémentation I1
- **[S4-I4-I5-IMPLEMENTATION-REPORT.md](S4-I4-I5-IMPLEMENTATION-REPORT.md)** - Rapport d'implémentation I4-I5
- Recherche fuzzy (tolérance 30%)
- Scoring de pertinence
- Historique de recherche
- Métriques UI/UX

### Sprint 8 - Performance (Janvier 2025)
- **[SPRINT-8-FINAL-REPORT.md](SPRINT-8-FINAL-REPORT.md)** - Rapport final Sprint 8
- **[SPRINT-8-PERFORMANCE-GUIDE.md](SPRINT-8-PERFORMANCE-GUIDE.md)** - Guide de performance
- Pagination dans HistoryViewModel
- ObservableRangeCollection
- Optimisations LINQ
- Réduction CPU (60%)

### Sprint 9 - Windows Integration (Janvier 2025)
- **[SPRINT-9-FINAL-REPORT.md](SPRINT-9-FINAL-REPORT.md)** - Rapport final Sprint 9
- **[WINSCRIPT-ANALYSIS.md](WINSCRIPT-ANALYSIS.md)** - Analyse des scripts Windows

### Autres Sprints
- **[sprint-prompts.md](sprint-prompts.md)** - Prompts utilisés pour les sprints

---

## Sécurité

### Audit de Sécurité (Janvier 2025)

**15 vulnérabilités critiques corrigées :**

- **[SECURITY.md](SECURITY.md)** - Politique de sécurité complète
- **[SECURITY_AUDIT_REPORT.md](SECURITY_AUDIT_REPORT.md)** - Rapport d'audit de sécurité détaillé
- **[SECURITY_EXECUTIVE_SUMMARY.md](SECURITY_EXECUTIVE_SUMMARY.md)** - Résumé exécutif sécurité
- **[SECURITY_ANALYSIS_INDEX.md](SECURITY_ANALYSIS_INDEX.md)** - Index des analyses de sécurité
- **[SECURITY_FIXES.md](SECURITY_FIXES.md)** - Liste des corrections de sécurité
- **[SECURITY_VULNERABILITIES_MAP.md](SECURITY_VULNERABILITIES_MAP.md)** - Carte des vulnérabilités
- **[SECURITY_REPORT_README.md](SECURITY_REPORT_README.md)** - Guide de lecture des rapports
- **[SECURITY_PHASE1_COMPLETE.md](SECURITY_PHASE1_COMPLETE.md)** - Phase 1 complète

### Vulnérabilités Corrigées

- ✅ Protection contre l'injection de commandes
- ✅ Protection path traversal (validation stricte des chemins)
- ✅ Correction de memory leaks (SemaphoreSlim non disposés)
- ✅ Résolution de race conditions dans ViewModels
- ✅ Protection contre l'information disclosure (exceptions)
- ✅ Protection DoS (limites sur fichiers et collections)
- ✅ Validation complète des entrées utilisateur
- ✅ Thread-safety améliorée

---

## Performance

### Optimisations (Janvier 2025)

- **[PERFORMANCE_ANALYSIS_INDEX.md](PERFORMANCE_ANALYSIS_INDEX.md)** - Index des analyses de performance
- **[PERFORMANCE_ANALYSIS_SUMMARY.md](PERFORMANCE_ANALYSIS_SUMMARY.md)** - Résumé des analyses
- **[PERFORMANCE_QUICK_START.md](PERFORMANCE_QUICK_START.md)** - Guide de démarrage rapide
- **[PERFORMANCE_PHASE4_COMPLETE.md](PERFORMANCE_PHASE4_COMPLETE.md)** - Phase 4 complète
- **[performance_analysis.md](performance_analysis.md)** - Analyse de performance détaillée
- **[performance_detailed_metrics.md](performance_detailed_metrics.md)** - Métriques détaillées

### Optimisations Windows

- **[OPTIMISATION-WINDOWS.md](OPTIMISATION-WINDOWS.md)** - Guide d'optimisation Windows (95 KB)
- **[PACKAGE-MANAGERS.md](PACKAGE-MANAGERS.md)** - Gestionnaires de paquets Windows

### Améliorations Clés

- ⚡ **Pagination** : Charge 50 entrées au lieu de 1000
- ⚡ **ObservableRangeCollection** : Single UI notification
- ⚡ **Filtrage optimisé** : Filtre avant chargement
- ⚡ **Magic numbers** : Constantes nommées
- ⚡ **LINQ optimisé** : Énumérations uniques
- ⚡ **Timer interval** : 250ms au lieu de 100ms (60% CPU reduction)

---

## Qualité de Code

### Analyses et Reviews

- **[START_HERE_CODE_REVIEW.md](START_HERE_CODE_REVIEW.md)** - Point de départ pour la revue de code
- **[CODE_REVIEW_MASTER_REPORT.md](CODE_REVIEW_MASTER_REPORT.md)** - Rapport maître de revue de code
- **[CODE_ANALYSIS_INDEX.md](CODE_ANALYSIS_INDEX.md)** - Index des analyses de code
- **[CODE_STYLE_ANALYSIS.md](CODE_STYLE_ANALYSIS.md)** - Analyse du style de code
- **[CODE_ISSUES_SUMMARY.md](CODE_ISSUES_SUMMARY.md)** - Résumé des problèmes de code
- **[QUALITY_PHASE3_COMPLETE.md](QUALITY_PHASE3_COMPLETE.md)** - Phase 3 qualité complète

### Audits

- **[AUDIT_COMPLET_2025.md](AUDIT_COMPLET_2025.md)** - Audit complet 2025
- **[AUDIT_COMPLET_RAPPORT.md](AUDIT_COMPLET_RAPPORT.md)** - Rapport d'audit complet

### Stabilité

- **[STABILITY_PHASE2_COMPLETE.md](STABILITY_PHASE2_COMPLETE.md)** - Phase 2 stabilité complète

**8 problèmes de stabilité résolus :**
- ✅ Gestion appropriée des ressources (IDisposable pattern)
- ✅ Locks pour sections critiques
- ✅ Timer cleanup dans ExecutionViewModel
- ✅ Fragile checks remplacés par flags explicites

---

## Tests

### Rapports de Tests

- **[TESTING_REPORT.md](TESTING_REPORT.md)** - Rapport de tests complet
- **[TESTING_EVIDENCE.md](TESTING_EVIDENCE.md)** - Preuves de tests
- **[DEBUGGING_REPORT.md](DEBUGGING_REPORT.md)** - Rapport de débogage

### Couverture de Tests

| Composant | Tests | Couverture |
|-----------|-------|------------|
| **SettingsService** | 14 tests | 100% pass |
| **SearchService** | 50+ tests | ~90% |
| **TextNormalizer** | 35+ tests | ~95% |

### Exécution des Tests

```bash
# Tous les tests
dotnet test

# Tests spécifiques
dotnet test --filter "FullyQualifiedName~SearchServiceTests"

# Avec couverture
dotnet test --collect:"XPlat Code Coverage"
```

---

## Migration et Déploiement

### Migration

- **[MIGRATION_NOTES.md](MIGRATION_NOTES.md)** - Notes de migration entre versions
- **[S2-MIGRATION-GUIDE.md](S2-MIGRATION-GUIDE.md)** - Guide de migration Sprint 2

### Post-Mortem

- **[POST_MORTEM.md](POST_MORTEM.md)** - Analyse post-mortem des incidents

### Pull Requests

- **[PR_DESCRIPTION.md](PR_DESCRIPTION.md)** - Templates de description PR

---

## Guides Techniques

### Recherche

- **[SEARCH_FUNCTIONALITY.md](SEARCH_FUNCTIONALITY.md)** - Documentation complète de la recherche (35 KB)
  - Normalisation de texte
  - Fuzzy matching (Levenshtein)
  - Scoring de pertinence
  - Historique de recherche
  - 85+ tests automatisés

### Thèmes

- **[THEME_ANALYSIS.md](THEME_ANALYSIS.md)** - Analyse des thèmes
- **[THEME_SOLUTION.md](THEME_SOLUTION.md)** - Solution de gestion des thèmes

### Refactoring

- **[RECOMMENDED_REFACTORINGS.md](RECOMMENDED_REFACTORINGS.md)** - Refactorings recommandés
- **[SOLUTION_V2.md](SOLUTION_V2.md)** - Solution version 2

### Prompts

- **[PROMPTS_FOR_NEXT_SESSIONS.md](PROMPTS_FOR_NEXT_SESSIONS.md)** - Prompts pour les prochaines sessions

### Résumés

- **[SUMMARY.md](SUMMARY.md)** - Résumé général du projet

---

## Contribuer au Projet

### Guide de Contribution

Pour contribuer à TwinShell :

1. **Forkez** le projet
2. Créez une **branche** pour votre fonctionnalité
3. **Commitez** vos changements
4. **Pushez** vers la branche
5. Ouvrez une **Pull Request**

### Standards de Code

- **Architecture** : MVVM avec CommunityToolkit.Mvvm
- **Naming** : PascalCase pour classes/méthodes, camelCase pour variables
- **Documentation** : XML comments sur toutes les APIs publiques
- **Tests** : Couverture minimale de 80%
- **Commits** : Messages clairs et descriptifs (en anglais)

### Ressources Utiles

- 📖 [README Utilisateur](../../README.md)
- 🚀 [Guide de Démarrage Rapide](../QuickStart.md)
- 📘 [Guide Utilisateur](../UserGuide.md)
- ❓ [FAQ](../FAQ.md)

---

## Ressources Externes

### Documentation Microsoft

- [.NET 8 Documentation](https://learn.microsoft.com/fr-fr/dotnet/core/whats-new/dotnet-8)
- [WPF Documentation](https://learn.microsoft.com/fr-fr/dotnet/desktop/wpf/)
- [Entity Framework Core](https://learn.microsoft.com/fr-fr/ef/core/)
- [PowerShell Documentation](https://learn.microsoft.com/fr-fr/powershell/)

### Communauté

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [SQLite](https://www.sqlite.org/docs.html)

---

## Contact

**Questions techniques ?** Ouvrez une issue sur [GitHub Issues](https://github.com/VBlackJack/TwinShell/issues)

**Discussions ?** Rejoignez [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)

---

*Dernière mise à jour : 2025-01-18*
*Version : 1.1.0*
