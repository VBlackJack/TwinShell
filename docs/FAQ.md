# ❓ FAQ - Questions Fréquemment Posées

**Trouvez rapidement des réponses aux questions les plus courantes sur TwinShell**

---

## Table des Matières

- [Général](#général)
- [Installation](#installation)
- [Recherche](#recherche)
- [Favoris et Historique](#favoris-et-historique)
- [Catégories](#catégories)
- [Thèmes et Interface](#thèmes-et-interface)
- [Export et Import](#export-et-import)
- [Sécurité](#sécurité)
- [Dépannage](#dépannage)
- [Fonctionnalités Futures](#fonctionnalités-futures)

---

## Général

### Q : TwinShell fonctionne-t-il sur Mac ou Linux ?

**R :** Non, TwinShell est une application **Windows WPF** (.NET 8). Elle nécessite :
- Windows 10 ou Windows 11 (64-bit)
- .NET 8 Runtime

Il n'existe pas de version Mac ou Linux pour le moment.

---

### Q : Puis-je exécuter les commandes directement depuis TwinShell ?

**R :** Non, TwinShell est un **générateur de commandes**, pas un terminal.

**Workflow :**
1. Sélectionnez une commande dans TwinShell
2. Remplissez les paramètres
3. Copiez la commande générée
4. Collez-la dans PowerShell ou Bash
5. Exécutez-la

**Raison :** Cela vous permet de **vérifier et modifier** la commande avant de l'exécuter (sécurité).

---

### Q : Les commandes Bash fonctionnent-elles sur Windows ?

**R :** Oui, **si vous avez installé WSL** (Windows Subsystem for Linux).

**Étapes :**
1. Installez WSL : [Guide Microsoft](https://docs.microsoft.com/fr-fr/windows/wsl/install)
2. Ouvrez un terminal WSL (ex: Ubuntu)
3. Collez et exécutez les commandes Bash générées par TwinShell

**Sans WSL :** Les commandes Bash ne fonctionneront pas sur Windows natif (CMD/PowerShell).

---

### Q : Combien de commandes sont disponibles dans TwinShell ?

**R :** TwinShell inclut **479 commandes** PowerShell et Bash au lancement initial.

**Catégories couvertes (15 au total) :**
- 🏢 Active Directory & GPO (utilisateurs, groupes, GPO)
- 🌐 Network & DNS (ping, traceroute, DNS)
- 📊 Monitoring & Logs (EventLog, journald)
- ⚙️ Services & Automation (services, tâches planifiées)
- 💻 Windows Optimization (maintenance Windows)
- 🐧 Linux Administration (administration Linux)
- 📦 Package Management (apt, yum, winget, choco)
- 📁 Files & Storage (fichiers, stockage)
- 🔒 Security & Encryption (sécurité, BitLocker)
- 🔄 Windows Updates (mises à jour)
- Et plus encore...

**Évolution :** De nouvelles commandes sont régulièrement ajoutées.

---

### Q : Puis-je ajouter mes propres commandes ?

**R :** **Pas encore via l'interface.**

**Méthode actuelle (technique) :**
1. Modifiez le fichier `data/seed/initial-actions.json`
2. Ajoutez votre commande au format JSON
3. Relancez l'application (la base de données sera mise à jour)

**À venir :** Une interface pour créer des commandes personnalisées est prévue dans une future version.

---

### Q : Où sont stockées mes données ?

**R :** TwinShell stocke vos données localement dans deux emplacements :

| Données | Emplacement | Format |
|---------|-------------|--------|
| **Base de données** (Actions, Favoris, Historique, Catégories) | `%LOCALAPPDATA%\TwinShell\twinshell.db` | SQLite |
| **Paramètres** (Thème, préférences) | `%APPDATA%\TwinShell\settings.json` | JSON |

**Exemple de chemins Windows :**
```
C:\Users\VotreNom\AppData\Local\TwinShell\twinshell.db
C:\Users\VotreNom\AppData\Roaming\TwinShell\settings.json
```

---

## Installation

### Q : Quelle version de .NET dois-je installer ?

**R :** Vous devez installer **.NET 8 Runtime** (version 8.0 ou supérieure).

**Téléchargement :**
- Page officielle : [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- Choisissez : **".NET Desktop Runtime 8.0.x"** (incluant WPF)

**Vérifier l'installation :**
```powershell
dotnet --version
# Doit afficher : 8.0.x
```

---

### Q : Quelle est la différence entre la version Setup et Portable ?

**R :**

| Caractéristique | Version Setup | Version Portable |
|-----------------|---------------|------------------|
| **Installation** | Nécessite un installeur | Décompression ZIP |
| **Emplacement** | Program Files | N'importe où |
| **Menu Démarrer** | Oui, raccourci créé | Non, lancement manuel |
| **Désinstallation** | Via "Programmes et fonctionnalités" | Suppression du dossier |
| **Mises à jour** | Via installeur | Remplacement manuel |
| **Recommandée pour** | Usage quotidien | USB, machines restreintes |

**Recommandation :** Utilisez la **version Setup** pour une installation classique.

---

### Q : TwinShell nécessite-t-il des droits administrateur ?

**R :** **Non** pour l'utilisation normale de TwinShell.

**Cependant :**
- Certaines **commandes générées** peuvent nécessiter des droits admin (ex: gestion de services, modification de GPO)
- L'**installation** (version Setup) peut demander des droits admin

**Conseil :** Lancez PowerShell **en tant qu'administrateur** si vous devez exécuter des commandes privilégiées.

---

## Recherche

### Q : Pourquoi ma recherche "Get-Service" trouve-t-elle aussi "Get Service" et "GetService" ?

**R :** TwinShell normalise la ponctuation pour une recherche **plus tolérante**.

**Normalisation appliquée :**
- Tirets (`-`) → Espace
- Underscores (`_`) → Espace
- Points (`.`) → Espace

**Exemples :**
- `Get-Service` → `get service`
- `List_AD_Users` → `list ad users`
- `System.Management` → `system management`

**Avantage :** Vous trouvez des commandes même si vous ne tapez pas la syntaxe exacte.

---

### Q : La recherche est-elle sensible aux accents ?

**R :** **Non**, les accents sont automatiquement ignorés.

**Exemples :**
- Recherche : `reseau` → Trouve "Configuration Réseau"
- Recherche : `cafe` → Trouve "Café"
- Recherche : `nino` → Trouve "Niño"

**Langues supportées :** Français, Espagnol, Allemand, Portugais, etc.

---

### Q : Comment chercher plusieurs mots ?

**R :** Tapez simplement les mots **séparés par des espaces**.

**Logique AND :** TOUS les mots doivent être présents dans la commande.

**Exemples :**

| Recherche | Résultats |
|-----------|-----------|
| `ad user` | Actions contenant "ad" ET "user" (ex: "List AD Users") |
| `dns cache` | Actions contenant "dns" ET "cache" (ex: "Clear DNS Cache") |
| `service windows` | Actions contenant "service" ET "windows" |

**Ordre indifférent :** `user ad` et `ad user` donnent les mêmes résultats.

---

### Q : Comment utiliser la recherche fuzzy ?

**R :** La recherche fuzzy est **automatique** !

TwinShell tolère jusqu'à **30% de différence** entre votre recherche et les mots dans les commandes.

**Exemples de fautes tolérées :**
- `serviec` → Trouve "service" (2 lettres inversées)
- `netwrok` → Trouve "network" (1 lettre mal placée)
- `usr` → Trouve "user" (1 lettre manquante)

**Pas de configuration nécessaire.**

---

### Q : Pourquoi ma recherche ne donne aucun résultat ?

**R :** Vérifiez ces points :

#### 1. Filtres actifs

- Désactivez les filtres **Platform** (Windows/Linux/Both)
- Désactivez les filtres **Level** (Info/Run/Dangerous)

#### 2. Catégorie sélectionnée

- Cliquez sur **"📋 All Commands"** pour voir toutes les commandes

#### 3. Recherche trop spécifique

- Essayez une recherche plus courte
- Exemple : Au lieu de `"comment obtenir les services"`, tapez `"service"`

#### 4. Commande inexistante

- Toutes les commandes ne sont pas forcément dans la base
- Consultez la liste dans `data/seed/initial-actions.json`

---

## Favoris et Historique

### Q : Combien de favoris puis-je avoir ?

**R :** **Maximum 50 favoris** par utilisateur.

**Raison :** Limiter le nombre de favoris force à sélectionner les commandes **vraiment importantes**, améliorant l'organisation.

**Si vous atteignez la limite :**
- Un message s'affiche : "Limite de favoris atteinte (50/50)"
- Retirez un favori existant pour en ajouter un nouveau

---

### Q : Combien de temps l'historique est-il conservé ?

**R :** **Par défaut : 90 jours**

**Configurable** dans les paramètres (de **1 à 3650 jours**).

**Comment modifier :**
1. `Ctrl+,` (Paramètres)
2. Section **Behavior** → **Command History Retention**
3. Changez la valeur (ex: 30, 180, 365 jours)
4. Cliquez sur **"Save"**

**Nettoyage automatique :** Les commandes plus anciennes que la durée configurée sont automatiquement supprimées.

---

### Q : Puis-je récupérer une commande supprimée de l'historique ?

**R :** **Non**, la suppression d'une commande de l'historique est **définitive**.

**Prévention :**
- Exportez régulièrement votre configuration (`Ctrl+E`)
- L'export inclut l'historique complet

**Récupération possible :**
- Si vous avez un export récent, importez-le (`Ctrl+I`)
- Les commandes de l'export seront fusionnées avec l'historique actuel

---

### Q : Les favoris sont-ils sauvegardés si je désinstalle TwinShell ?

**R :** **Ça dépend.**

| Méthode | Favoris conservés ? |
|---------|---------------------|
| **Désinstallation standard** | ❌ Non (base de données supprimée) |
| **Export avant désinstallation** | ✅ Oui (si vous importez après réinstallation) |

**Bonne pratique :**
1. Exportez votre configuration (`Ctrl+E`)
2. Désinstallez TwinShell
3. Réinstallez TwinShell
4. Importez le fichier exporté (`Ctrl+I`)

---

## Catégories

### Q : Quelle est la différence entre catégories système et personnalisées ?

**R :**

| Caractéristique | Catégories Système | Catégories Personnalisées |
|-----------------|-------------------|---------------------------|
| **Préinstallées** | ✅ Oui | ❌ Non, créées par l'utilisateur |
| **Badge** | 🟡 Jaune "System" | 🟢 Vert "Custom" |
| **Renommer** | ❌ Non | ✅ Oui |
| **Supprimer** | ❌ Non | ✅ Oui |
| **Masquer** | ✅ Oui | ✅ Oui |
| **Exemples** | AD, DNS, Logs, Services | Backup, Monitoring, Urgences |

---

### Q : Combien de catégories personnalisées puis-je créer ?

**R :** **Maximum 50 catégories personnalisées**.

**Message si limite atteinte :**
```
⚠️ Limite de catégories atteinte (50/50)
Supprimez une catégorie existante pour en créer une nouvelle.
```

**Note :** Les catégories système ne comptent pas dans cette limite.

---

### Q : Que se passe-t-il si je supprime une catégorie personnalisée ?

**R :** La catégorie est **retirée de toutes les actions** qui l'utilisent.

**Les actions NE SONT PAS supprimées**, seule la référence à la catégorie est effacée.

**Exemple :**
- Catégorie "Backup" assignée à 5 actions
- Vous supprimez "Backup"
- Les 5 actions existent toujours, mais n'ont plus de catégorie "Backup"

**Confirmation :**
Avant de supprimer, un message affiche le **nombre d'actions affectées**.

---

### Q : Puis-je assigner plusieurs catégories à une action ?

**R :** **Pas encore** dans la version actuelle (v1.2.0).

Chaque action a **une seule catégorie**.

**À venir :** Le support multi-catégories est prévu dans une future version.

---

### Q : Pourquoi ne puis-je pas supprimer la catégorie "Active Directory" ?

**R :** "Active Directory" est une **catégorie système**, protégée contre la suppression.

**Catégories système non supprimables :**
- Active Directory & GPO
- Network & DNS
- Monitoring & Logs
- Services & Automation
- Windows Optimization
- Linux Administration
- Package Management
- Files & Storage
- Security & Encryption
- Windows Updates
- User Management
- Containers & VMs
- Database
- Development
- Performance

**Alternative :**
- Vous pouvez **masquer** la catégorie (bouton "Hide")
- Créez une nouvelle catégorie personnalisée si besoin

---

## Thèmes et Interface

### Q : Le thème ne change pas après avoir cliqué sur "Save"

**R :** Vérifiez ces points :

#### 1. Avez-vous cliqué sur "Save" ?

- Assurez-vous de cliquer sur **"Save"** (et non "Cancel")
- Une notification doit s'afficher : ✅ "Paramètres sauvegardés"

#### 2. Redémarrez l'application

- Fermez complètement TwinShell (vérifiez le Gestionnaire des tâches)
- Relancez l'application

#### 3. Vérifiez le fichier settings.json

Ouvrez :
```
%APPDATA%\TwinShell\settings.json
```

Vérifiez la valeur de `Theme` :
```json
{
  "Theme": "Dark"  // Doit être "Light", "Dark" ou "System"
}
```

#### 4. Fichier en lecture seule ?

- Clic droit sur `settings.json` → Propriétés
- Décochez "Lecture seule" si coché

---

### Q : Quelle est la différence entre le mode "Dark" et le mode "System" ?

**R :**

| Mode | Comportement |
|------|-------------|
| **Light** | Thème clair en permanence (fond blanc, texte noir) |
| **Dark** | Thème sombre en permanence (fond noir, texte blanc) |
| **System** | Suit automatiquement le thème Windows (clair le jour, sombre la nuit par exemple) |

**Mode "System" - Comment ça marche :**
1. TwinShell lit le registre Windows : `HKEY_CURRENT_USER\...\Personalize\AppsUseLightTheme`
2. Si Windows est en mode sombre → TwinShell bascule en mode sombre
3. Si Windows est en mode clair → TwinShell bascule en mode clair
4. Le changement est **automatique et instantané**

**Idéal pour :** Ceux qui changent de thème Windows selon l'heure de la journée.

---

### Q : Le thème sombre est-il accessible (WCAG) ?

**R :** **Oui**, les thèmes TwinShell respectent **WCAG AAA** (niveau le plus élevé).

**Contraste vérifié :**
| Élément | Contraste | Norme WCAG AAA | Statut |
|---------|-----------|----------------|--------|
| Texte principal | 11.6:1 | 7:1 requis | ✅ Conforme |
| Texte secondaire | 9.2:1 | 7:1 requis | ✅ Conforme |
| Éléments UI | 7.2:1 | 3:1 requis | ✅ Conforme |

**Avantages :**
- Lisibilité optimale même pour les personnes malvoyantes
- Réduction de la fatigue oculaire lors de longues sessions

---

### Q : Puis-je créer mon propre thème personnalisé ?

**R :** **Pas encore** dans l'interface utilisateur.

**Méthode technique (développeurs) :**
1. Créez un nouveau fichier `Themes/CustomTheme.xaml`
2. Définissez vos couleurs (SolidColorBrush)
3. Modifiez `ThemeService.cs` pour charger votre thème
4. Recompilez l'application

**À venir :** Un éditeur de thèmes est prévu dans une future version.

---

## Export et Import

### Q : Que contient un fichier d'export ?

**R :** Le fichier JSON exporté contient **3 sections** :

```json
{
  "favorites": [...],      // Liste de vos favoris
  "history": [...],        // Historique complet des commandes
  "settings": {...}        // Vos paramètres (thème, rétention, etc.)
}
```

**Favoris :**
- IDs des actions favorites
- Date d'ajout aux favoris

**Historique :**
- Commandes générées complètes
- Paramètres utilisés
- Dates de génération
- Catégories et plateformes

**Paramètres :**
- Thème sélectionné
- Durée de rétention de l'historique
- Nombre de commandes récentes
- Options de comportement

---

### Q : Puis-je partager mes favoris avec un collègue ?

**R :** **Oui !**

**Méthode :**
1. Exportez votre configuration (`Ctrl+E`)
2. Partagez le fichier JSON avec votre collègue
3. Votre collègue importe le fichier (`Ctrl+I`)
4. Ses favoris sont fusionnés avec les vôtres (pas de doublon)

**Idéal pour :**
- Standardiser les commandes dans une équipe
- Partager des "best practices"
- Former de nouveaux administrateurs système

---

### Q : L'import supprime-t-il mes données existantes ?

**R :** **Non**, l'import utilise un **mode fusion**.

**Comportement :**
- **Favoris** : Les favoris importés sont **ajoutés** (pas de remplacement)
- **Historique** : Les commandes importées sont **fusionnées** avec l'existant
- **Paramètres** : Les paramètres importés **remplacent** les existants

**Exemple :**
- Vous avez 10 favoris
- Vous importez un fichier avec 5 favoris (dont 2 déjà présents)
- Résultat : 13 favoris (10 existants + 3 nouveaux)

---

### Q : Comment migrer TwinShell vers un nouveau PC ?

**R :** **4 étapes simples :**

**Sur l'ancien PC :**
1. Exportez votre configuration (`Ctrl+E`)
2. Sauvegardez le fichier JSON (ex: sur clé USB ou cloud)

**Sur le nouveau PC :**
3. Installez TwinShell
4. Importez le fichier JSON (`Ctrl+I`)

**Résultat :** Vous retrouvez tous vos favoris, historique et paramètres !

---

## Sécurité

### Q : TwinShell exécute-t-il les commandes automatiquement ?

**R :** **Non**, TwinShell **ne jamais exécute** de commandes.

**Workflow sécurisé :**
1. TwinShell **génère** la commande
2. Vous **copiez** la commande
3. Vous **collez** dans PowerShell/Bash
4. Vous **vérifiez** la commande
5. **Vous** exécutez manuellement

**Avantage :** Vous gardez le contrôle total et pouvez modifier avant d'exécuter.

---

### Q : Que signifie le niveau "Dangerous" ?

**R :** Les commandes de niveau **Dangerous** peuvent **modifier le système** ou **supprimer des données**.

**Exemples de commandes dangereuses :**
- `Clear-EventLog` : Efface les logs Windows (perte de données)
- `Disable-ADAccount` : Désactive un compte Active Directory (impact utilisateur)
- `Stop-Process -Force` : Force l'arrêt d'un processus (risque de perte de données)
- `Remove-Item -Recurse` : Supprime des fichiers/dossiers récursivement

**Protection dans TwinShell :**
- Bandeau d'avertissement rouge affiché
- Option de confirmation avant copie (configurable dans les paramètres)

**Recommandation :** Vérifiez **toujours** les paramètres avant d'exécuter une commande dangereuse.

---

### Q : TwinShell stocke-t-il mes mots de passe ou identifiants ?

**R :** **Non**.

TwinShell stocke uniquement :
- Les **paramètres** que vous remplissez (ex: nom de service, nom d'utilisateur AD)
- **Jamais** de mots de passe ou secrets

**Exemple :**
- Action : "Reset User Password"
- Stocké : Nom d'utilisateur (`jdoe`)
- **Non stocké** : Nouveau mot de passe

**Les mots de passe** doivent être entrés **au moment de l'exécution** dans le terminal.

---

### Q : Les données sont-elles envoyées sur Internet ?

**R :** **Non**, TwinShell est **100% local**.

**Aucune donnée n'est envoyée** :
- Pas de télémétrie
- Pas d'analytiques
- Pas de connexion cloud (sauf si vous activez la synchronisation cloud dans une future version)

**Toutes les données** (favoris, historique, paramètres) restent sur votre PC.

---

## Dépannage

### Q : L'application ne démarre pas - Erreur ".NET Runtime manquant"

**R :** Installez **.NET 8 Runtime**.

**Téléchargement :**
```
https://dotnet.microsoft.com/download/dotnet/8.0
```

**Choisissez :** ".NET Desktop Runtime 8.0.x" (incluant WPF)

**Après installation :**
- Redémarrez votre PC
- Lancez TwinShell

---

### Q : Erreur "Impossible d'accéder à la base de données"

**R :** **Causes possibles :**

#### 1. Permissions insuffisantes

Vérifiez que vous avez accès en **écriture** à :
```
%LOCALAPPDATA%\TwinShell
```

**Solution :**
- Exécutez TwinShell en tant qu'administrateur (temporaire)
- Ajustez les permissions du dossier

#### 2. Base de données corrompue

**Solution :**
1. Exportez d'abord votre configuration (si possible)
2. Fermez TwinShell
3. Supprimez : `%LOCALAPPDATA%\TwinShell\twinshell.db`
4. Relancez TwinShell (une nouvelle base sera créée)
5. Importez votre configuration exportée

---

### Q : Les raccourcis clavier ne fonctionnent pas

**R :** Vérifiez ces points :

#### 1. Focus sur la fenêtre

- Cliquez sur la fenêtre TwinShell pour lui donner le focus
- Les raccourcis ne fonctionnent que si TwinShell est au **premier plan**

#### 2. Conflit avec une autre application

Certaines applications interceptent les raccourcis :
- `Ctrl+,` : Peut être utilisé par Visual Studio Code, IntelliJ, etc.
- `F5` : Peut être utilisé par des navigateurs

**Solution :**
- Fermez temporairement l'application conflictuelle
- Ou utilisez les menus au lieu des raccourcis

#### 3. Vérifier les raccourcis actifs

Appuyez sur **F1** pour afficher la liste complète des raccourcis.

---

### Q : La fenêtre de TwinShell est trop petite / trop grande

**R :** TwinShell est **responsive** et s'adapte à votre écran.

**Taille minimale :** 800x600 pixels
**Taille maximale :** Plein écran

**Redimensionner :**
- Glissez les bords de la fenêtre
- Double-cliquez sur la barre de titre pour maximiser/restaurer
- La taille est sauvegardée entre les sessions

**Problème d'affichage ?**
- Vérifiez la résolution de votre écran (minimum 1024x768 recommandé)
- Ajustez la mise à l'échelle Windows (Paramètres → Affichage → Mise à l'échelle)

---

## Fonctionnalités Futures

### Q : Quand pourrai-je ajouter mes propres commandes via l'interface ?

**R :** Cette fonctionnalité est **disponible depuis la version 1.2.0**.

**Fonctionnalités attendues :**
- Créer une nouvelle commande via un formulaire
- Définir des paramètres personnalisés
- Assigner des catégories et tags
- Sauvegarder dans la base de données

**Suivre l'avancement :** [GitHub Issues](https://github.com/VBlackJack/TwinShell/issues)

---

### Q : Y aura-t-il un support multi-langues (anglais) ?

**R :** Prévu pour une **future version**.

**Langues prévues :**
- 🇫🇷 Français (actuel)
- 🇬🇧 Anglais
- (Autres langues selon la demande communautaire)

**Paramètre :**
Settings → **Language** → Français | English

---

### Q : Est-il prévu d'intégrer un terminal directement dans TwinShell ?

**R :** Oui, c'est dans la **roadmap pour une future version**.

**Fonctionnalités envisagées :**
- Terminal PowerShell intégré
- Terminal Bash (WSL) intégré
- Exécution directe depuis TwinShell
- Affichage du résultat dans l'interface

**Avantage :** Workflow complet sans quitter TwinShell.

---

### Q : La synchronisation cloud est-elle prévue ?

**R :** Oui, **optionnelle** dans une future version (v1.3+).

**Fonctionnalités envisagées :**
- Synchronisation des favoris entre machines
- Synchronisation des catégories personnalisées
- **Opt-in** : Vous choisissez d'activer ou non

**Confidentialité :**
- Chiffrement des données
- Aucune donnée sensible (pas de commandes de l'historique)

---

## Ressources

### Documentation

- [Guide de Demarrage Rapide](QuickStart.md)
- [Guide Utilisateur Complet](UserGuide.md)
- [GitHub TwinShell](https://github.com/VBlackJack/TwinShell)

### Support

- 💬 [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)
- 🐛 [Signaler un Bug](https://github.com/VBlackJack/TwinShell/issues)

---

**Question non listée ?** Posez-la sur [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions) !

---

*Dernière mise à jour : 2025-11-26*
*Version : 1.2.0*
