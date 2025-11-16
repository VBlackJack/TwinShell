# TwinShell

**Gestionnaire de commandes PowerShell & Bash pour administrateurs système**

TwinShell est une application Windows WPF (.NET 8) qui aide les administrateurs système à trouver rapidement les bonnes commandes PowerShell et Bash pour gérer une infrastructure mixte Windows/Linux.

## 🚀 Fonctionnalités

### Sprint 1 : MVP (Fonctionnalités de base)

- ✅ **Référentiel de 30+ actions** couvrant :
  - Active Directory (utilisateurs, GPO, diagnostics)
  - DNS (requêtes, cache)
  - Logs (EventLog Windows, journald Linux)
  - Services (systemd, Windows Services)
  - Network & System diagnostics

- 🔍 **Recherche en temps réel** par titre, description, tags
- 🏷️ **Filtres avancés** par Platform (Windows/Linux/Both) et Level (Info/Run/Dangerous)
- 📂 **Navigation par catégories**
- 🛠️ **Générateur de commandes** avec paramètres dynamiques
- 📋 **Copie vers presse-papiers** en un clic
- ⚠️ **Alertes de sécurité** pour les commandes dangereuses

### Sprint 2 : Personnalisation & Historique

- 📜 **Historique des commandes** avec:
  - Tracking automatique de chaque commande générée
  - Recherche et filtrage (par texte, date, catégorie, plateforme)
  - Visualisation avec horodatage et détails d'action
  - Nettoyage automatique (90 jours de rétention par défaut)
  - Copie et suppression d'entrées individuelles

- ⭐ **Système de favoris** avec:
  - Marquer jusqu'à 50 actions comme favorites
  - Bouton étoile (☆/★) avec effet hover doré
  - Catégorie spéciale "⭐ Favorites" pour accès rapide
  - Gestion des limites avec messages explicites

- 💾 **Export/Import de configuration** :
  - Export au format JSON (favorites + historique)
  - Import avec validation et mode fusion
  - Préservation des données existantes
  - Validation de l'intégrité des fichiers

- 🕐 **Widget Commandes Récentes** :
  - Affichage des 5 dernières commandes sur la page d'accueil
  - Temps relatif ("5 min ago", "2h ago")
  - Copie en un clic via click sur l'entrée
  - Message d'état vide élégant

## 🏗️ Architecture

```
TwinShell/
├── src/
│   ├── TwinShell.App/          # Application WPF (UI, ViewModels)
│   ├── TwinShell.Core/          # Logique métier (Models, Services)
│   ├── TwinShell.Persistence/   # EF Core + SQLite
│   └── TwinShell.Infrastructure/# Services transverses (Clipboard, Seed)
├── tests/
│   ├── TwinShell.Core.Tests/
│   └── TwinShell.Persistence.Tests/
└── data/seed/
    └── initial-actions.json     # Données de seed (30 actions)
```

### Stack technique

- **.NET 8.0** - Framework de développement
- **WPF** (Windows Presentation Foundation) - Interface utilisateur
- **SQLite** + **Entity Framework Core** - Persistence
- **MVVM** avec **CommunityToolkit.Mvvm** - Architecture
- **xUnit** + **FluentAssertions** - Tests unitaires

## 📦 Prérequis

- **Windows 10/11**
- **.NET 8 SDK** ou **Visual Studio 2022** (17.8+)
- **PowerShell** (pour exécuter les commandes générées)

## 🛠️ Installation

### Option 1 : Visual Studio

1. Cloner le repository :
   ```bash
   git clone https://github.com/VBlackJack/TwinShell.git
   cd TwinShell
   ```

2. Ouvrir `TwinShell.sln` dans Visual Studio 2022

3. Restaurer les packages NuGet (automatique)

4. **Appliquer les migrations EF Core** (requis pour Sprint 2) :
   ```powershell
   # Dans la Console du Gestionnaire de Package
   Add-Migration AddCommandHistoryAndFavorites -Project TwinShell.Persistence -StartupProject TwinShell.App
   Update-Database -Project TwinShell.Persistence -StartupProject TwinShell.App
   ```

   Ou via dotnet CLI :
   ```bash
   dotnet ef migrations add AddCommandHistoryAndFavorites --project src/TwinShell.Persistence --startup-project src/TwinShell.App
   dotnet ef database update --project src/TwinShell.Persistence --startup-project src/TwinShell.App
   ```

5. Compiler la solution (F6)

6. Lancer l'application (F5)

### Option 2 : Ligne de commande

```bash
# Cloner le repository
git clone https://github.com/VBlackJack/TwinShell.git
cd TwinShell

# Restaurer les packages
dotnet restore

# Appliquer les migrations EF Core (requis pour Sprint 2)
dotnet ef migrations add AddCommandHistoryAndFavorites --project src/TwinShell.Persistence --startup-project src/TwinShell.App
dotnet ef database update --project src/TwinShell.Persistence --startup-project src/TwinShell.App

# Compiler
dotnet build

# Lancer l'application
dotnet run --project src/TwinShell.App
```

> **Note** : Les migrations sont automatiquement appliquées au premier lancement de l'application. L'étape manuelle ci-dessus est optionnelle mais recommandée pour détecter les erreurs de migration avant le lancement.

