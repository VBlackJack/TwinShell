# TwinShell - Sprint Prompts Autonomes

Ce document contient les prompts de sprint autonomes générés à partir de la roadmap TwinShell.

## Vue d'ensemble de la roadmap

- **Sprint 1 (S1)** : MVP - ✅ **COMPLÉTÉ**
- **Sprint 2 (S2)** : User Personalization & History (2-3 semaines)
- **Sprint 3 (S3)** : UI/UX & Customization (2-3 semaines)
- **Sprint 4 (S4)** : Advanced Features & Integration (2-3 semaines)

---

## Sprint 2 - User Personalization & History

```xml
<sprint_session>
  <metadata>
    <sprint_id>S2</sprint_id>
    <title>User Personalization &amp; History</title>
    <duration>2-3 semaines</duration>
    <dependencies>
      <completed>Sprint 1 (MVP)</completed>
    </dependencies>
  </metadata>

  <objectives>
    <primary>Permettre aux utilisateurs de personnaliser leur expérience et de garder un historique des commandes utilisées</primary>
    <business_value>
      <item>Augmenter la productivité des utilisateurs via l'accès rapide aux commandes fréquentes</item>
      <item>Améliorer la rétention en permettant la sauvegarde/restauration de configurations</item>
      <item>Réduire le temps de recherche grâce aux favoris et à l'historique</item>
    </business_value>
    <kpis>
      <kpi>80%+ des utilisateurs utilisent l'historique dans les 7 jours</kpi>
      <kpi>50%+ des utilisateurs créent au moins 3 favoris</kpi>
      <kpi>Export/Import fonctionne sans perte de données (100% fidélité)</kpi>
    </kpis>
  </objectives>

  <backlog_prioritized>
    <item priority="1" id="S2-I1">
      <title>Historique de commandes</title>
      <description>Tracker et afficher l'historique des commandes générées/copiées par l'utilisateur</description>
      <user_story>En tant qu'administrateur système, je veux voir l'historique de mes commandes récentes pour les réutiliser rapidement sans rechercher à nouveau</user_story>
      <acceptance_criteria>
        <criterion>L'historique stocke chaque commande générée avec timestamp, action source, et paramètres</criterion>
        <criterion>UI affiche les 50 dernières commandes dans un panneau dédié</criterion>
        <criterion>Possibilité de copier, réutiliser, ou supprimer une entrée d'historique</criterion>
        <criterion>Recherche dans l'historique fonctionne (texte, date, catégorie)</criterion>
        <criterion>Nettoyage automatique après 90 jours (configurable)</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer modèle CommandHistory (EF Core): Id, UserId, ActionId, GeneratedCommand, Parameters JSON, CreatedAt</task>
        <task>Ajouter DbSet CommandHistories dans TwinShellDbContext</task>
        <task>Migration EF Core pour table CommandHistory</task>
        <task>Implémenter CommandHistoryService (Add, GetRecent, Search, Delete, Cleanup)</task>
        <task>Créer HistoryViewModel avec ObservableCollection et commandes (Copy, Reuse, Delete)</task>
        <task>Concevoir UserControl HistoryPanel.xaml (liste + recherche + boutons)</task>
        <task>Intégrer HistoryPanel dans MainWindow (panneau latéral ou onglet)</task>
        <task>Déclencher Add dans CommandHistoryService lors de "Copier vers presse-papiers"</task>
        <task>Implémenter job de nettoyage automatique (background task ou au démarrage)</task>
      </technical_tasks>
      <tests>
        <test>CommandHistoryService.Add crée une entrée avec timestamp correct</test>
        <test>GetRecent retourne les 50 dernières commandes triées par date DESC</test>
        <test>Search filtre correctement par texte, date, ActionId</test>
        <test>Cleanup supprime les entrées > 90 jours</test>
        <test>HistoryViewModel charge les données au démarrage</test>
        <test>UI: copier une commande depuis l'historique met à jour le presse-papiers</test>
      </tests>
    </item>

    <item priority="2" id="S2-I2">
      <title>Système de favoris</title>
      <description>Permettre aux utilisateurs de marquer des actions comme favorites pour y accéder rapidement</description>
      <user_story>En tant qu'utilisateur fréquent, je veux marquer mes actions courantes comme favorites pour les retrouver en un clic</user_story>
      <acceptance_criteria>
        <criterion>Bouton "Ajouter aux favoris" (étoile) visible sur chaque action</criterion>
        <criterion>Section "Favoris" dans le menu de navigation (au-dessus des catégories)</criterion>
        <criterion>Les favoris persistent en base de données</criterion>
        <criterion>Possibilité de retirer des favoris</criterion>
        <criterion>Limite de 50 favoris par utilisateur</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer modèle UserFavorite (EF Core): Id, UserId, ActionId, CreatedAt, DisplayOrder</task>
        <task>Migration EF Core pour table UserFavorites</task>
        <task>Implémenter FavoritesService (Add, Remove, GetAll, Reorder)</task>
        <task>Ajouter propriété IsFavorite dans ActionViewModel (computed)</task>
        <task>Ajouter ToggleFavoriteCommand dans ActionViewModel</task>
        <task>Créer FavoritesViewModel avec liste de favoris</task>
        <task>Ajouter bouton étoile dans ActionListItem.xaml (Style WPF avec toggle)</task>
        <task>Ajouter section "Favoris" dans CategoriesPanel avec compteur</task>
        <task>Implémenter filtre "ShowFavoritesOnly" dans MainViewModel</task>
        <task>Limiter à 50 favoris (message d'erreur si dépassement)</task>
      </technical_tasks>
      <tests>
        <test>FavoritesService.Add ajoute un favori et retourne succès</test>
        <test>Add retourne échec si limite de 50 atteinte</test>
        <test>Remove supprime correctement un favori</test>
        <test>GetAll retourne favoris triés par DisplayOrder</test>
        <test>ToggleFavoriteCommand met à jour IsFavorite et UI réagit</test>
        <test>Section Favoris affiche uniquement les actions favorites</test>
      </tests>
    </item>

    <item priority="3" id="S2-I3">
      <title>Export/Import de configurations</title>
      <description>Exporter et importer les favoris, historique, et préférences utilisateur</description>
      <user_story>En tant qu'administrateur multi-postes, je veux exporter ma configuration pour la réutiliser sur un autre poste</user_story>
      <acceptance_criteria>
        <criterion>Bouton "Exporter configuration" dans menu Fichier</criterion>
        <criterion>Bouton "Importer configuration" dans menu Fichier</criterion>
        <criterion>Format JSON structuré (version, favoris, historique, préférences)</criterion>
        <criterion>Import fusionne intelligemment (pas d'écrasement total)</criterion>
        <criterion>Validation du fichier JSON avant import</criterion>
        <criterion>Message de confirmation après export/import réussi</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer UserConfigurationDto: Version, ExportDate, Favorites[], History[], Preferences{}</task>
        <task>Implémenter ConfigurationExportService.ExportToJson(filePath)</task>
        <task>Implémenter ConfigurationImportService.ImportFromJson(filePath)</task>
        <task>Ajouter validation JSON schema (Newtonsoft.Json.Schema ou System.Text.Json)</task>
        <task>Implémenter logique de merge (éviter doublons favoris par ActionId)</task>
        <task>Créer ExportCommand et ImportCommand dans MainViewModel</task>
        <task>Ajouter MenuItems dans MainWindow.xaml (Fichier > Export/Import)</task>
        <task>Utiliser SaveFileDialog et OpenFileDialog (WPF)</task>
        <task>Afficher notifications success/error (SnackBar ou MessageBox)</task>
        <task>Logger les opérations d'export/import pour debug</task>
      </technical_tasks>
      <tests>
        <test>ExportToJson crée fichier JSON valide avec structure attendue</test>
        <test>JSON contient tous les favoris de l'utilisateur</test>
        <test>ImportFromJson charge favoris et les insère en base</test>
        <test>Import rejette fichier JSON invalide avec message d'erreur</test>
        <test>Import ne crée pas de doublons pour favoris existants</test>
        <test>Versionning: import d'une version future affiche warning</test>
      </tests>
    </item>

    <item priority="4" id="S2-I4">
      <title>Recherche dans l'historique</title>
      <description>Ajouter filtres et recherche avancée dans le panneau historique</description>
      <user_story>En tant qu'utilisateur power, je veux filtrer mon historique par date, catégorie, ou plateforme pour retrouver rapidement une ancienne commande</user_story>
      <acceptance_criteria>
        <criterion>Barre de recherche dans HistoryPanel</criterion>
        <criterion>Filtres par date (aujourd'hui, 7 jours, 30 jours, personnalisé)</criterion>
        <criterion>Filtres par plateforme (Windows/Linux/Both)</criterion>
        <criterion>Filtres par catégorie</criterion>
        <criterion>Résultats mis à jour en temps réel</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Ajouter SearchText, DateFilter, PlatformFilter, CategoryFilter dans HistoryViewModel</task>
        <task>Implémenter méthode FilteredHistory (IEnumerable avec LINQ)</task>
        <task>Créer UI pour filtres (ComboBox, DatePicker) dans HistoryPanel.xaml</task>
        <task>Binder filtres aux propriétés ViewModel</task>
        <task>Implémenter ReactiveCommand pour déclencher filtrage au changement</task>
        <task>Ajouter indicateur visuel "X résultats trouvés"</task>
      </technical_tasks>
      <tests>
        <test>SearchText filtre par commande contenant le texte (case-insensitive)</test>
        <test>DateFilter retourne uniquement commandes dans plage sélectionnée</test>
        <test>PlatformFilter filtre par Windows/Linux</test>
        <test>Combinaison de filtres fonctionne (AND logique)</test>
        <test>UI met à jour résultats en temps réel</test>
      </tests>
    </item>

    <item priority="5" id="S2-I5">
      <title>Widget commandes récentes</title>
      <description>Afficher les 5 commandes les plus récentes sur la page d'accueil</description>
      <user_story>En tant qu'utilisateur, je veux voir mes 5 dernières commandes dès l'ouverture de l'application pour les réutiliser immédiatement</user_user_story>
      <acceptance_criteria>
        <criterion>Widget "Commandes récentes" sur page d'accueil (MainWindow)</criterion>
        <criterion>Affiche 5 dernières commandes avec timestamp relatif</criterion>
        <criterion>Click sur une commande la copie dans le presse-papiers</criterion>
        <criterion>Bouton "Voir tout" redirige vers panneau Historique complet</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer RecentCommandsViewModel avec ObservableCollection des 5 dernières</task>
        <task>Créer UserControl RecentCommandsWidget.xaml</task>
        <task>Implémenter conversion timestamp vers "Il y a 5 min", "Il y a 2h" (ValueConverter)</task>
        <task>Ajouter CopyCommand pour chaque item</task>
        <task>Intégrer widget dans MainWindow (zone supérieure ou dashboard)</task>
        <task>Auto-refresh du widget lors d'ajout d'une nouvelle commande</task>
      </technical_tasks>
      <tests>
        <test>Widget affiche 5 commandes max</test>
        <test>Timestamp relatif est correct ("Il y a 1 min")</test>
        <test>Click copie commande dans presse-papiers</test>
        <test>Bouton "Voir tout" navigue vers HistoryPanel</test>
        <test>Widget se met à jour automatiquement après nouvelle commande</test>
      </tests>
    </item>
  </backlog_prioritized>

  <risks_and_mitigations>
    <risk>
      <description>Performances dégradées si historique contient des milliers d'entrées</description>
      <impact>Moyen</impact>
      <mitigation>Implémenter pagination (50 résultats par page) + index SQL sur CreatedAt</mitigation>
    </risk>
    <risk>
      <description>Import de fichier JSON malformé peut crasher l'application</description>
      <impact>Élevé</impact>
      <mitigation>Validation stricte du JSON + try-catch avec messages d'erreur clairs</mitigation>
    </risk>
    <risk>
      <description>Conflit de merge lors d'import de configurations multiples</description>
      <impact>Faible</impact>
      <mitigation>Stratégie de merge documentée (garder existant + ajouter nouveau, pas d'écrasement)</mitigation>
    </risk>
  </risks_and_mitigations>

  <deliverables>
    <deliverable type="code">
      <item>Models: CommandHistory, UserFavorite, UserConfigurationDto</item>
      <item>Services: CommandHistoryService, FavoritesService, ConfigurationExportService, ConfigurationImportService</item>
      <item>ViewModels: HistoryViewModel, FavoritesViewModel, RecentCommandsViewModel</item>
      <item>Views: HistoryPanel.xaml, RecentCommandsWidget.xaml</item>
      <item>Migrations EF Core pour CommandHistory et UserFavorites</item>
    </deliverable>
    <deliverable type="tests">
      <item>Tests unitaires pour tous les services (70%+ couverture)</item>
      <item>Tests d'intégration pour Export/Import (validation JSON)</item>
      <item>Tests UI pour HistoryPanel et FavoritesPanel (ViewModel bindings)</item>
    </deliverable>
    <deliverable type="documentation">
      <item>README mis à jour avec features S2</item>
      <item>Documentation utilisateur pour Export/Import</item>
      <item>Schema JSON de configuration (pour validation externe)</item>
    </deliverable>
    <deliverable type="ci">
      <item>Build verte sur toutes les plateformes</item>
      <item>Tests passent à 100%</item>
      <item>Aucune régression sur fonctionnalités S1</item>
    </deliverable>
  </deliverables>

  <technical_constraints>
    <constraint>Respecter architecture MVVM existante</constraint>
    <constraint>Utiliser EF Core pour persistance (pas de SQL brut)</constraint>
    <constraint>Format JSON pour export/import (pas XML ni binaire)</constraint>
    <constraint>Compatible .NET 8.0 et Windows 10+</constraint>
    <constraint>Pas de dépendance externe supplémentaire (sauf si critique)</constraint>
  </technical_constraints>

  <acceptance_criteria_global>
    <criterion>Tous les tests unitaires et d'intégration passent</criterion>
    <criterion>Application compile sans warning</criterion>
    <criterion>Aucune régression sur fonctionnalités S1 (tests de non-régression)</criterion>
    <criterion>Documentation utilisateur complète pour nouvelles features</criterion>
    <criterion>Export/Import testé manuellement avec fichier de 100+ favoris</criterion>
    <criterion>Performance: Historique de 1000 commandes charge en < 200ms</criterion>
  </acceptance_criteria_global>

  <handoff>
    <next_item_prompt>
      <title>Prompt d'implémentation - Item S2-I1: Historique de commandes</title>
      <content><![CDATA[
Tu es un développeur senior .NET/WPF travaillant sur TwinShell (Sprint 2).

**Objectif**: Implémenter le système d'historique de commandes (Item S2-I1).

**Contexte**:
- Architecture existante: MVVM, EF Core, SQLite, CommunityToolkit.Mvvm
- Namespace: TwinShell.Core (Models/Services), TwinShell.Persistence (DbContext), TwinShell.App (ViewModels/Views)
- DbContext actuel: TwinShellDbContext dans TwinShell.Persistence

**Tâches**:
1. Créer modèle `CommandHistory` (TwinShell.Core/Models):
   - Id (Guid), UserId (string, nullable pour MVP), ActionId (Guid, FK vers Action)
   - GeneratedCommand (string), Parameters (string, JSON sérialisé)
   - CreatedAt (DateTime), Platform (enum)

2. Ajouter `DbSet<CommandHistory> CommandHistories` dans TwinShellDbContext

3. Créer migration EF Core: `dotnet ef migrations add AddCommandHistory --project src/TwinShell.Persistence`

4. Implémenter `CommandHistoryService` (TwinShell.Core/Services):
   - AddCommandAsync(ActionId, GeneratedCommand, Parameters)
   - GetRecentAsync(int count = 50)
   - SearchAsync(string query, DateTime? from, DateTime? to)
   - DeleteAsync(Guid historyId)
   - CleanupOldEntriesAsync(int daysToKeep = 90)

5. Créer `HistoryViewModel` (TwinShell.App/ViewModels):
   - ObservableCollection<CommandHistoryViewModel> HistoryItems
   - SearchText (string), DateFilter (enum: All, Today, Week, Month)
   - CopyCommand, DeleteCommand, ClearAllCommand
   - LoadHistoryAsync() au constructeur

6. Créer `HistoryPanel.xaml` (TwinShell.App/Views):
   - ListView pour afficher historique (template: commande, timestamp, catégorie)
   - SearchBox, ComboBox pour DateFilter
   - Boutons: Copier, Supprimer, Tout effacer

7. Intégrer dans `MainWindow.xaml`:
   - Ajouter TabItem "Historique" ou panneau latéral

8. Déclencher `AddCommandAsync` dans `CommandGeneratorViewModel` lors du clic "Copier"

9. Implémenter job de nettoyage: appeler `CleanupOldEntriesAsync` au démarrage de l'app

**Tests requis** (TwinShell.Core.Tests):
- CommandHistoryServiceTests: Add, GetRecent, Search, Delete, Cleanup
- HistoryViewModelTests: LoadHistory, Search, CopyCommand

**Critères d'acceptation**:
- [ ] Migration EF Core appliquée sans erreur
- [ ] Service crée des entrées d'historique avec timestamp UTC
- [ ] UI affiche 50 dernières commandes triées par date DESC
- [ ] Recherche filtre correctement
- [ ] Cleanup supprime entrées > 90 jours
- [ ] Tests unitaires passent (70%+ couverture)

**Livrable**: Code + tests + migration, prêt pour commit.
      ]]></content>
    </next_item_prompt>

    <alternative_paths>
      <path id="S2-I2-parallel">
        <title>Implémenter Favoris (S2-I2) en parallèle</title>
        <description>Si ressources disponibles, implémenter le système de favoris en parallèle (pas de dépendance technique avec S2-I1)</description>
      </path>
      <path id="S2-I3-after-I2">
        <title>Prioriser Export/Import (S2-I3) après Favoris</title>
        <description>Export/Import nécessite que Favoris (S2-I2) soit complété pour exporter les favoris</description>
      </path>
    </alternative_paths>
  </handoff>
</sprint_session>
```

