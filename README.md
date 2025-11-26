# 🐚 TwinShell

**Votre gestionnaire de commandes PowerShell et Bash pour l'administration système**

![Version](https://img.shields.io/badge/version-1.2.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Apache--2.0-blue)

---

## 📖 Vue d'ensemble

**TwinShell** est une application Windows moderne qui aide les administrateurs système à trouver, organiser et utiliser rapidement les commandes PowerShell et Bash dont ils ont besoin au quotidien.

### À qui s'adresse TwinShell ?

- **Administrateurs système** gérant des infrastructures Windows et Linux
- **DevOps** travaillant sur des environnements hybrides
- **Techniciens IT** cherchant à centraliser leurs commandes fréquentes
- **Ingénieurs réseau** ayant besoin d'un accès rapide aux diagnostics

### Quel problème TwinShell résout-il ?

En tant qu'administrateur système, vous jonglez constamment entre PowerShell et Bash, vous cherchez dans vos notes dispersées, et vous perdez du temps à retrouver la syntaxe exacte de commandes que vous utilisez rarement mais qui sont critiques.

**TwinShell centralise tout cela** : une bibliothèque de **500+ commandes PowerShell** prêtes à l'emploi, une recherche intelligente, des favoris, un historique, et une interface moderne avec thème sombre.

---

## ✨ Fonctionnalités principales

### 🔍 Recherche Intelligente
- **Recherche en temps réel** par titre, description, tags ou contenu de commande
- **Normalisation automatique** : trouve "réseau" même si vous tapez "reseau"
- **Recherche multi-mots** : "AD user" trouve toutes les commandes liées aux utilisateurs Active Directory
- **Recherche fuzzy** : tolère les fautes de frappe (jusqu'à 30% de différence)
- **Suggestions intelligentes** : l'historique de recherche propose des suggestions d'autocomplétion

### 📂 Organisation Puissante
- **Catégories prédéfinies** : Active Directory, DNS, Logs, Services, Réseau, etc.
- **Catégories personnalisées** : Créez vos propres catégories avec icônes et couleurs
- **Catégorie "📋 All Commands"** : Vue d'ensemble de toutes les commandes disponibles
- **Filtres avancés** : Par plateforme (Windows/Linux), niveau de risque, catégorie

### ⭐ Favoris et Historique
- **Système de favoris** : Marquez jusqu'à 50 commandes pour y accéder instantanément
- **Historique complet** : Toutes vos commandes générées sont sauvegardées avec horodatage
- **Recherche dans l'historique** : Retrouvez une commande que vous avez utilisée il y a 3 semaines
- **Export/Import** : Sauvegardez et partagez vos favoris et historique au format JSON

### 🛠️ Générateur de Commandes
- **Paramètres dynamiques** : Remplissez simplement les champs, la commande se génère automatiquement
- **Exemples intégrés** : Chaque commande inclut des exemples d'utilisation (avec sélection de texte)
- **Copie en un clic** : Copiez la commande générée vers le presse-papiers
- **Affichage des tags** : Identifiez rapidement le type de commande grâce aux tags visuels

### 🌙 Personnalisation
- **Thème sombre professionnel** : Réduisez la fatigue oculaire lors de longues sessions
- **Mode système** : Suit automatiquement le thème Windows (clair/sombre)
- **Contraste WCAG AAA** : Ratio de 7:1 pour une lisibilité optimale
- **Paramètres personnalisables** : Rétention de l'historique, nombre de commandes récentes, etc.

### ⚠️ Sécurité
- **Alertes de sécurité** : Les commandes dangereuses affichent un bandeau d'avertissement rouge
- **Confirmation avant exécution** : Option pour confirmer les commandes critiques
- **Protection des catégories système** : Les catégories prédéfinies ne peuvent pas être supprimées
- **Audit de sécurité complet** : 15 vulnérabilités critiques corrigées (injection, path traversal, etc.)

### ♿ Accessibilité
- **Navigation clavier complète** : Raccourcis pour toutes les fonctionnalités (Ctrl+M, F5, etc.)
- **Conformité WCAG AA** : Contraste, taille des cibles, animations
- **Support lecteurs d'écran** : AutomationProperties configurées
- **Fenêtre responsive** : S'adapte de 800x600 pixels au plein écran

---

## 📦 Installation

### Prérequis

- **Windows 10 ou Windows 11** (64-bit)
- **.NET 8 Runtime** ([Télécharger ici](https://dotnet.microsoft.com/download/dotnet/8.0))
- **PowerShell 5.1+** (inclus dans Windows)
- Optionnel : **Bash** (via WSL) pour les commandes Linux

### Installation Rapide

1. **Télécharger la dernière version**
   - Rendez-vous sur la page [Releases](https://github.com/VBlackJack/TwinShell/releases)
   - Téléchargez `TwinShell-Setup.exe` ou `TwinShell-Portable.zip`

2. **Installer l'application**
   - **Version Setup** : Exécutez `TwinShell-Setup.exe` et suivez l'assistant
   - **Version Portable** : Décompressez `TwinShell-Portable.zip` et lancez `TwinShell.exe`

3. **Premier lancement**
   - L'application crée automatiquement sa base de données SQLite
   - 500+ commandes PowerShell sont chargées automatiquement au démarrage
   - Vous êtes prêt à utiliser TwinShell !

### Installation depuis le Code Source

Pour les développeurs souhaitant compiler le projet :

```bash
# Cloner le repository
git clone https://github.com/VBlackJack/TwinShell.git
cd TwinShell

# Restaurer les packages
dotnet restore

# Compiler
dotnet build --configuration Release

# Lancer l'application
dotnet run --project src/TwinShell.App
```

👉 **Voir le [Guide de Démarrage Rapide](docs/QuickStart.md) pour plus de détails**

---

## 🚀 Guide d'Utilisation

### Premiers Pas

#### 1. Interface Principale

L'interface TwinShell est divisée en 3 panneaux :

```
┌─────────────────────────────────────────────────────────────┐
│  [Recherche]  [Filtres: Platform | Level]          [Menu]   │
├──────────────┬──────────────────────────┬───────────────────┤
│              │                          │                   │
│  Catégories  │   Liste des Commandes    │  Détails + Params │
│              │                          │                   │
│  • All Cmds  │  ✓ Get-Service           │  Title: Get-Svc   │
│  • Favorites │    List Windows svcs     │  [param1] [____]  │
│  • AD        │                          │  [param2] [____]  │
│  • DNS       │  ✓ systemctl status      │                   │
│  • Logs      │    Linux service info    │  Generated Cmd:   │
│  • ...       │                          │  Get-Service...   │
│              │  ✓ Get-EventLog          │                   │
│              │    Windows event logs    │  [📋 Copy]        │
│              │                          │                   │
└──────────────┴──────────────────────────┴───────────────────┘
```

#### 2. Rechercher une Commande

**Méthode 1 : Barre de recherche**
1. Tapez dans la barre de recherche (ex: `"dns"`, `"service"`, `"AD user"`)
2. La liste se filtre en temps réel
3. La recherche ignore les accents, la casse, et tolère les fautes de frappe

**Méthode 2 : Navigation par catégories**
1. Cliquez sur une catégorie dans le panneau de gauche
2. Parcourez la liste des commandes de cette catégorie
3. Cliquez sur "📋 All Commands" pour voir toutes les commandes

**Méthode 3 : Filtres**
- **Platform** : Cochez Windows, Linux ou Both pour filtrer par plateforme
- **Level** : Filtrez par niveau de risque (Info, Run, Dangerous)

#### 3. Générer et Copier une Commande

1. **Sélectionnez une commande** dans la liste
2. **Remplissez les paramètres** dans le panneau de droite (si applicable)
   - Exemple : Pour "Get-Service", entrez le nom du service
   - Les champs obligatoires sont marqués d'un astérisque (*)
3. **La commande se génère automatiquement** en bas du panneau
4. **Cliquez sur "📋 Copier dans le presse-papiers"**
5. **Collez** dans votre terminal PowerShell ou Bash (Ctrl+V)

#### 4. Utiliser les Favoris

**Ajouter un favori :**
- Cliquez sur l'étoile (☆) à côté du titre de la commande
- L'étoile devient pleine (★) et dorée

**Accéder aux favoris :**
- Cliquez sur la catégorie "⭐ Favorites" dans le panneau de gauche
- Vos commandes favorites s'affichent

**Limites :**
- Maximum 50 favoris par utilisateur
- Un message s'affiche si vous atteignez la limite

#### 5. Consulter l'Historique

1. Cliquez sur l'onglet **"📜 History"** dans le menu
2. Vous voyez toutes vos commandes générées avec :
   - Date et heure de génération
   - Commande complète
   - Catégorie et plateforme
3. **Recherchez** dans l'historique avec la barre de recherche
4. **Filtrez** par date, catégorie ou plateforme
5. **Copiez** une commande passée en cliquant dessus
6. **Supprimez** une entrée avec le bouton Supprimer

**Configuration :**
- Par défaut, l'historique conserve 90 jours de commandes
- Modifiable dans les paramètres (1 à 3650 jours)
- Limite d'affichage : 1000 commandes maximum (configurable dans les paramètres)

#### 6. Gérer les Catégories Personnalisées

**Ouvrir le gestionnaire :**
- Menu **Tools → Manage Categories** (ou **Ctrl+M**)

**Créer une catégorie :**
1. Cliquez sur **"+ Add New Category"**
2. Remplissez :
   - **Nom** : Ex. "Backup Quotidien"
   - **Icône** : Choisissez parmi 24 icônes (folder, star, tools, etc.)
   - **Couleur** : Sélectionnez une des 12 couleurs
   - **Description** : (optionnel) Décrivez son usage
3. Cliquez sur **"Save Category"**

**Renommer une catégorie :**
1. Sélectionnez la catégorie dans la liste
2. Cliquez sur **"Rename Category"**
3. Modifiez le nom et cliquez sur **"Save"**

**Supprimer une catégorie :**
1. Sélectionnez la catégorie
2. Cliquez sur **"Delete Category"**
3. Confirmez la suppression
4. ⚠️ Les catégories système (badge jaune) ne peuvent pas être supprimées

**Réorganiser les catégories :**
- Utilisez les boutons **"Move Up"** et **"Move Down"**
- L'ordre est sauvegardé automatiquement

**Masquer/Afficher une catégorie :**
- Bouton **"Hide"** pour masquer une catégorie de la navigation
- Bouton **"Show"** pour la réafficher

#### 7. Changer le Thème

**Ouvrir les paramètres :**
- Menu **File → Settings** (ou **Ctrl+,**)

**Choisir un thème :**
1. Section **Appearance**
2. Sélectionnez :
   - **Light** : Thème clair (défaut)
   - **Dark** : Thème sombre pour réduire la fatigue oculaire
   - **System** : Suit automatiquement le thème Windows
3. Cliquez sur **"Preview Theme"** pour voir le résultat
4. Cliquez sur **"Save"** pour enregistrer

**Caractéristiques :**
- Transition fluide sans clignotement
- Contraste WCAG AAA (ratio 7:1)
- Tous les écrans sont supportés

#### 8. Exporter/Importer la Configuration

**Exporter :**
1. Menu **File → Export Configuration** (ou **Ctrl+E**)
2. Choisissez l'emplacement et le nom du fichier JSON
3. Le fichier contient :
   - Vos favoris
   - Votre historique de commandes
   - Vos paramètres

**Importer :**
1. Menu **File → Import Configuration** (ou **Ctrl+I**)
2. Sélectionnez le fichier JSON à importer
3. Mode **fusion** : Les données existantes sont préservées
4. Validation automatique de l'intégrité du fichier

---

## ⌨️ Raccourcis Clavier

| Raccourci | Action |
|-----------|--------|
| **Ctrl+,** | Ouvrir les Paramètres |
| **Ctrl+M** | Gérer les Catégories |
| **Ctrl+E** | Exporter la Configuration |
| **Ctrl+I** | Importer la Configuration |
| **Ctrl+F** | Focus sur la barre de recherche |
| **Ctrl+C** | Copier la commande générée |
| **F1** | Afficher l'Aide |
| **Enter** | Exécuter l'action sélectionnée |
| **Tab** | Naviguer entre les contrôles |
| **Esc** | Annuler/Effacer la recherche |
| **Alt+F4** | Quitter l'application |

👉 **Voir le [Guide Utilisateur Complet](docs/UserGuide.md) pour plus de détails**

---

## ❓ FAQ / Questions Fréquentes

### Général

**Q : TwinShell fonctionne-t-il sur Mac ou Linux ?**
R : Non, TwinShell est une application Windows WPF. Elle nécessite Windows 10 ou 11.

**Q : Puis-je exécuter les commandes directement depuis TwinShell ?**
R : Non, TwinShell est un générateur de commandes. Vous copiez la commande et la collez dans votre terminal PowerShell ou Bash.

**Q : Les commandes Bash fonctionnent-elles sur Windows ?**
R : Oui, si vous avez installé WSL (Windows Subsystem for Linux). Les commandes Bash doivent être exécutées dans un terminal WSL.

**Q : Où sont stockées mes données ?**
R :
- Base de données : `%LOCALAPPDATA%\TwinShell\twinshell.db` (SQLite)
- Paramètres : `%APPDATA%\TwinShell\settings.json`

### Recherche

**Q : Pourquoi ma recherche "Get-Service" trouve-t-elle aussi "Get Service" ?**
R : TwinShell normalise les tirets, underscores et points en espaces pour une recherche plus permissive. Cela permet de trouver des commandes même si vous ne tapez pas la syntaxe exacte.

**Q : La recherche est-elle sensible aux accents ?**
R : Non, les accents sont automatiquement ignorés. "réseau" et "reseau" donnent les mêmes résultats.

**Q : Comment chercher plusieurs mots ?**
R : Tapez simplement les mots séparés par des espaces (ex: "AD user"). TOUS les mots doivent être présents dans la commande (logique AND).

### Fonctionnalités

**Q : Puis-je ajouter mes propres commandes ?**
R : Cette fonctionnalité n'est pas encore disponible dans l'interface. Vous pouvez modifier le fichier `data/seed/initial-actions.json` et relancer l'application.

**Q : Combien de favoris puis-je avoir ?**
R : Maximum 50 favoris par utilisateur.

**Q : Combien de temps l'historique est-il conservé ?**
R : Par défaut 90 jours, modifiable dans les paramètres (de 1 à 3650 jours).

**Q : Puis-je partager mes favoris avec un collègue ?**
R : Oui, utilisez la fonction Export/Import pour partager votre configuration au format JSON.

### Dépannage

**Q : Le thème ne change pas après sauvegarde**
R : Vérifiez que vous avez bien cliqué sur "Save". Si le problème persiste, redémarrez l'application.

**Q : Je ne peux pas modifier une catégorie**
R : Vérifiez qu'il ne s'agit pas d'une catégorie système (badge jaune). Les catégories système sont protégées contre la modification et la suppression.

**Q : L'application ne démarre pas**
R :
1. Vérifiez que .NET 8 Runtime est installé
2. Vérifiez les permissions d'accès au dossier `%LOCALAPPDATA%\TwinShell`
3. Consultez les logs dans `%LOCALAPPDATA%\TwinShell\logs`

**Q : Les raccourcis clavier ne fonctionnent pas**
R :
1. Assurez-vous que la fenêtre TwinShell a le focus
2. Vérifiez qu'aucune autre application n'intercepte le même raccourci
3. Appuyez sur F1 pour voir la liste complète des raccourcis actifs

👉 **Voir la [FAQ Complète](docs/FAQ.md) pour plus de questions**

---

## 🤝 Support et Contribution

### Obtenir de l'Aide

- 📖 **Documentation** : Consultez le [Guide Utilisateur](docs/UserGuide.md)
- 💬 **Discussions** : [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)
- 🐛 **Signaler un Bug** : [Issues GitHub](https://github.com/VBlackJack/TwinShell/issues)

### Contribuer au Projet

Les contributions sont les bienvenues ! Pour contribuer :

1. **Forkez** le projet
2. Créez une **branche** pour votre fonctionnalité (`git checkout -b feature/AmazingFeature`)
3. **Commitez** vos changements (`git commit -m 'Add some AmazingFeature'`)
4. **Pushez** vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrez une **Pull Request**

Pour toute question sur le développement, consultez les [Discussions GitHub](https://github.com/VBlackJack/TwinShell/discussions).

---

## 📝 Licence

Ce projet est sous licence **Apache 2.0**. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

Vous êtes libre de :
- ✅ Utiliser le logiciel à des fins personnelles et commerciales
- ✅ Modifier le code source
- ✅ Distribuer le logiciel
- ✅ Utiliser le logiciel dans des projets privés

Conditions :
- ℹ️ Vous devez inclure la licence Apache 2.0 dans toute copie du logiciel
- ℹ️ Le logiciel est fourni "tel quel", sans garantie

---

## 👥 Auteurs et Remerciements

### Auteur Principal

- **VBlackJack** - *Développeur principal* - [GitHub](https://github.com/VBlackJack)

### Remerciements

- **Microsoft** - Documentation PowerShell et Active Directory
- **Communauté Linux** - Documentation systemd et bash
- **CommunityToolkit.Mvvm** - Framework MVVM moderne pour .NET
- **Tous les contributeurs** qui améliorent TwinShell

---

## 🔗 Liens Utiles

- 🏠 **Site Web** : [À venir]
- 📦 **Releases** : [GitHub Releases](https://github.com/VBlackJack/TwinShell/releases)
- 📖 **Documentation Complète** : [docs/](docs/)
- 🔧 **Documentation Développeur** : [docs/developer/](docs/developer/)
- 🐛 **Signaler un Bug** : [Issues](https://github.com/VBlackJack/TwinShell/issues)
- 💬 **Discussions** : [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)

---

<div align="center">

**TwinShell** - Votre Compagnon d'Administration Système

[![Star on GitHub](https://img.shields.io/github/stars/VBlackJack/TwinShell?style=social)](https://github.com/VBlackJack/TwinShell)

*Développé avec ❤️ pour la communauté des Administrateurs Système*

</div>