## 🧪 Tests

Exécuter les tests unitaires :

```bash
# Tous les tests
dotnet test

# Avec couverture
dotnet test --collect:"XPlat Code Coverage"
```

## 📖 Utilisation

### Fonctionnalités de base

1. **Rechercher une action** : Tapez dans la barre de recherche (ex: "gpo", "dns", "service")

2. **Filtrer** : Utilisez les checkboxes Platform/Level pour affiner les résultats

3. **Sélectionner une catégorie** : Cliquez dans le panneau de gauche

4. **Générer une commande** :
   - Sélectionnez une action dans la liste
   - Remplissez les paramètres dans le panneau de droite
   - La commande se génère automatiquement

5. **Copier** : Cliquez sur "Copier dans le presse-papiers"

### Nouvelles fonctionnalités (Sprint 2)

6. **Favoris** :
   - Cliquez sur l'étoile (☆) à côté du titre de l'action pour l'ajouter aux favoris
   - Accédez rapidement à vos favoris via la catégorie "⭐ Favorites"
   - Maximum de 50 favoris par utilisateur

7. **Historique** :
   - Consultez l'onglet "📜 History" pour voir toutes vos commandes générées
   - Recherchez par texte, filtrez par date, catégorie ou plateforme
   - Copiez ou supprimez des entrées individuelles

8. **Commandes récentes** :
   - Widget en haut de la page d'accueil affichant les 5 dernières commandes
   - Cliquez sur une entrée pour copier la commande

9. **Export/Import** :
   - Menu **File → Export Configuration** pour sauvegarder vos favoris et historique
   - Menu **File → Import Configuration** pour restaurer ou fusionner une configuration
   - Format JSON pour faciliter le partage et le versioning

## 🗄️ Base de données

- **Emplacement** : `%LOCALAPPDATA%/TwinShell/twinshell.db`
- **Type** : SQLite
- **Migration** : Automatique au premier lancement
- **Seeding** : 30 actions chargées depuis `data/seed/initial-actions.json`

## 🔒 Sécurité

Les commandes de niveau **Dangerous** (rouge) affichent un bandeau d'alerte :

> ⚠️ ATTENTION : Cette commande peut causer des modifications importantes du système

Exemples : `Clear-EventLog`, `Disable-ADAccount`, `Stop-Process -Force`

## 🧩 Modèle de données

### Action
```csharp
- Id: string
- Title: string
- Description: string
- Category: string (AD, DNS, GPO, Logs, Linux Services...)
- Platform: enum (Windows, Linux, Both)
- Level: enum (Info, Run, Dangerous)
- Tags: List<string>
- WindowsCommandTemplate: CommandTemplate?
- LinuxCommandTemplate: CommandTemplate?
- Examples: List<CommandExample>
- Notes: string?
- Links: List<ExternalLink>
```

### CommandTemplate
```csharp
- Id: string
- Platform: enum
- Name: string
- CommandPattern: string (ex: "Get-ADUser -Identity {username}")
- Parameters: List<TemplateParameter>
```

### CommandHistory (Sprint 2)
```csharp
- Id: string
- UserId: string? (nullable pour mode single-user)
- ActionId: string
- GeneratedCommand: string
- Parameters: Dictionary<string, string>
- Platform: enum
- CreatedAt: DateTime
- Category: string (dénormalisé pour performance)
- ActionTitle: string (dénormalisé pour performance)
```

### UserFavorite (Sprint 2)
```csharp
- Id: string
- UserId: string? (nullable pour mode single-user)
- ActionId: string
- CreatedAt: DateTime
- DisplayOrder: int (pour réorganisation future)
```

## 🎯 Roadmap

### ✅ Complété

**Sprint 1 - MVP** (Janvier 2025)
- Référentiel d'actions avec templates de commandes
- Recherche et filtrage avancés
- Générateur de commandes avec paramètres dynamiques
- Copie vers presse-papiers

**Sprint 2 - Personnalisation & Historique** (Janvier 2025)
- Historique des commandes avec recherche et filtrage
- Système de favoris (max 50)
- Export/Import de configuration JSON
- Widget des commandes récentes

### 🔮 Sprints futurs

**Sprint 3 - Collaboration & Productivité**
- [ ] Catégories personnalisées
- [ ] Partage d'actions entre utilisateurs
- [ ] Templates de commandes personnalisés
- [ ] Notes et annotations sur les actions

**Sprint 4 - Avancé**
- [ ] Mode sombre
- [ ] Support multi-langues (EN/FR)
- [ ] Intégration PowerShell/Bash direct (exécution)
- [ ] Statistiques d'utilisation
- [ ] Synchronisation cloud (optionnelle)

## 🤝 Contribution

Les contributions sont bienvenues ! Merci de :

1. Fork le projet
2. Créer une branche feature (`git checkout -b feature/AmazingFeature`)
3. Commit vos changements (`git commit -m 'Add some AmazingFeature'`)
4. Push vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

## 📝 License

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 👥 Auteurs

- **VBlackJack** - *Développeur principal*

## 🙏 Remerciements

- Documentation Microsoft pour PowerShell et Active Directory
- Communauté Linux pour systemd et bash
- Contributors de CommunityToolkit.Mvvm