---

## Sprint 3 - UI/UX & Customization

```xml
<sprint_session>
  <metadata>
    <sprint_id>S3</sprint_id>
    <title>UI/UX &amp; Customization</title>
    <duration>2-3 semaines</duration>
    <dependencies>
      <completed>Sprint 1 (MVP), Sprint 2 (User Personalization)</completed>
    </dependencies>
  </metadata>

  <objectives>
    <primary>Améliorer l'expérience utilisateur via un mode sombre, des catégories personnalisées, et une interface plus accessible</primary>
    <business_value>
      <item>Augmenter la satisfaction utilisateur (dark mode = feature #1 demandée)</item>
      <item>Permettre adaptation aux workflows spécifiques (catégories custom)</item>
      <item>Améliorer accessibilité (conformité WCAG 2.1 niveau AA)</item>
    </business_value>
    <kpis>
      <kpi>60%+ des utilisateurs activent le mode sombre dans les 30 jours</kpi>
      <kpi>30%+ des utilisateurs créent au moins 1 catégorie personnalisée</kpi>
      <kpi>Score d'accessibilité > 90/100 (WAVE tool)</kpi>
    </kpis>
  </objectives>

  <backlog_prioritized>
    <item priority="1" id="S3-I1">
      <title>Mode sombre (Dark Mode)</title>
      <description>Implémenter un thème sombre complet avec toggle dans les paramètres</description>
      <user_story>En tant qu'utilisateur travaillant en environnement sombre, je veux activer un mode sombre pour réduire la fatigue oculaire</user_story>
      <acceptance_criteria>
        <criterion>Toggle "Mode sombre" dans menu Paramètres</criterion>
        <criterion>Thème sombre appliqué à tous les contrôles WPF (fenêtres, listes, boutons, inputs)</criterion>
        <criterion>Palette de couleurs respectant contraste WCAG AA (ratio 4.5:1 minimum)</criterion>
        <criterion>Préférence sauvegardée et restaurée au redémarrage</criterion>
        <criterion>Transition fluide entre thèmes (pas de scintillement)</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer ResourceDictionary LightTheme.xaml avec couleurs actuelles</task>
        <task>Créer ResourceDictionary DarkTheme.xaml avec palette sombre (fond #1E1E1E, texte #E0E0E0, accents #007ACC)</task>
        <task>Définir styles pour tous les contrôles (Button, TextBox, ListView, ComboBox, etc.)</task>
        <task>Créer ThemeService pour gérer changement de thème dynamique</task>
        <task>Ajouter propriété UserPreferences.Theme (enum: Light, Dark, System)</task>
        <task>Implémenter ThemeService.ApplyTheme(Theme) qui merge ResourceDictionary</task>
        <task>Créer SettingsViewModel avec ThemeToggleCommand</task>
        <task>Créer SettingsWindow.xaml avec RadioButtons pour Light/Dark/System</task>
        <task>Ajouter MenuItem "Paramètres" dans MainWindow menu</task>
        <task>Sauvegarder préférence dans UserSettings.json (ou table Settings en DB)</task>
        <task>Charger thème au démarrage de l'application (App.xaml.cs)</task>
      </technical_tasks>
      <tests>
        <test>ThemeService.ApplyTheme(Dark) charge DarkTheme.xaml</test>
        <test>Vérifier contraste couleurs (automated avec Accessibility Insights)</test>
        <test>Préférence Theme persiste après redémarrage</test>
        <test>UI: tous les contrôles réagissent au changement de thème</test>
        <test>Pas de XAML exception lors du switch Light <-> Dark</test>
      </tests>
    </item>

    <item priority="2" id="S3-I2">
      <title>Catégories personnalisées</title>
      <description>Permettre aux utilisateurs de créer, modifier, et supprimer leurs propres catégories</description>
      <user_story>En tant qu'administrateur avec workflow spécifique, je veux créer mes propres catégories (ex: "Backup Daily Tasks") pour organiser mes actions favorites</user_story>
      <acceptance_criteria>
        <criterion>Bouton "Gérer catégories" dans panneau de navigation</criterion>
        <criterion>Fenêtre modale pour ajouter/éditer/supprimer catégories</criterion>
        <criterion>Catégories custom peuvent contenir des actions existantes (assignation multiple)</criterion>
        <criterion>Icônes et couleurs personnalisables pour chaque catégorie</criterion>
        <criterion>Catégories système (AD, DNS, etc.) non supprimables mais masquables</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer modèle CustomCategory: Id, Name, IconKey, ColorHex, IsSystemCategory, DisplayOrder, CreatedAt</task>
        <task>Créer table de liaison ActionCategoryMapping (many-to-many): ActionId, CategoryId</task>
        <task>Migrations EF Core pour CustomCategory et ActionCategoryMapping</task>
        <task>Implémenter CustomCategoryService (CRUD + Reorder)</task>
        <task>Créer CategoryManagementViewModel avec liste de catégories</task>
        <task>Créer CategoryManagementWindow.xaml (liste + formulaire ajout/édition)</task>
        <task>Ajouter ColorPicker et IconSelector dans formulaire</task>
        <task>Implémenter assignation d'actions à catégorie (drag & drop ou modal)</task>
        <task>Mettre à jour CategoriesPanel pour afficher catégories custom</task>
        <task>Implémenter filtrage par catégorie custom dans MainViewModel</task>
        <task>Ajouter option "Masquer catégorie" (soft delete ou flag Hidden)</task>
      </technical_tasks>
      <tests>
        <test>CustomCategoryService.Create ajoute catégorie avec propriétés correctes</test>
        <test>ActionCategoryMapping crée liaison many-to-many</test>
        <test>Impossible de supprimer catégorie système</test>
        <test>UI: catégorie custom apparaît dans navigation</test>
        <test>Filtrer par catégorie custom affiche uniquement actions assignées</test>
        <test>IconSelector affiche 20+ icônes disponibles</test>
      </tests>
    </item>

    <item priority="3" id="S3-I3">
      <title>Améliorations UI/UX</title>
      <description>Polish de l'interface: animations, feedback visuel, responsive layout</description>
      <user_story>En tant qu'utilisateur, je veux une interface fluide et réactive avec des feedbacks visuels clairs</user_user_story>
      <acceptance_criteria>
        <criterion>Animations de transition pour navigation (fade, slide)</criterion>
        <criterion>Loading spinners pendant opérations async (recherche, chargement historique)</criterion>
        <criterion>Toast notifications pour actions (favori ajouté, commande copiée, etc.)</criterion>
        <criterion>Tooltips informatifs sur tous les boutons et contrôles</criterion>
        <criterion>Responsive layout: fenêtre redimensionnable min 800x600, max fullscreen</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer Storyboard animations pour page transitions (fade in/out, slide)</task>
        <task>Ajouter ProgressRing (WPF) dans MainViewModel.IsLoading</task>
        <task>Implémenter NotificationService avec queue de toasts (ou utiliser WPF NotifyIcon)</task>
        <task>Ajouter Tooltips dans tous les XAML (Button, TextBox, etc.)</task>
        <task>Implémenter Grid responsive avec MinWidth/MinHeight dans MainWindow</task>
        <task>Améliorer styles Button (hover effect, ripple animation)</task>
        <task>Ajouter Skeleton Loaders pour listes (pendant chargement)</task>
        <task>Optimiser performance rendering (virtualisation ListView si > 100 items)</task>
      </technical_tasks>
      <tests>
        <test>Animation fade fonctionne lors changement de vue</test>
        <test>ProgressRing affiché pendant LoadHistoryAsync</test>
        <test>Toast notification apparaît après ajout favori (durée 3s)</test>
        <test>Tooltips affichent texte correct au hover</test>
        <test>Fenêtre redimensionnable sans casser layout (min 800x600)</test>
      </tests>
    </item>

    <item priority="4" id="S3-I4">
      <title>Accessibilité (A11y)</title>
      <description>Améliorer accessibilité selon WCAG 2.1 niveau AA</description>
      <user_story>En tant qu'utilisateur avec déficience visuelle, je veux utiliser TwinShell avec un lecteur d'écran et navigation clavier</user_story>
      <acceptance_criteria>
        <criterion>Navigation clavier complète (Tab, Enter, Esc, Arrow keys)</criterion>
        <criterion>Support lecteur d'écran (AutomationProperties.Name sur tous les contrôles)</criterion>
        <criterion>Contraste couleurs WCAG AA (ratio 4.5:1 texte, 3:1 UI)</criterion>
        <criterion>Focus visible sur tous les éléments interactifs</criterion>
        <criterion>Shortcuts clavier documentés (Ctrl+F = recherche, Ctrl+H = historique, etc.)</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Ajouter AutomationProperties.Name et AutomationProperties.HelpText dans tous les XAML</task>
        <task>Implémenter KeyboardNavigation.TabNavigation dans layouts</task>
        <task>Définir FocusVisualStyle pour tous les contrôles interactifs</task>
        <task>Créer KeyBindingService pour gérer shortcuts globaux (Ctrl+F, Ctrl+H, etc.)</task>
        <task>Ajouter InputBindings dans MainWindow pour shortcuts</task>
        <task>Tester avec Accessibility Insights for Windows</task>
        <task>Tester avec NVDA screen reader</task>
        <task>Documenter shortcuts dans Help > Keyboard Shortcuts</task>
      </technical_tasks>
      <tests>
        <test>Navigation Tab parcourt tous les contrôles dans ordre logique</test>
        <test>Lecteur d'écran annonce correctement bouton "Copier vers presse-papiers"</test>
        <test>Ctrl+F ouvre recherche et focus dans SearchBox</test>
        <test>Contraste vérifié avec Color Contrast Analyzer (WCAG AA)</test>
        <test>Focus visible sur Button après navigation Tab</test>
      </tests>
    </item>

    <item priority="5" id="S3-I5">
      <title>Système de préférences utilisateur</title>
      <description>Centraliser toutes les préférences utilisateur (thème, langue future, layout, etc.)</description>
      <user_story>En tant qu'utilisateur, je veux configurer mes préférences dans un seul endroit</user_story>
      <acceptance_criteria>
        <criterion>Fenêtre Settings avec sections: Apparence, Comportement, Avancé</criterion>
        <criterion>Préférences sauvegardées en JSON local (%APPDATA%/TwinShell/settings.json)</criterion>
        <criterion>Réinitialisation aux valeurs par défaut possible</criterion>
        <criterion>Validation des valeurs avant sauvegarde</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer modèle UserSettings: Theme, Language (future), AutoCleanupDays, MaxHistoryItems, DefaultPlatformFilter</task>
        <task>Implémenter SettingsService (Load, Save, Reset to Default)</task>
        <task>Créer SettingsViewModel avec propriétés bindables</task>
        <task>Créer SettingsWindow.xaml avec TabControl (Apparence, Comportement, Avancé)</task>
        <task>Sérialiser/Désérialiser en JSON (System.Text.Json)</task>
        <task>Charger settings au démarrage (App.xaml.cs)</task>
        <task>Ajouter bouton "Réinitialiser" avec confirmation</task>
      </technical_tasks>
      <tests>
        <test>SettingsService.Save écrit fichier JSON valide</test>
        <test>SettingsService.Load charge settings depuis JSON</test>
        <test>Reset retourne valeurs par défaut</test>
        <test>Validation empêche AutoCleanupDays < 1</test>
        <test>Settings persistent après redémarrage app</test>
      </tests>
    </item>
  </backlog_prioritized>

  <risks_and_mitigations>
    <risk>
      <description>Thème sombre peut causer des problèmes de contraste sur certains contrôles custom</description>
      <impact>Moyen</impact>
      <mitigation>Tester tous les contrôles avec Color Contrast Analyzer + review manuelle</mitigation>
    </risk>
    <risk>
      <description>Catégories custom peuvent créer confusion si trop nombreuses</description>
      <impact>Faible</impact>
      <mitigation>Limiter à 20 catégories custom + UX guideline (recommander 5-10 max)</mitigation>
    </risk>
    <risk>
      <description>Animations peuvent ralentir l'UI sur machines anciennes</description>
      <impact>Faible</impact>
      <mitigation>Ajouter option "Désactiver animations" dans Settings avancés</mitigation>
    </risk>
  </risks_and_mitigations>

  <deliverables>
    <deliverable type="code">
      <item>ResourceDictionaries: LightTheme.xaml, DarkTheme.xaml</item>
      <item>Services: ThemeService, CustomCategoryService, SettingsService, NotificationService</item>
      <item>Models: CustomCategory, ActionCategoryMapping, UserSettings</item>
      <item>ViewModels: SettingsViewModel, CategoryManagementViewModel</item>
      <item>Views: SettingsWindow.xaml, CategoryManagementWindow.xaml</item>
      <item>Migrations EF Core pour CustomCategory et ActionCategoryMapping</item>
    </deliverable>
    <deliverable type="tests">
      <item>Tests unitaires pour tous les services (70%+ couverture)</item>
      <item>Tests accessibilité avec Accessibility Insights</item>
      <item>Tests manuels dark mode sur tous les écrans</item>
    </deliverable>
    <deliverable type="documentation">
      <item>Guide utilisateur: Créer catégories personnalisées</item>
      <item>Documentation shortcuts clavier</item>
      <item>Rapport accessibilité WCAG 2.1 AA</item>
    </deliverable>
    <deliverable type="ci">
      <item>Build verte</item>
      <item>Tests passent à 100%</item>
      <item>Aucune régression S1/S2</item>
    </deliverable>
  </deliverables>

  <technical_constraints>
    <constraint>WPF ResourceDictionary pour theming (pas de lib externe sauf MaterialDesignThemes si nécessaire)</constraint>
    <constraint>Accessibilité: AutomationProperties obligatoires sur tous les contrôles</constraint>
    <constraint>Animations: max 300ms pour transitions (UX guideline)</constraint>
    <constraint>Settings en JSON local (pas en base de données pour faciliter backup)</constraint>
  </technical_constraints>

  <acceptance_criteria_global>
    <criterion>Mode sombre fonctionne sur tous les écrans sans artefact visuel</criterion>
    <criterion>Score accessibilité > 90/100 (Accessibility Insights)</criterion>
    <criterion>Navigation clavier complète (test sans souris possible)</criterion>
    <criterion>Catégories custom créables et fonctionnelles</criterion>
    <criterion>Tous tests passent + aucune régression</criterion>
  </acceptance_criteria_global>

  <handoff>
    <next_item_prompt>
      <title>Prompt d'implémentation - Item S3-I1: Mode sombre</title>
      <content><![CDATA[
Tu es un développeur senior WPF travaillant sur TwinShell (Sprint 3).

**Objectif**: Implémenter le mode sombre complet (Item S3-I1).

**Contexte**:
- Application WPF .NET 8, architecture MVVM
- Styles actuels définis dans App.xaml
- Namespace: TwinShell.App (Views/ViewModels), TwinShell.Core (Services)

**Tâches**:
1. Créer `Themes/LightTheme.xaml` (ResourceDictionary):
   - Extraire couleurs actuelles en ressources nommées (Background, Foreground, Accent, etc.)
   - Définir styles de base pour Button, TextBox, ListView, ComboBox, Label

2. Créer `Themes/DarkTheme.xaml`:
   - Palette sombre: Background #1E1E1E, Surface #2D2D30, Foreground #E0E0E0, Accent #007ACC
   - Border: #3E3E42, Hover: #3E3E42, Pressed: #505050
   - Danger: #E74856, Success: #10B981
   - Respecter contraste WCAG AA (ratio 4.5:1)

3. Créer `ThemeService` (TwinShell.Core/Services):
   ```csharp
   public enum Theme { Light, Dark, System }
   public class ThemeService
   {
       public void ApplyTheme(Theme theme);
       public Theme GetCurrentTheme();
       // Merge ResourceDictionary dans Application.Current.Resources
   }
   ```

4. Créer modèle `UserSettings` (TwinShell.Core/Models):
   - Theme (enum), AutoCleanupDays (int), autres préférences futures

5. Créer `SettingsService`:
   - LoadSettings() depuis %APPDATA%/TwinShell/settings.json
   - SaveSettings(UserSettings)
   - ResetToDefault()

6. Créer `SettingsViewModel` (TwinShell.App/ViewModels):
   - SelectedTheme (ObservableProperty)
   - SaveCommand, ResetCommand

7. Créer `SettingsWindow.xaml`:
   - RadioButtons pour Light/Dark/System
   - Boutons Sauvegarder/Annuler/Réinitialiser

8. Intégrer dans `App.xaml.cs`:
   - Charger settings au démarrage
   - Appliquer thème via ThemeService

9. Ajouter MenuItem "Paramètres" dans `MainWindow.xaml` menu

**Tests requis**:
- ThemeServiceTests: ApplyTheme charge bon ResourceDictionary
- SettingsServiceTests: Load/Save/Reset
- Vérification contraste avec Color Contrast Analyzer
- Test manuel: switch Light <-> Dark sans erreur XAML

**Critères d'acceptation**:
- [ ] Thème sombre appliqué à tous les contrôles
- [ ] Contraste WCAG AA respecté (4.5:1)
- [ ] Préférence sauvegardée et restaurée au redémarrage
- [ ] Transition fluide sans scintillement
- [ ] Tests unitaires passent

**Livrable**: Code + tests + ResourceDictionaries, prêt pour commit.
      ]]></content>
    </next_item_prompt>

    <alternative_paths>
      <path id="S3-I2-parallel">
        <title>Implémenter Catégories custom (S3-I2) en parallèle</title>
        <description>Pas de dépendance technique avec S3-I1, peut être développé en parallèle</description>
      </path>
      <path id="S3-I3-after-I1">
        <title>Prioriser UI/UX polish (S3-I3) après mode sombre</title>
        <description>Animations et polish UI bénéficient d'avoir le dark mode déjà implémenté pour tester les deux thèmes</description>
      </path>
    </alternative_paths>
  </handoff>
</sprint_session>
```

