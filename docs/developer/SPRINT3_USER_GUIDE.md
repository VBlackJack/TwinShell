# 📘 TwinShell Sprint 3 - Guide Utilisateur

Bienvenue dans TwinShell avec les nouvelles fonctionnalités du Sprint 3 ! Ce guide vous aidera à tirer le meilleur parti des améliorations UI/UX et de customisation.

---

## 🌙 Mode Sombre

### Activation du Mode Sombre

1. **Via le menu** : `File` → `Settings...`
2. Dans la section **Appearance**, sélectionnez votre thème préféré :
   - **Light** - Thème clair (par défaut)
   - **Dark** - Thème sombre pour réduire la fatigue oculaire
   - **System** - Suit automatiquement le thème Windows

3. Cliquez sur **Preview Theme** pour voir le résultat immédiatement
4. Cliquez sur **Save** pour enregistrer votre préférence

### Raccourci Clavier
- **Ctrl+,** (Ctrl+virgule) ouvre directement les paramètres

### Caractéristiques
- ✅ Contraste WCAG AA (ratio 4.5:1 minimum)
- ✅ Tous les écrans et contrôles supportés
- ✅ Transition fluide sans clignotement
- ✅ Préférence sauvegardée automatiquement

---

## 📁 Catégories Personnalisées

### Créer une Nouvelle Catégorie

1. **Via le menu** : `Categories` → `Manage Categories...`
2. Cliquez sur **+ Add New Category**
3. Remplissez le formulaire :
   - **Nom** : Nom de votre catégorie (ex: "Backup quotidien")
   - **Icône** : Choisissez parmi 24 icônes disponibles
   - **Couleur** : Sélectionnez une des 12 couleurs proposées
   - **Description** (optionnel) : Décrivez l'usage de cette catégorie

4. Cliquez sur **Save Category**

### Raccourci Clavier
- **Ctrl+M** ouvre directement la gestion des catégories

### Gérer les Catégories

#### Modifier une Catégorie
1. Dans la liste de gauche, sélectionnez la catégorie
2. Cliquez sur **Edit Category**
3. Modifiez les informations
4. **Save** pour enregistrer

#### Réorganiser les Catégories
1. Sélectionnez une catégorie
2. Utilisez les boutons **Move Up** et **Move Down**
3. L'ordre est sauvegardé automatiquement

#### Masquer une Catégorie
1. Sélectionnez la catégorie
2. Cliquez sur **Hide** (ou **Show** pour la réafficher)
3. Les catégories masquées ne disparaissent pas de la liste mais ne s'affichent plus dans la navigation

#### Supprimer une Catégorie
1. Sélectionnez la catégorie
2. Cliquez sur **Delete Category**
3. Confirmez la suppression
4. ⚠️ **Note** : Les catégories système (marquées "System") ne peuvent pas être supprimées

### Limites
- Maximum **50 catégories** pour éviter la surcharge
- Les catégories système sont protégées contre la modification/suppression

---

## ⚙️ Paramètres Avancés

### Accès aux Paramètres
- **Menu** : `File` → `Settings...`
- **Raccourci** : `Ctrl+,`

### Options Disponibles

#### Section Apparence
- **Theme** : Light | Dark | System
- **Preview Theme** : Prévisualiser avant de sauvegarder

#### Section Comportement

**Historique**
- **Command History Retention** : Nombre de jours de conservation (1-3650 jours)
  - Par défaut : 90 jours
  - Les commandes plus anciennes sont automatiquement supprimées

**Affichage**
- **Maximum History Items** : Nombre max d'items dans les vues historique (10-100,000)
  - Par défaut : 1000 items

**Widget Commandes Récentes**
- **Recent Commands Count** : Nombre de commandes récentes affichées (1-50)
  - Par défaut : 5 commandes
- **Show Recent Commands Widget** : Afficher/masquer le widget au démarrage

**Sécurité**
- **Confirm before executing dangerous commands** : Active les confirmations pour les commandes critiques
  - Recommandé : ✅ Activé

### Réinitialiser les Paramètres
1. Cliquez sur **Reset to Defaults**
2. Confirmez l'action
3. Tous les paramètres reviennent aux valeurs par défaut

### Sauvegarde des Paramètres
- Les paramètres sont automatiquement sauvegardés dans :
  ```
  %APPDATA%\TwinShell\settings.json
  ```
- Vous pouvez sauvegarder ce fichier pour restaurer vos préférences plus tard

---

## ⌨️ Raccourcis Clavier

### Raccourcis Globaux

| Raccourci | Action |
|-----------|--------|
| **Ctrl+,** | Ouvrir les Paramètres |
| **Ctrl+M** | Gérer les Catégories |
| **Ctrl+E** | Exporter la Configuration |
| **Ctrl+I** | Importer la Configuration |
| **F1** | Afficher l'Aide |
| **F5** | Actualiser les Actions |
| **Tab** | Naviguer entre les contrôles |
| **Esc** | Annuler/Fermer |
| **Alt+F4** | Quitter l'Application |

### Navigation au Clavier
- Toutes les fonctionnalités sont accessibles au clavier
- **Tab** et **Shift+Tab** pour naviguer
- **Enter** pour activer
- **Espace** pour cocher/décocher
- **Flèches** pour naviguer dans les listes

### Aide Intégrée
- Appuyez sur **F1** pour voir tous les raccourcis disponibles
- Menu `Help` → `Keyboard Shortcuts` pour la liste complète

---

## 🔔 Notifications

### Types de Notifications

L'application affiche des notifications toast en haut à droite pour :

#### Informations (Bleu)
- Actions réussies
- Chargement de données
- Durée : 3 secondes

#### Succès (Vert)
- Sauvegarde réussie
- Création de catégorie
- Export complété
- Durée : 3 secondes

