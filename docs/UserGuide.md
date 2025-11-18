# 📘 Guide Utilisateur Complet - TwinShell

**Documentation complète pour maîtriser toutes les fonctionnalités de TwinShell**

---

## Table des Matières

1. [Interface Principale](#interface-principale)
2. [Recherche et Filtrage](#recherche-et-filtrage)
3. [Générer et Copier des Commandes](#générer-et-copier-des-commandes)
4. [Système de Favoris](#système-de-favoris)
5. [Historique des Commandes](#historique-des-commandes)
6. [Catégories Personnalisées](#catégories-personnalisées)
7. [Thèmes et Personnalisation](#thèmes-et-personnalisation)
8. [Paramètres Avancés](#paramètres-avancés)
9. [Export et Import de Configuration](#export-et-import-de-configuration)
10. [Raccourcis Clavier](#raccourcis-clavier)
11. [Conseils et Astuces](#conseils-et-astuces)
12. [Résolution de Problèmes](#résolution-de-problèmes)

---

## Interface Principale

### Vue d'Ensemble

L'interface TwinShell est organisée en **3 panneaux principaux** :

```
┌─────────────────────────────────────────────────────────────────┐
│  TwinShell                                              [_][□][X]│
├─────────────────────────────────────────────────────────────────┤
│  📁 File  ⚙️ Tools  ❓ Help        [Recherche___] 🔍  Filtres   │
├──────────────┬──────────────────────────────┬───────────────────┤
│  Catégories  │   Liste des Commandes       │  Détails & Params │
│              │                              │                   │
│  📋 All      │  ✓ Get-Service              │  Get-Service      │
│  ⭐ Favs     │    List Windows services    │  ───────────────  │
│              │    [Windows] [Run]          │  Service Name:    │
│  🔵 AD       │                              │  [Spooler____]    │
│  🟢 DNS      │  ✓ systemctl status         │                   │
│  🟡 GPO      │    Linux service info       │  Generated:       │
│  🔴 Logs     │    [Linux] [Info]           │  ┌──────────────┐ │
│  🟣 Svcs     │                              │  │Get-Service...│ │
│  🟠 Net      │  ✓ Get-EventLog             │  └──────────────┘ │
│  ...         │    Windows event logs       │                   │
│              │    [Windows] [Dangerous]    │  [📋 Copier]      │
└──────────────┴──────────────────────────────┴───────────────────┘
```

### Panneau 1 : Catégories (Gauche)

**Catégories Spéciales :**
- **📋 All Commands** : Affiche toutes les commandes disponibles (sans filtre)
- **⭐ Favorites** : Accès rapide à vos commandes favorites (max 50)

**Catégories Prédéfinies :**
- 🔵 **Active Directory** : Gestion des utilisateurs, groupes, GPO
- 🟢 **DNS** : Requêtes DNS, cache, diagnostics
- 🟡 **GPO** : Group Policy Objects (Windows)
- 🔴 **Logs** : EventLog (Windows) et journald (Linux)
- 🟣 **Services** : systemd (Linux) et Windows Services
- 🟠 **Network** : Diagnostics réseau (ping, traceroute, etc.)
- 🟤 **System** : Informations système

**Catégories Personnalisées :**
- Créez vos propres catégories avec icônes et couleurs personnalisées
- Gérez-les via `Ctrl+M` ou menu **Tools → Manage Categories**

### Panneau 2 : Liste des Commandes (Centre)

Affiche les commandes filtrées avec :
- **Titre** de la commande
- **Description** courte
- **Badges** :
  - Plateforme : [Windows] | [Linux] | [Both]
  - Niveau : [Info] (bleu) | [Run] (orange) | [Dangerous] (rouge)
- **Tags visuels** : Identifiez rapidement le type de commande

### Panneau 3 : Détails et Paramètres (Droite)

Affiche pour la commande sélectionnée :
- **Titre et Description** complète
- **Étoile de favori** (☆/★)
- **Paramètres dynamiques** à remplir
- **Commande générée** en temps réel
- **Bouton Copier** pour le presse-papiers
- **Exemples d'utilisation** (avec sélection de texte)
- **Notes** et **Liens externes** (si disponibles)

---

## Recherche et Filtrage

### Recherche Textuelle

#### Fonctionnement

La barre de recherche (en haut) filtre les commandes en **temps réel** dans :
- Titre
- Description
- Catégorie
- Tags
- Notes
- Templates de commandes (PowerShell et Bash)

#### Recherche Intelligente

TwinShell utilise une **normalisation avancée** pour une recherche tolérante :

| Fonctionnalité | Exemple | Résultat |
|----------------|---------|----------|
| **Insensible à la casse** | `SERVICE` | Trouve "Get-Service" |
| **Ignore les accents** | `reseau` | Trouve "Configuration Réseau" |
| **Normalise la ponctuation** | `Get Service` | Trouve "Get-Service" |
| **Fuzzy matching** | `serviec` | Trouve "service" (tolérance 30%) |
| **Multi-mots (AND)** | `AD user` | Trouve actions contenant "AD" ET "user" |

#### Suggestions d'Autocomplétion

Lors de la saisie, TwinShell propose des **suggestions basées sur l'historique** de vos recherches précédentes.

#### Exemples de Recherche

```
Recherche : "dns"
Résultats : "Clear DNS Cache", "DNS Query", "Get DNS Records"

Recherche : "active directory user"
Résultats : "List AD Users", "Create AD User", "Get AD User Info"

Recherche : "systemctl"
Résultats : Toutes les commandes utilisant systemctl dans leurs templates
```

### Filtres Avancés

#### Filtre Platform

Cochez les plateformes souhaitées :
- ☑ **Windows** : Commandes PowerShell uniquement
- ☑ **Linux** : Commandes Bash uniquement
- ☑ **Both** : Commandes disponibles sur les deux plateformes

#### Filtre Level

Filtrez par niveau de criticité :
- ☑ **Info** (bleu) : Commandes de lecture, sans danger
- ☑ **Run** (orange) : Commandes d'exécution, nécessitent attention
- ☑ **Dangerous** (rouge) : Commandes de modification système, DANGER !

> ⚠️ **Note** : Quand une recherche textuelle est active, le filtre de catégorie est automatiquement désactivé pour montrer tous les résultats pertinents.

### Métriques de Recherche

En bas de la liste, TwinShell affiche :
- 📊 **Nombre de résultats** : Ex. "142 résultats trouvés"
- ⏱️ **Temps de recherche** : Ex. "en 23ms"

---

## Générer et Copier des Commandes

### Étape par Étape

#### 1. Sélectionner une Commande

Cliquez sur une commande dans la liste centrale. Le panneau de droite affiche les détails.

#### 2. Remplir les Paramètres

Selon la commande, remplissez les champs demandés :

**Exemple : Get-Service**
- **ServiceName** (requis) : `Spooler`

**Exemple : Get-ADUser**
- **Identity** (requis) : `jdoe`
- **Properties** (optionnel) : `DisplayName,EmailAddress`

> 💡 Les champs **obligatoires** sont marqués d'un astérisque `*`

#### 3. Commande Générée Automatiquement

La commande se génère **en temps réel** au fur et à mesure que vous remplissez les paramètres.

**Avant :**
```
Get-Service -Name {ServiceName}
```

**Après (avec paramètre "Spooler") :**
```
Get-Service -Name Spooler
```

#### 4. Copier dans le Presse-Papiers

Cliquez sur le bouton **"📋 Copier dans le presse-papiers"**.

Une notification toast verte confirme : ✅ "Commande copiée !"

#### 5. Exécuter dans votre Terminal

**PowerShell :**
```powershell
# Collez (Ctrl+V) dans PowerShell
Get-Service -Name Spooler
```

**Bash (WSL) :**
```bash
# Collez (Ctrl+Shift+V) dans un terminal WSL
systemctl status nginx
```

### Exemples Intégrés

Chaque commande inclut des **exemples d'utilisation** :

```
Exemple 1 : Lister tous les services
Get-Service

Exemple 2 : Obtenir un service spécifique
Get-Service -Name Spooler

Exemple 3 : Filtrer les services en cours d'exécution
Get-Service | Where-Object {$_.Status -eq 'Running'}
```

> 💡 **Astuce** : Vous pouvez **sélectionner le texte** dans les exemples pour le copier manuellement.

### Alertes de Sécurité

Les commandes de niveau **Dangerous** affichent un **bandeau d'avertissement rouge** :

```
⚠️ ATTENTION : Cette commande peut causer des modifications importantes du système
              Vérifiez tous les paramètres avant exécution.
```

**Exemples de commandes dangereuses :**
- `Clear-EventLog` : Efface les logs Windows
- `Disable-ADAccount` : Désactive un compte Active Directory
- `Stop-Process -Force` : Force l'arrêt d'un processus

---

## Système de Favoris

### Ajouter un Favori

1. Sélectionnez une commande dans la liste
2. Cliquez sur l'**étoile vide (☆)** à côté du titre
3. L'étoile devient **pleine et dorée (★)**
4. Un message confirme : ✅ "Ajouté aux favoris"

### Accéder aux Favoris

- Cliquez sur la catégorie **"⭐ Favorites"** dans le panneau de gauche
- Toutes vos commandes favorites s'affichent

### Retirer un Favori

- Cliquez sur l'**étoile pleine (★)** pour la vider
- La commande est retirée des favoris

### Limites et Messages

- **Maximum** : 50 favoris par utilisateur
- Si vous atteignez la limite, un message s'affiche :
  ```
  ⚠️ Limite de favoris atteinte (50/50)
  Retirez un favori existant pour en ajouter un nouveau.
  ```

### Persistance

Les favoris sont sauvegardés dans la base de données SQLite et **persistent entre les sessions**.

---

## Historique des Commandes

### Vue d'Ensemble

TwinShell enregistre automatiquement **chaque commande générée** avec :
- **Commande complète** générée
- **Date et heure** de génération
- **Catégorie** et **Titre** de l'action
- **Plateforme** (Windows/Linux)
- **Paramètres** utilisés

### Accéder à l'Historique

**Menu** : Cliquez sur l'onglet **"📜 History"**

### Interface de l'Historique

```
┌────────────────────────────────────────────────────────────────┐
│  📜 Historique des Commandes                                   │
├────────────────────────────────────────────────────────────────┤
│  [Recherche___] 🔍    Filtres: [Date] [Catégorie] [Platform]  │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ⏰ Il y a 5 min                                  [Windows]    │
│  Get-Service -Name Spooler                                     │
│  Action: Get-Service | Catégorie: Services                     │
│  [📋 Copier]  [🗑️ Supprimer]                                   │
│  ──────────────────────────────────────────────────────────────│
│                                                                │
│  ⏰ Il y a 2h                                     [Linux]      │
│  systemctl status nginx                                        │
│  Action: System Service Check | Catégorie: Services           │
│  [📋 Copier]  [🗑️ Supprimer]                                   │
│  ──────────────────────────────────────────────────────────────│
│                                                                │
│  ⏰ Hier à 14:30                                  [Windows]    │
│  Get-ADUser -Identity jdoe -Properties DisplayName             │
│  Action: Get AD User | Catégorie: Active Directory            │
│  [📋 Copier]  [🗑️ Supprimer]                                   │
│  ──────────────────────────────────────────────────────────────│
│                                                                │
│  ... (affiche jusqu'à 1000 entrées par défaut)                │
└────────────────────────────────────────────────────────────────┘
```

### Rechercher dans l'Historique

Tapez dans la barre de recherche pour filtrer par :
- Texte de la commande
- Titre de l'action
- Catégorie

### Filtrer l'Historique

**Par Date :**
- Sélectionnez une plage de dates (ex: "Derniers 7 jours", "Derniers 30 jours", "Personnalisé")

**Par Catégorie :**
- Sélectionnez une catégorie spécifique (ex: "Active Directory", "DNS")

**Par Plateforme :**
- Windows uniquement
- Linux uniquement
- Les deux

### Copier une Commande Passée

- Cliquez sur le bouton **"📋 Copier"** à côté de la commande
- La commande est copiée dans le presse-papiers

### Supprimer une Entrée

- Cliquez sur le bouton **"🗑️ Supprimer"**
- Confirmez la suppression
- L'entrée est supprimée de l'historique

### Nettoyage Automatique

Par défaut, l'historique conserve **90 jours** de commandes.

Les commandes plus anciennes sont **automatiquement supprimées**.

**Configurable dans les paramètres** (de 1 à 3650 jours).

### Widget des Commandes Récentes

Sur la **page d'accueil**, un widget affiche vos **5 dernières commandes** :

```
📋 Commandes Récentes

⏰ 5 min ago    Get-Service -Name Spooler
⏰ 2h ago       systemctl status nginx
⏰ Hier 14:30   Get-ADUser -Identity jdoe
⏰ 3 jours      Clear-DnsClientCache
⏰ 1 semaine    Get-EventLog -LogName System -Newest 100
```

**Cliquez** sur une entrée pour la copier instantanément !

---

## Catégories Personnalisées

### Ouvrir le Gestionnaire de Catégories

**Méthode 1 :** Menu **Tools → Manage Categories**
**Méthode 2 :** Raccourci clavier **Ctrl+M**

### Interface de Gestion

```
┌────────────────────────────────────────────────────────────────┐
│  Gérer les Catégories                                          │
├──────────────────────┬─────────────────────────────────────────┤
│  Catégories          │  Détails de la Catégorie                │
│  [+ Add New]         │                                         │
│                      │  🔵 Active Directory                     │
│  🔵 AD (5 actions)   │  Icon: user                             │
│  🟢 DNS (3 actions)  │  Color: #2196F3                         │
│  🔴 Logs (7 actions) │  Actions: 5                             │
│  🟠 Backup           │  Status: Visible                        │
│  🟣 Monitoring       │  Type: Custom                           │
│                      │                                         │
│  [↑ Move Up]         │  [Rename]  [Hide]  [Delete]            │
│  [↓ Move Down]       │                                         │
└──────────────────────┴─────────────────────────────────────────┘
```

### Créer une Catégorie

1. Cliquez sur **"+ Add New Category"**
2. Remplissez le formulaire :
   - **Nom** : Ex. "Backup Quotidien"
   - **Icône** : Choisissez parmi 24 icônes :
     - 📁 folder, ⭐ star, 🔧 tools, 🗄️ database, 📊 chart, 🔒 lock, etc.
   - **Couleur** : Sélectionnez une des 12 couleurs :
     - Bleu, Vert, Rouge, Orange, Violet, Rose, Jaune, Cyan, etc.
   - **Description** (optionnel) : "Commandes pour les backups quotidiens automatisés"
3. Cliquez sur **"Save Category"**

Une notification confirme : ✅ "Catégorie créée avec succès"

### Renommer une Catégorie

1. Sélectionnez la catégorie dans la liste
2. Cliquez sur **"Rename Category"**
3. Modifiez le nom
4. Cliquez sur **"Save"**

> ⚠️ **Note** : Les catégories système (avec badge jaune "System") ne peuvent pas être renommées.

### Supprimer une Catégorie

1. Sélectionnez la catégorie
2. Cliquez sur **"Delete Category"**
3. Un message de confirmation affiche le **nombre d'actions affectées**
4. Confirmez la suppression

**Comportement :**
- La catégorie est retirée de toutes les actions qui l'utilisent
- Les actions ne sont PAS supprimées (juste la référence à la catégorie)

> ⚠️ **Protection** : Les catégories système ne peuvent pas être supprimées.

### Réorganiser les Catégories

**Boutons "Move Up" et "Move Down" :**
- Changez l'ordre d'affichage dans le panneau de gauche
- L'ordre est sauvegardé automatiquement

### Masquer/Afficher une Catégorie

**Bouton "Hide" :**
- Masque la catégorie de la navigation (panneau gauche)
- La catégorie existe toujours dans la base de données

**Bouton "Show" :**
- Réaffiche la catégorie masquée

### Limites

- **Maximum** : 50 catégories personnalisées
- Message si limite atteinte : "Limite de catégories atteinte (50/50)"

---

## Thèmes et Personnalisation

### Accéder aux Paramètres de Thème

**Méthode 1 :** Menu **File → Settings**
**Méthode 2 :** Raccourci clavier **Ctrl+,**

### Choisir un Thème

Section **Appearance** :

1. **Light** : Thème clair (par défaut)
   - Fond blanc/gris clair
   - Texte noir
   - Idéal pour les environnements lumineux

2. **Dark** : Thème sombre professionnel
   - Fond gris foncé/noir
   - Texte blanc
   - Réduit la fatigue oculaire

3. **System** : Suit automatiquement le thème Windows
   - Lit le registre Windows : `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`
   - Change automatiquement si Windows passe de clair à sombre (et vice versa)

### Prévisualiser un Thème

Cliquez sur **"Preview Theme"** pour voir le résultat **sans sauvegarder**.

**Caractéristiques :**
- **Transition fluide** : Changement sans clignotement
- **Application immédiate** : Tous les écrans sont mis à jour
- **Réversible** : Cliquez "Cancel" pour revenir au thème précédent

### Sauvegarder le Thème

Cliquez sur **"Save"** pour enregistrer votre choix.

Le thème est sauvegardé dans `%APPDATA%\TwinShell\settings.json` et **persiste entre les sessions**.

### Conformité WCAG AAA

Les thèmes TwinShell respectent les normes **WCAG AAA** d'accessibilité :

| Élément | Contraste | Norme WCAG AAA |
|---------|-----------|----------------|
| **Texte principal** | 11.6:1 | ✅ Dépasse 7:1 requis |
| **Texte secondaire** | 9.2:1 | ✅ Dépasse 7:1 requis |
| **Éléments UI** | 7.2:1 | ✅ Dépasse 3:1 requis |

**Avantages :**
- Lisibilité optimale
- Confort visuel
- Accessibilité pour les personnes malvoyantes

---

## Paramètres Avancés

### Accéder aux Paramètres

**Ctrl+,** ou Menu **File → Settings**

### Section Appearance (Apparence)

- **Theme** : Light | Dark | System
- **Preview Theme** : Prévisualiser avant de sauvegarder

### Section Behavior (Comportement)

#### Historique

**Command History Retention** (Rétention de l'historique)
- **Plage** : 1 à 3650 jours
- **Par défaut** : 90 jours
- **Comportement** : Les commandes plus anciennes sont automatiquement supprimées

**Maximum History Items** (Nombre max d'items affichés)
- **Plage** : 10 à 100 000
- **Par défaut** : 1000
- **Comportement** : Limite le nombre d'entrées affichées dans la vue historique

#### Widget Commandes Récentes

**Recent Commands Count** (Nombre de commandes récentes)
- **Plage** : 1 à 50
- **Par défaut** : 5
- **Comportement** : Nombre de commandes affichées dans le widget de la page d'accueil

**Show Recent Commands Widget** (Afficher le widget)
- ☑ Coché : Widget visible au démarrage
- ☐ Décoché : Widget masqué

#### Sécurité

**Confirm before executing dangerous commands** (Confirmer avant les commandes dangereuses)
- ☑ **Recommandé** : Affiche une confirmation avant de copier des commandes de niveau "Dangerous"
- ☐ Désactivé : Copie directement sans confirmation

### Réinitialiser les Paramètres

Bouton **"Reset to Defaults"** :
- Restaure tous les paramètres aux valeurs par défaut
- Demande confirmation avant de réinitialiser
- Nécessite un redémarrage de l'application

### Emplacement du Fichier de Configuration

Les paramètres sont sauvegardés dans :
```
%APPDATA%\TwinShell\settings.json
```

**Exemple de contenu :**
```json
{
  "Theme": "Dark",
  "AutoCleanupDays": 90,
  "MaxHistoryItems": 1000,
  "RecentCommandsCount": 5,
  "ShowRecentCommandsWidget": true,
  "ConfirmDangerousActions": true,
  "DefaultPlatformFilter": null
}
```

---

## Export et Import de Configuration

### Exporter la Configuration

#### Ouvrir l'Export

**Méthode 1 :** Menu **File → Export Configuration**
**Méthode 2 :** Raccourci clavier **Ctrl+E**

#### Processus d'Export

1. Une boîte de dialogue "Enregistrer sous" s'ouvre
2. Choisissez l'emplacement et le nom du fichier
3. Format : **JSON** (ex: `twinshell-config-2025-01-18.json`)
4. Cliquez sur **"Enregistrer"**
5. Notification : ✅ "Configuration exportée avec succès"

#### Contenu Exporté

Le fichier JSON contient :
- **Favoris** : Liste des actions favorites avec leurs IDs
- **Historique** : Toutes les commandes générées avec horodatage
- **Paramètres** : Vos préférences (thème, rétention, etc.)

**Exemple de structure :**
```json
{
  "favorites": [
    {
      "actionId": "get-service-windows",
      "createdAt": "2025-01-15T10:30:00Z"
    },
    ...
  ],
  "history": [
    {
      "actionId": "get-service-windows",
      "generatedCommand": "Get-Service -Name Spooler",
      "parameters": { "ServiceName": "Spooler" },
      "platform": "Windows",
      "createdAt": "2025-01-18T08:45:00Z",
      "category": "Services",
      "actionTitle": "Get-Service"
    },
    ...
  ],
  "settings": {
    "theme": "Dark",
    "autoCleanupDays": 90,
    ...
  }
}
```

### Importer une Configuration

#### Ouvrir l'Import

**Méthode 1 :** Menu **File → Import Configuration**
**Méthode 2 :** Raccourci clavier **Ctrl+I**

#### Processus d'Import

1. Une boîte de dialogue "Ouvrir" s'affiche
2. Sélectionnez le fichier JSON à importer
3. Cliquez sur **"Ouvrir"**
4. TwinShell valide l'intégrité du fichier
5. **Mode fusion** : Les données existantes sont préservées
6. Notification : ✅ "Configuration importée avec succès"

#### Comportement de Fusion

- **Favoris** : Les favoris importés sont ajoutés (pas de doublon)
- **Historique** : Les commandes importées sont fusionnées avec l'existant
- **Paramètres** : Les paramètres importés **remplacent** les existants

#### Validation

TwinShell vérifie :
- Format JSON valide
- Structure correcte (clés `favorites`, `history`, `settings`)
- Intégrité des données (dates valides, IDs cohérents)

Si le fichier est invalide, un message d'erreur s'affiche :
```
❌ Erreur : Fichier de configuration invalide
Le fichier JSON est corrompu ou incompatible.
```

### Cas d'Usage

#### Sauvegarde Régulière

Exportez votre configuration chaque semaine pour sauvegarder :
- Vos favoris
- Votre historique
- Vos préférences

#### Partage avec un Collègue

Exportez vos favoris et partagez le fichier JSON :
- Votre collègue importe le fichier
- Il récupère vos commandes favorites
- Idéal pour standardiser les commandes dans une équipe

#### Migration vers un Nouveau PC

1. Exportez votre configuration sur l'ancien PC
2. Installez TwinShell sur le nouveau PC
3. Importez le fichier JSON
4. Vous retrouvez toutes vos données !

---

## Raccourcis Clavier

### Raccourcis Globaux

| Raccourci | Action | Description |
|-----------|--------|-------------|
| **Ctrl+,** | Paramètres | Ouvre la fenêtre des paramètres |
| **Ctrl+M** | Gérer les Catégories | Ouvre le gestionnaire de catégories |
| **Ctrl+E** | Exporter | Exporte la configuration au format JSON |
| **Ctrl+I** | Importer | Importe une configuration depuis un fichier JSON |
| **F1** | Aide | Affiche l'aide et les raccourcis clavier |
| **F5** | Actualiser | Recharge la liste des actions depuis la base de données |
| **Esc** | Annuler/Fermer | Ferme la fenêtre ou annule l'action en cours |
| **Alt+F4** | Quitter | Ferme l'application |

### Navigation au Clavier

| Raccourci | Action |
|-----------|--------|
| **Tab** | Passe au contrôle suivant |
| **Shift+Tab** | Passe au contrôle précédent |
| **Espace** | Coche/décoche une case |
| **Entrée** | Active le bouton/élément sélectionné |
| **Flèches ↑↓** | Navigue dans les listes |

### Aide Intégrée

Appuyez sur **F1** pour afficher une fenêtre d'aide avec tous les raccourcis disponibles.

---

## Conseils et Astuces

### Optimiser votre Workflow

#### 1. Créez des Catégories Thématiques

Organisez vos commandes par thème :
- **"Tâches Quotidiennes"** : Commandes que vous utilisez tous les jours
- **"Maintenance"** : Commandes de maintenance hebdomadaire
- **"Urgences"** : Commandes critiques pour résoudre des problèmes rapidement
- **"Formation"** : Commandes pour former de nouveaux membres de l'équipe

Assignez des **couleurs distinctes** pour une reconnaissance rapide.

#### 2. Utilisez les Raccourcis Clavier

Gagnez du temps en mémorisant ces raccourcis :
- `Ctrl+M` pour gérer les catégories
- `F5` pour actualiser
- `Ctrl+E` pour exporter régulièrement

#### 3. Configurez le Mode Sombre

Réduisez la fatigue oculaire lors de sessions longues :
- Utilisez le **mode System** pour un changement automatique jour/nuit
- Activez le **mode Dark** pour les sessions en soirée

#### 4. Ajustez les Préférences d'Historique

- **Réduisez la rétention** (30 jours) si l'espace disque est limité
- **Augmentez la rétention** (365 jours) pour conserver un historique étendu

#### 5. Activez les Confirmations pour Actions Dangereuses

Évitez les erreurs accidentelles :
- Paramètres → **"Confirm before executing dangerous commands"** ☑
- Recommandé pour les environnements de production

### Recherche Efficace

#### Utilisez des Mots-Clés Courts

Au lieu de :
```
"comment lister tous les services windows en cours d'exécution"
```

Tapez simplement :
```
"service windows"
```

#### Profitez du Fuzzy Matching

Même avec des fautes de frappe, vous trouverez :
- `serviec` → trouve "service"
- `netwrok` → trouve "network"
- `usr` → trouve "user"

#### Recherchez par Tags

Les tags sont très utiles pour trouver des types de commandes :
- `diagnostic` → Commandes de diagnostic
- `security` → Commandes de sécurité
- `performance` → Commandes d'optimisation

### Widget des Commandes Récentes

Le widget sur la page d'accueil est très pratique :
- **Cliquez** sur une commande pour la recopier instantanément
- Plus besoin d'aller dans l'historique pour retrouver une commande récente

### Export Régulier

Exportez votre configuration **une fois par mois** :
- Protection contre la perte de données
- Sauvegarde de vos favoris et historique
- Migration facile vers un nouveau PC

---

## Résolution de Problèmes

### Problème : L'application ne démarre pas

**Causes possibles :**
1. .NET 8 Runtime manquant
2. Permissions insuffisantes
3. Base de données corrompue

**Solutions :**

#### 1. Vérifier .NET 8 Runtime

Ouvrez PowerShell et tapez :
```powershell
dotnet --version
```

Si la commande échoue ou affiche une version < 8.0 :
1. Téléchargez [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Installez-le
3. Redémarrez votre PC

#### 2. Vérifier les Permissions

Vérifiez que vous avez accès en écriture à :
```
%LOCALAPPDATA%\TwinShell
%APPDATA%\TwinShell
```

Si nécessaire, exécutez TwinShell **en tant qu'administrateur** (clic droit → "Exécuter en tant qu'administrateur").

#### 3. Réinitialiser la Base de Données

Si la base de données est corrompue :
1. Fermez TwinShell
2. Supprimez le fichier : `%LOCALAPPDATA%\TwinShell\twinshell.db`
3. Relancez TwinShell (une nouvelle base sera créée)

> ⚠️ **Attention** : Vous perdrez vos favoris et historique. Exportez d'abord si possible.

---

### Problème : Le thème ne change pas après sauvegarde

**Causes possibles :**
1. Paramètres non sauvegardés
2. Fichier settings.json en lecture seule

**Solutions :**

#### 1. Vérifier la Sauvegarde

- Assurez-vous de cliquer sur **"Save"** (et non "Cancel")
- Vérifiez qu'une notification ✅ "Paramètres sauvegardés" s'affiche

#### 2. Vérifier le Fichier settings.json

Ouvrez l'Explorateur Windows et allez dans :
```
%APPDATA%\TwinShell\settings.json
```

Vérifiez le contenu :
```json
{
  "Theme": "Dark",  // Doit être "Light", "Dark" ou "System"
  ...
}
```

Si le fichier est en **lecture seule** :
1. Clic droit → Propriétés
2. Décochez "Lecture seule"
3. Cliquez sur "Appliquer"

#### 3. Redémarrer l'Application

Si le thème ne se charge pas :
1. Fermez complètement TwinShell (vérifiez le Gestionnaire des tâches)
2. Relancez l'application

---

### Problème : Je ne trouve pas une commande

**Causes possibles :**
1. Filtres actifs
2. Recherche trop spécifique
3. Catégorie non sélectionnée

**Solutions :**

#### 1. Désactiver les Filtres

- Décochez tous les filtres **Platform** et **Level**
- Cliquez sur **"📋 All Commands"** pour voir toutes les commandes

#### 2. Simplifier la Recherche

Au lieu de :
```
"comment obtenir les informations d'un utilisateur active directory"
```

Tapez :
```
"AD user"
```

#### 3. Vérifier l'Existence

Toutes les commandes ne sont pas forcément dans la base de données.

Consultez la liste complète dans : `data/seed/initial-actions.json`

Si une commande manque, vous pouvez :
- Créer une issue sur GitHub pour demander son ajout
- Modifier manuellement le fichier JSON (nécessite des connaissances techniques)

---

### Problème : Les raccourcis clavier ne fonctionnent pas

**Causes possibles :**
1. La fenêtre TwinShell n'a pas le focus
2. Conflit avec une autre application

**Solutions :**

#### 1. Vérifier le Focus

- Cliquez sur la fenêtre TwinShell pour lui donner le focus
- Les raccourcis ne fonctionnent que si TwinShell est au premier plan

#### 2. Vérifier les Conflits

Certaines applications interceptent les raccourcis globaux :
- **Ctrl+,** : Peut être utilisé par des IDE (Visual Studio Code, etc.)
- **F5** : Peut être utilisé par des navigateurs

**Solution :**
- Fermez temporairement l'application conflictuelle
- Ou utilisez les menus au lieu des raccourcis

#### 3. Consulter la Liste des Raccourcis

Appuyez sur **F1** pour voir tous les raccourcis actifs.

---

### Problème : Catégorie non modifiable/supprimable

**Cause :**
Les **catégories système** sont protégées.

**Identification :**
Les catégories système ont un **badge jaune "System"** dans le gestionnaire de catégories.

**Catégories système :**
- Active Directory
- DNS
- GPO
- Logs
- Services
- Network
- System

**Vous NE POUVEZ PAS :**
- Renommer une catégorie système
- Supprimer une catégorie système

**Vous POUVEZ :**
- Masquer une catégorie système (bouton "Hide")
- Créer une nouvelle catégorie personnalisée similaire

---

### Problème : Import de configuration échoue

**Causes possibles :**
1. Fichier JSON invalide
2. Fichier corrompu
3. Format incompatible

**Solutions :**

#### 1. Vérifier le Format JSON

Ouvrez le fichier JSON dans un éditeur de texte (Notepad++, VS Code) :

**Bon format :**
```json
{
  "favorites": [...],
  "history": [...],
  "settings": {...}
}
```

**Mauvais format :**
```
{
  favorites: [... // Guillemets manquants
```

#### 2. Valider le JSON

Utilisez un validateur JSON en ligne :
- [jsonlint.com](https://jsonlint.com/)
- Collez le contenu du fichier
- Corrigez les erreurs signalées

#### 3. Réexporter une Nouvelle Configuration

Si le fichier est corrompu :
1. Exportez une nouvelle configuration depuis TwinShell
2. Comparez les deux fichiers pour identifier les différences
3. Fusionnez manuellement les données si nécessaire

---

### Problème : Base de données volumineuse

**Cause :**
L'historique contient des milliers de commandes.

**Solution :**

#### 1. Réduire la Rétention

Paramètres → **Command History Retention** → Réduisez à 30 ou 60 jours

#### 2. Nettoyer Manuellement

Vue Historique → Sélectionnez des entrées → Supprimez-les

#### 3. Réinitialiser l'Historique

⚠️ **ATTENTION** : Cela supprime tout l'historique.

1. Fermez TwinShell
2. Supprimez : `%LOCALAPPDATA%\TwinShell\twinshell.db`
3. Relancez TwinShell

#### 4. Exporter Avant Nettoyage

Exportez d'abord votre configuration pour sauvegarder vos favoris.

---

## 📚 Ressources Supplémentaires

### Documentation

- 🚀 [Guide de Démarrage Rapide](QuickStart.md)
- ❓ [FAQ - Questions Fréquentes](FAQ.md)
- 🏠 [README Principal](../README.md)
- 🔧 [Documentation Développeur](developer/)

### Support

- 💬 [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)
- 🐛 [Signaler un Bug](https://github.com/VBlackJack/TwinShell/issues)

### Communauté

- ⭐ [Star sur GitHub](https://github.com/VBlackJack/TwinShell)
- 🤝 [Contribuer au Projet](developer/CONTRIBUTING.md)

---

**Bon travail avec TwinShell !** 🚀

*Dernière mise à jour : 2025-01-18*
*Version : 1.0.0*