---

## Sprint 4 - Advanced Features & Integration

```xml
<sprint_session>
  <metadata>
    <sprint_id>S4</sprint_id>
    <title>Advanced Features &amp; Integration</title>
    <duration>2-3 semaines</duration>
    <dependencies>
      <completed>Sprint 1 (MVP), Sprint 2 (User Personalization), Sprint 3 (UI/UX)</completed>
    </dependencies>
  </metadata>

  <objectives>
    <primary>Ajouter fonctionnalités avancées: exécution directe PowerShell/Bash, support multi-langues, et sécurité renforcée</primary>
    <business_value>
      <item>Réduire friction: exécuter commandes sans copier-coller manuel</item>
      <item>Élargir marché: support multi-langues (FR, EN, ES)</item>
      <item>Renforcer confiance: audit log et validations de sécurité</item>
    </business_value>
    <kpis>
      <kpi>40%+ des utilisateurs utilisent exécution directe dans les 30 jours</kpi>
      <kpi>20%+ d'utilisateurs non-francophones après support multi-langues</kpi>
      <kpi>100% des commandes dangereuses requièrent confirmation explicite</kpi>
    </kpis>
  </objectives>

  <backlog_prioritized>
    <item priority="1" id="S4-I1">
      <title>Exécution directe PowerShell/Bash</title>
      <description>Permettre l'exécution de commandes directement depuis TwinShell avec capture de sortie</description>
      <user_story>En tant qu'administrateur, je veux exécuter une commande PowerShell/Bash directement depuis TwinShell et voir le résultat sans ouvrir un terminal</user_story>
      <acceptance_criteria>
        <criterion>Bouton "Exécuter" (à côté de "Copier") pour chaque commande générée</criterion>
        <criterion>Détection automatique de la plateforme (Windows = PowerShell, Linux = Bash)</criterion>
        <criterion>Panneau de sortie affichant stdout/stderr en temps réel</criterion>
        <criterion>Indicateur de progression pendant exécution</criterion>
        <criterion>Boutons "Arrêter" pour interrompre commande en cours</criterion>
        <criterion>Commandes dangereuses requièrent double confirmation</criterion>
        <criterion>Timeout configuré (30s par défaut, configurable)</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer CommandExecutionService (TwinShell.Infrastructure/Services)</task>
        <task>Implémenter ExecuteAsync(string command, Platform platform, CancellationToken ct)</task>
        <task>Utiliser System.Diagnostics.Process pour PowerShell.exe / bash</task>
        <task>Capturer stdout/stderr avec event handlers (OutputDataReceived)</task>
        <task>Créer modèle ExecutionResult: ExitCode, Stdout, Stderr, Duration, Success</task>
        <task>Implémenter gestion timeout avec CancellationTokenSource</task>
        <task>Créer ExecutionViewModel avec ObservableCollection<OutputLine></task>
        <task>Créer OutputPanel.xaml (console-like avec TextBlock scrollable)</task>
        <task>Ajouter ExecuteCommand dans CommandGeneratorViewModel</task>
        <task>Implémenter confirmation dialog pour commandes Level=Dangerous</task>
        <task>Logger toutes les exécutions dans CommandHistory (flag IsExecuted=true)</task>
        <task>Ajouter bouton "Stop" qui appelle ct.Cancel()</task>
      </technical_tasks>
      <tests>
        <test>ExecuteAsync("echo test") retourne Success avec Stdout="test"</test>
        <test>ExecuteAsync d'une commande invalide retourne Success=false avec Stderr</test>
        <test>Timeout après 30s annule commande et retourne erreur</test>
        <test>CancellationToken.Cancel() interrompt Process correctement</test>
        <test>Commande dangereuse affiche dialog de confirmation</test>
        <test>Historique enregistre IsExecuted=true après exécution</test>
      </tests>
    </item>

    <item priority="2" id="S4-I2">
      <title>Support multi-langues (i18n)</title>
      <description>Internationaliser l'application pour supporter FR, EN, ES</description>
      <user_story>En tant qu'utilisateur anglophone, je veux utiliser TwinShell dans ma langue native</user_story>
      <acceptance_criteria>
        <criterion>Support FR (défaut), EN, ES</criterion>
        <criterion>Sélecteur de langue dans Settings</criterion>
        <criterion>Tous les textes UI traduits (labels, boutons, messages)</criterion>
        <criterion>Documentation actions traduite (Title, Description)</criterion>
        <criterion>Changement de langue sans redémarrage</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Installer package Microsoft.Extensions.Localization</task>
        <task>Créer fichiers .resx: Resources.resx (FR), Resources.en.resx, Resources.es.resx</task>
        <task>Traduire tous les strings UI (labels, boutons, tooltips, messages)</task>
        <task>Créer LocalizationService avec ChangeLanguage(CultureInfo culture)</task>
        <task>Implémenter IValueConverter LocalizedStringConverter pour binding XAML</task>
        <task>Ajouter Culture dans UserSettings</task>
        <task>Créer table ActionTranslation (ActionId, CultureCode, Title, Description)</task>
        <task>Seed données traduites pour 30 actions (EN, ES)</task>
        <task>Mettre à jour ActionService.GetAll() pour charger traduction selon culture</task>
        <task>Ajouter ComboBox langue dans SettingsWindow</task>
        <task>Appliquer culture au démarrage (Thread.CurrentThread.CurrentUICulture)</task>
      </technical_tasks>
      <tests>
        <test>LocalizationService.ChangeLanguage("en") charge Resources.en.resx</test>
        <test>UI affiche textes anglais après changement de langue</test>
        <test>ActionService retourne Title traduit selon CurrentUICulture</test>
        <test>Changement de langue ne requiert pas redémarrage</test>
        <test>Fallback FR si traduction manquante</test>
      </tests>
    </item>

    <item priority="3" id="S4-I3">
      <title>Audit log & sécurité renforcée</title>
      <description>Logger toutes les actions critiques et renforcer validations de sécurité</description>
      <user_story>En tant qu'administrateur d'entreprise, je veux un audit log de toutes les commandes exécutées pour compliance</user_story>
      <acceptance_criteria>
        <criterion>Table AuditLog stockant toutes les exécutions de commandes</criterion>
        <criterion>Log contient: timestamp, user, action, command, exitCode, success</criterion>
        <criterion>Export audit log en CSV</criterion>
        <criterion>Validation des paramètres pour prévenir injection de commandes</criterion>
        <criterion>Confirmation obligatoire pour commandes Level=Dangerous</criterion>
        <criterion>Délai de 3s avant exécution commande Dangerous (annulable)</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer modèle AuditLog: Id, Timestamp, UserId, ActionId, Command, Platform, ExitCode, Success, Duration</task>
        <task>Migration EF Core pour table AuditLogs</task>
        <task>Implémenter AuditLogService (Add, GetRecent, ExportToCsv)</task>
        <task>Intercepter toutes les exécutions dans CommandExecutionService pour logger</task>
        <task>Créer ParameterValidationService pour valider inputs (anti-injection)</task>
        <task>Implémenter regex/whitelist validation pour paramètres sensibles</task>
        <task>Créer DangerousCommandConfirmationDialog avec countdown 3s</task>
        <task>Créer AuditLogViewModel avec liste et export CSV</task>
        <task>Créer AuditLogWindow.xaml (accessible depuis menu Admin)</task>
        <task>Implémenter ExportToCsvCommand</task>
      </technical_tasks>
      <tests>
        <test>AuditLogService.Add crée entrée avec timestamp UTC</test>
        <test>CommandExecutionService log toutes les exécutions</test>
        <test>ExportToCsv génère fichier CSV valide</test>
        <test>ParameterValidationService détecte injection "; rm -rf /"</test>
        <test>Commande Dangerous affiche confirmation avec countdown 3s</test>
        <test>Annulation pendant countdown empêche exécution</test>
      </tests>
    </item>

    <item priority="4" id="S4-I4">
      <title>Batch execution (exécution multiple)</title>
      <description>Permettre exécution séquentielle de plusieurs commandes</description>
      <user_story>En tant qu'administrateur, je veux exécuter plusieurs commandes d'affilée (ex: backup script) sans intervention manuelle</user_story>
      <acceptance_criteria>
        <criterion>Mode "Batch" pour sélectionner plusieurs actions</criterion>
        <criterion>Ordre d'exécution configurable (drag & drop)</criterion>
        <criterion>Exécution séquentielle avec log de chaque commande</criterion>
        <criterion>Stop-on-error ou Continue-on-error configurable</criterion>
        <criterion>Export/Import de batches en JSON</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer modèle CommandBatch: Id, Name, Commands[], StopOnError, CreatedAt</task>
        <task>Créer modèle BatchCommand: Order, ActionId, Parameters, Enabled</task>
        <task>Implémenter BatchExecutionService.ExecuteBatchAsync(batch, ct)</task>
        <task>Créer BatchBuilderViewModel avec ObservableCollection<BatchCommandViewModel></task>
        <task>Créer BatchBuilderWindow.xaml avec drag & drop reordering</task>
        <task>Implémenter ExecuteBatchCommand avec progress tracking</task>
        <task>Logger chaque commande batch dans AuditLog</task>
        <task>Ajouter export/import JSON de batches</task>
        <task>Sauvegarder batches en DB (table Batches)</task>
      </technical_tasks>
      <tests>
        <test>ExecuteBatchAsync exécute commandes dans ordre correct</test>
        <test>StopOnError=true arrête batch après première erreur</test>
        <test>ContinueOnError=true continue après erreur</test>
        <test>Progress callback mis à jour après chaque commande</test>
        <test>Export/Import batch JSON preserve ordre et paramètres</test>
      </tests>
    </item>

    <item priority="5" id="S4-I5">
      <title>Intégration PowerShell Gallery</title>
      <description>Permettre import de modules PowerShell depuis PowerShell Gallery</description>
      <user_story>En tant qu'utilisateur avancé, je veux importer des cmdlets personnalisés depuis PSGallery pour étendre TwinShell</user_story>
      <acceptance_criteria>
        <criterion>Recherche de modules dans PowerShell Gallery (API)</criterion>
        <criterion>Import de module ajoute cmdlets comme nouvelles actions</criterion>
        <criterion>Parsing automatique de Get-Help pour générer templates</criterion>
        <criterion>Catégorie "Custom Modules" pour actions importées</criterion>
      </acceptance_criteria>
      <technical_tasks>
        <task>Créer PowerShellGalleryService (API HTTP vers powershellgallery.com)</task>
        <task>Implémenter SearchModulesAsync(query)</task>
        <task>Implémenter GetModuleInfoAsync(moduleName)</task>
        <task>Créer PowerShellParserService pour parser Get-Help output</task>
        <task>Générer CommandTemplate à partir de paramètres cmdlet</task>
        <task>Créer ModuleImportViewModel</task>
        <task>Créer ModuleImportWindow.xaml (recherche + liste cmdlets + import)</task>
        <task>Ajouter actions importées dans DB avec Category="Custom Modules"</task>
      </technical_tasks>
      <tests>
        <test>SearchModulesAsync retourne résultats depuis PSGallery API</test>
        <test>PowerShellParserService parse Get-Help correctement</test>
        <test>Import module crée actions avec templates valides</test>
        <test>Actions importées apparaissent dans catégorie "Custom Modules"</test>
      </tests>
    </item>
  </backlog_prioritized>

  <risks_and_mitigations>
    <risk>
      <description>Exécution directe peut exposer à des injections de commandes</description>
      <impact>Critique</impact>
      <mitigation>Validation stricte des paramètres + confirmation pour commandes dangereuses + audit log complet</mitigation>
    </risk>
    <risk>
      <description>Traductions incomplètes ou incorrectes peuvent frustrer utilisateurs</description>
      <impact>Moyen</impact>
      <mitigation>Review par native speakers + fallback FR si traduction manquante</mitigation>
    </risk>
    <risk>
      <description>Timeout trop court peut interrompre commandes légitimes longues</description>
      <impact>Faible</impact>
      <mitigation>Timeout configurable dans Settings (défaut 30s, max 300s)</mitigation>
    </risk>
  </risks_and_mitigations>

  <deliverables>
    <deliverable type="code">
      <item>Services: CommandExecutionService, AuditLogService, LocalizationService, BatchExecutionService, PowerShellGalleryService</item>
      <item>Models: ExecutionResult, AuditLog, CommandBatch, BatchCommand, ActionTranslation</item>
      <item>ViewModels: ExecutionViewModel, AuditLogViewModel, BatchBuilderViewModel, ModuleImportViewModel</item>
      <item>Views: OutputPanel.xaml, AuditLogWindow.xaml, BatchBuilderWindow.xaml, ModuleImportWindow.xaml</item>
      <item>Resource files: Resources.resx, Resources.en.resx, Resources.es.resx</item>
      <item>Migrations EF Core pour AuditLog, Batches, ActionTranslation</item>
    </deliverable>
    <deliverable type="tests">
      <item>Tests unitaires pour tous les services (70%+ couverture)</item>
      <item>Tests d'intégration pour exécution PowerShell/Bash</item>
      <item>Tests sécurité: validation paramètres, injection prevention</item>
      <item>Tests i18n: changement langue, fallback FR</item>
    </deliverable>
    <deliverable type="documentation">
      <item>Guide sécurité: bonnes pratiques exécution commandes</item>
      <item>Documentation batch execution avec exemples</item>
      <item>Guide traduction pour contributeurs (ajouter nouvelle langue)</item>
    </deliverable>
    <deliverable type="ci">
      <item>Build verte sur toutes les plateformes</item>
      <item>Tests passent à 100%</item>
      <item>Aucune régression S1/S2/S3</item>
      <item>Audit sécurité passé (OWASP Top 10 check)</item>
    </deliverable>
  </deliverables>

  <technical_constraints>
    <constraint>Exécution PowerShell: utiliser System.Diagnostics.Process (pas PowerShell SDK pour éviter dépendances lourdes)</constraint>
    <constraint>i18n: utiliser .resx (standard .NET) + Microsoft.Extensions.Localization</constraint>
    <constraint>Validation paramètres: whitelist + regex (pas de blacklist)</constraint>
    <constraint>Audit log: retention minimum 1 an (configurable), export CSV obligatoire</constraint>
  </technical_constraints>

  <acceptance_criteria_global>
    <criterion>Exécution PowerShell/Bash fonctionne sans erreur sur commandes simples</criterion>
    <criterion>Support FR, EN, ES complet (100% strings traduits)</criterion>
    <criterion>Audit log capture toutes les exécutions avec détails complets</criterion>
    <criterion>Validation sécurité empêche injections de commandes basiques</criterion>
    <criterion>Batch execution exécute 10+ commandes séquentiellement sans crash</criterion>
    <criterion>Tous tests passent + aucune régression</criterion>
  </acceptance_criteria_global>

  <handoff>
    <next_item_prompt>
      <title>Prompt d'implémentation - Item S4-I1: Exécution directe PowerShell/Bash</title>
      <content><![CDATA[
Tu es un développeur senior .NET travaillant sur TwinShell (Sprint 4).

**Objectif**: Implémenter l'exécution directe de commandes PowerShell/Bash (Item S4-I1).

**Contexte**:
- Application WPF .NET 8
- Namespace: TwinShell.Infrastructure (Services), TwinShell.App (ViewModels/Views)
- Utiliser System.Diagnostics.Process pour exécution

**Tâches**:
1. Créer `CommandExecutionService` (TwinShell.Infrastructure/Services):
   ```csharp
   public class ExecutionResult
   {
       public bool Success { get; set; }
       public int ExitCode { get; set; }
       public string Stdout { get; set; }
       public string Stderr { get; set; }
       public TimeSpan Duration { get; set; }
   }

   public interface ICommandExecutionService
   {
       Task<ExecutionResult> ExecuteAsync(string command, Platform platform, CancellationToken ct, int timeoutSeconds = 30);
   }
   ```

2. Implémenter `CommandExecutionService.ExecuteAsync`:
   - Créer ProcessStartInfo pour powershell.exe (Windows) ou bash (Linux)
   - Arguments: `-Command` pour PowerShell, `-c` pour bash
   - RedirectStandardOutput = true, RedirectStandardError = true
   - UseShellExecute = false, CreateNoWindow = true
   - Capturer stdout/stderr avec OutputDataReceived/ErrorDataReceived
   - Gérer timeout avec CancellationTokenSource
   - Mesurer Duration avec Stopwatch

3. Créer `ExecutionViewModel` (TwinShell.App/ViewModels):
   - ObservableCollection<OutputLine> OutputLines (pour binding ListView)
   - IsExecuting (bool, pour afficher ProgressRing)
   - ExecuteCommand, StopCommand

4. Créer `OutputPanel.xaml` (UserControl):
   - ListView avec TextBlocks pour afficher stdout/stderr (console-like)
   - Couleur différente pour stderr (rouge) vs stdout (blanc/noir)
   - ScrollViewer auto-scroll vers bas
   - ProgressRing affiché si IsExecuting=true
   - Bouton "Stop" (enabled si IsExecuting)

5. Intégrer dans `CommandGeneratorViewModel`:
   - Ajouter ExecuteCommand à côté de CopyCommand
   - Appeler CommandExecutionService.ExecuteAsync
   - Mettre à jour OutputPanel en temps réel

6. Implémenter confirmation pour commandes dangereuses:
   - Si Action.Level == Dangerous, afficher MessageBox de confirmation
   - Texte: "⚠️ ATTENTION: Cette commande peut modifier le système. Continuer ?"

7. Logger exécutions dans CommandHistory:
   - Ajouter flag IsExecuted (bool) dans modèle CommandHistory
   - Logger après exécution avec ExitCode et Success

**Tests requis**:
- CommandExecutionServiceTests:
  - ExecuteAsync("echo test") retourne Success=true, Stdout="test"
  - ExecuteAsync commande invalide retourne Success=false
  - Timeout après 30s annule process
  - CancellationToken.Cancel() interrompt process
- ExecutionViewModelTests:
  - ExecuteCommand met IsExecuting=true pendant exécution
  - StopCommand appelle CancellationToken.Cancel()

**Critères d'acceptation**:
- [ ] ExecuteAsync fonctionne pour PowerShell (Windows) et Bash (Linux)
- [ ] Stdout/stderr capturés et affichés en temps réel
- [ ] Timeout configurable fonctionne
- [ ] Bouton Stop interrompt commande
- [ ] Commandes dangereuses requièrent confirmation
- [ ] Exécutions loggées dans CommandHistory
- [ ] Tests unitaires passent

**Livrable**: Code + tests, prêt pour commit.
      ]]></content>
    </next_item_prompt>

    <alternative_paths>
      <path id="S4-I2-parallel">
        <title>Implémenter i18n (S4-I2) en parallèle</title>
        <description>Support multi-langues peut être développé indépendamment de l'exécution de commandes</description>
      </path>
      <path id="S4-I3-after-I1">
        <title>Prioriser Audit Log (S4-I3) après exécution directe</title>
        <description>Audit log dépend de l'exécution de commandes pour logger les résultats</description>
      </path>
    </alternative_paths>
  </handoff>
</sprint_session>
```