#### Avertissements (Orange)
- Actions nécessitant attention
- Limites atteintes
- Durée : 4 secondes

#### Erreurs (Rouge)
- Échec d'une opération
- Validation échouée
- Problème de connexion
- Durée : 5 secondes

### Caractéristiques
- ✅ Apparition fluide (animation fade-in)
- ✅ Disparition automatique
- ✅ Effet d'ombre pour la lisibilité
- ✅ Positionnement non-intrusif

---

## 🎨 Animations et Transitions

### Animations Intégrées

L'interface utilise des animations subtiles pour améliorer l'expérience :

#### Transitions de Page
- Fade in/out (300ms)
- Apparition progressive du contenu

#### Interactions
- **Hover** : Déplacement léger des items de liste
- **Click** : Effet ripple sur les boutons
- **Selection** : Mise en surbrillance animée

#### Chargement
- **Spinner** : Barre de progression indéterminée
- **Skeleton Loaders** : Placeholders animés pendant le chargement

### Performance
- Toutes les animations sont **< 300ms** (conformité WCAG)
- Optimisées pour ne pas ralentir l'interface
- Utilisation d'easing functions pour la fluidité

---

## ♿ Accessibilité

### Navigation au Clavier
- ✅ **100% accessible au clavier**
- ✅ Tous les contrôles ont un **focus visible**
- ✅ Tab navigation logique
- ✅ Raccourcis pour toutes les actions principales

### Contraste et Lisibilité
- ✅ **WCAG AA compliant** (ratio 4.5:1+)
- ✅ Texte lisible en Light et Dark mode
- ✅ Couleurs sémantiques (rouge=danger, vert=succès)

### Support Lecteur d'Écran
- Propriétés d'automation configurées
- Descriptions textuelles pour tous les contrôles
- Compatible avec les lecteurs d'écran modernes

### Taille de Fenêtre
- **Minimum** : 800x600 pixels
- **Responsive** : S'adapte jusqu'à plein écran
- Layouts flexibles qui ne cassent pas

---

## 💡 Conseils et Astuces

### Optimiser votre Workflow

1. **Créez des catégories thématiques**
   - Exemple : "Tâches Quotidiennes", "Maintenance", "Urgences"
   - Assignez des couleurs distinctes pour reconnaissance rapide

2. **Utilisez les raccourcis clavier**
   - Gagnez du temps avec Ctrl+M, F5, etc.
   - Mémorisez F1 pour l'aide instantanée

3. **Configurez le mode sombre**
   - Réduisez la fatigue oculaire lors de sessions longues
   - Utilisez le mode System pour changement automatique jour/nuit

4. **Ajustez les préférences d'historique**
   - Réduisez la rétention si l'espace disque est limité
   - Augmentez pour conserver un historique étendu

5. **Activez les confirmations pour actions dangereuses**
   - Évite les erreurs accidentelles
   - Recommandé pour environnements de production

### Résolution de Problèmes

#### Le thème ne change pas
- Vérifiez que vous avez cliqué sur **Save**
- Redémarrez l'application si nécessaire
- Vérifiez le fichier settings.json dans %APPDATA%

#### Catégorie non modifiable
- Vérifiez si c'est une catégorie **System** (badge jaune)
- Les catégories système sont protégées

#### Raccourcis clavier ne fonctionnent pas
- Assurez-vous que la fenêtre TwinShell a le focus
- Vérifiez qu'aucune autre application n'intercepte le raccourci
- Consultez F1 pour la liste des raccourcis actifs

---

## 📊 Fichiers et Données

### Emplacement des Données

#### Base de Données
```
%LOCALAPPDATA%\TwinShell\twinshell.db
```
Contient :
- Actions
- Historique de commandes
- Favoris
- **Catégories personnalisées** (nouveau)

#### Paramètres Utilisateur
```
%APPDATA%\TwinShell\settings.json
```
Contient :
- Thème sélectionné
- Préférences de comportement
- Options d'affichage

### Sauvegarde Recommandée

Pour sauvegarder vos configurations :
1. **Export via menu** : `File` → `Export Configuration...`
2. **Copie manuelle** des fichiers :
   - `twinshell.db` (base de données)
   - `settings.json` (préférences)

### Restauration
1. **Import via menu** : `File` → `Import Configuration...`
2. **Ou copie manuelle** des fichiers dans les emplacements correspondants

---

## 🆘 Support

### Aide Intégrée
- **F1** : Aide rapide avec raccourcis
- **Menu Help** → **About** : Informations sur l'application
- **Menu Help** → **Keyboard Shortcuts** : Liste complète des raccourcis

### Documentation
- **SPRINT3_SUMMARY.md** : Vue d'ensemble technique
- **SPRINT3_FINAL_REPORT.md** : Rapport complet du sprint
- **MIGRATION_NOTES.md** : Notes pour administrateurs

### Feedback
Pour signaler un bug ou suggérer une amélioration :
- Créez une issue sur le repository GitHub
- Incluez des captures d'écran si possible
- Décrivez les étapes pour reproduire

---

## 🎉 Profitez de TwinShell !

Vous disposez maintenant d'un outil puissant et personnalisable pour gérer vos commandes PowerShell et Bash.

**Fonctionnalités Sprint 3** :
- ✅ Mode sombre professionnel
- ✅ Catégories personnalisées illimitées (50 max)
- ✅ Interface fluide avec animations
- ✅ Raccourcis clavier complets
- ✅ Notifications informatives
- ✅ Accessibilité WCAG AA

**Bon travail et productivité accrue !** 🚀

---

*Guide Version: 1.0 - Sprint 3*
*Dernière mise à jour: 2025-01-16*
*Application: TwinShell v1.0*
