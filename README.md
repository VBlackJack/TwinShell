# TwinShell

**Gestionnaire de commandes PowerShell & Bash pour administrateurs système**

TwinShell est une application Windows WPF (.NET 8) qui aide les administrateurs système à trouver rapidement les bonnes commandes PowerShell et Bash pour gérer une infrastructure mixte Windows/Linux.

## 🚀 Fonctionnalités (MVP - Sprint 1)

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

4. Compiler la solution (F6)

5. Lancer l'application (F5)

### Option 2 : Ligne de commande

```bash
# Cloner le repository
git clone https://github.com/VBlackJack/TwinShell.git
cd TwinShell

# Restaurer les packages
dotnet restore

# Compiler
dotnet build

# Lancer l'application
dotnet run --project src/TwinShell.App
```

## 🧪 Tests

Exécuter les tests unitaires :

```bash
# Tous les tests
dotnet test

# Avec couverture
dotnet test --collect:"XPlat Code Coverage"
```

## 📖 Utilisation

1. **Rechercher une action** : Tapez dans la barre de recherche (ex: "gpo", "dns", "service")

2. **Filtrer** : Utilisez les checkboxes Platform/Level pour affiner les résultats

3. **Sélectionner une catégorie** : Cliquez dans le panneau de gauche

4. **Générer une commande** :
   - Sélectionnez une action dans la liste
   - Remplissez les paramètres dans le panneau de droite
   - La commande se génère automatiquement

5. **Copier** : Cliquez sur "Copier dans le presse-papiers"

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

## 🎯 Roadmap (Sprints futurs)

- [ ] Export/Import de configurations
- [ ] Historique des commandes exécutées
- [ ] Favoris utilisateur
- [ ] Catégories personnalisées
- [ ] Mode sombre
- [ ] Support multi-langues
- [ ] Intégration PowerShell/Bash direct

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