---

## Résumé de la roadmap

| Sprint | Titre | Durée | Items principaux | Dépendances |
|--------|-------|-------|------------------|-------------|
| S1 | MVP | ✅ Complété | Référentiel actions, Recherche, Filtres, Générateur, Clipboard | Aucune |
| S2 | User Personalization & History | 2-3 semaines | Historique, Favoris, Export/Import, Recherche historique, Widget récents | S1 |
| S3 | UI/UX & Customization | 2-3 semaines | Mode sombre, Catégories custom, Polish UI, Accessibilité, Préférences | S1, S2 |
| S4 | Advanced Features & Integration | 2-3 semaines | Exécution directe, i18n, Audit log, Batch execution, PSGallery | S1, S2, S3 |

**Durée totale estimée**: 6-9 semaines (hors Sprint 1 déjà complété)

---

## Notes d'utilisation

### Comment utiliser ces prompts

1. **Copier le prompt XML complet** du sprint souhaité (S2, S3, ou S4)
2. **Coller dans une session Claude Code** (ou autre LLM)
3. **Le prompt est autonome**: tous les contextes, objectifs, tâches, tests sont inclus
4. **Après implémentation du sprint**: utiliser le **handoff** pour générer le prompt d'implémentation du premier item
5. **Itérer**: après chaque item complété, passer au suivant selon priorité

### Règles importantes

- **Mono-objectif**: Chaque prompt traite UN sprint complet
- **Exécutable tel quel**: Pas besoin de contexte additionnel
- **Respect des dépendances**: S2 avant S3, S3 avant S4
- **CI verte obligatoire**: Tous les tests doivent passer avant de passer au sprint suivant
- **Pas de régression**: Tester fonctionnalités des sprints précédents après chaque livraison

### Handoff

Chaque prompt inclut un **handoff** qui génère:
1. Le prompt d'implémentation détaillé pour l'item #1 (priorité la plus haute)
2. 2-3 chemins alternatifs pour items #2-#3 (parallèle ou séquentiel)

Cela permet de démarrer immédiatement l'implémentation sans ambiguïté.
