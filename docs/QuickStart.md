# 🚀 Guide de Démarrage Rapide - TwinShell

**Commencez à utiliser TwinShell en 5 minutes !**

---

## 📥 Installation (2 minutes)

### Étape 1 : Télécharger TwinShell

1. Rendez-vous sur [GitHub Releases](https://github.com/VBlackJack/TwinShell/releases)
2. Téléchargez la dernière version :
   - **TwinShell-Setup.exe** (Installeur - Recommandé)
   - **TwinShell-Portable.zip** (Version portable)

### Étape 2 : Installer

**Option A : Installeur (Recommandé)**
1. Double-cliquez sur `TwinShell-Setup.exe`
2. Suivez l'assistant d'installation
3. Lancez TwinShell depuis le menu Démarrer

**Option B : Version Portable**
1. Décompressez `TwinShell-Portable.zip`
2. Double-cliquez sur `TwinShell.exe`

### Étape 3 : Premier Lancement

Au premier lancement, TwinShell :
- Crée automatiquement sa base de données SQLite
- Charge **479 commandes** PowerShell et Bash
- Est prêt à l'emploi !

---

## 🎯 Vos 3 Premières Actions (3 minutes)

### Action 1 : Trouver et Copier une Commande

1. **Tapez** dans la barre de recherche : `service`
2. **Cliquez** sur "Get-Service" dans la liste
3. **Remplissez** le paramètre (ex: `Spooler` pour le service d'impression)
4. **Cliquez** sur "📋 Copier dans le presse-papiers"
5. **Collez** dans PowerShell et exécutez !

```powershell
Get-Service -Name Spooler
```

✅ **Félicitations !** Vous avez généré votre première commande.

---

### Action 2 : Ajouter un Favori

1. **Recherchez** une commande que vous utilisez souvent (ex: `dns`)
2. **Cliquez** sur l'étoile (☆) à côté du titre
3. L'étoile devient dorée (★) - la commande est ajoutée aux favoris
4. **Cliquez** sur "⭐ Favorites" dans le panneau de gauche
5. Votre commande favorite apparaît !

---

### Action 3 : Activer le Mode Sombre

1. **Appuyez** sur `Ctrl+,` (ou Menu **File → Settings**)
2. Section **Appearance**, sélectionnez **Dark**
3. **Cliquez** sur "Preview Theme" pour voir le résultat
4. **Cliquez** sur "Save"

🌙 **Magnifique !** Vos yeux vous remercient.

---

## 🔍 Conseils pour Bien Commencer

### Navigation Rapide

- **Catégories** (panneau gauche) : Cliquez pour filtrer par type (AD & GPO, Network & DNS, Logs, etc.)
- **📋 All Commands** : Voir toutes les commandes disponibles
- **⭐ Favorites** : Accès rapide à vos commandes préférées

### Recherche Intelligente

La recherche TwinShell est **très tolérante** :

| Vous tapez | Trouve |
|------------|--------|
| `reseau` (sans accent) | "Configuration Réseau" |
| `Get Service` (sans tiret) | "Get-Service" |
| `AD user` (multi-mots) | "List AD Users", "Get AD User Info" |
| `serviec` (faute de frappe) | "service" (fuzzy matching) |

### Filtres Utiles

- **Platform** : Windows, Linux ou Both
- **Level** :
  - Info (bleu) = Lecture seule, sans danger
  - Run (orange) = Exécution, attention
  - Dangerous (rouge) = Modification système, danger !

---

## ⌨️ Raccourcis à Connaître

| Raccourci | Action |
|-----------|--------|
| `Ctrl+,` | Paramètres |
| `Ctrl+M` | Gérer les catégories |
| `F5` | Actualiser |
| `F1` | Aide |

---

## 📚 Et Maintenant ?

### Explorez les Fonctionnalités Avancées

1. **Historique** : Menu "📜 History" - Consultez toutes vos commandes passées
2. **Export/Import** : Menu "File → Export Configuration" - Sauvegardez vos favoris
3. **Catégories Personnalisées** : `Ctrl+M` - Créez vos propres catégories

### Documentation Complète

- [Guide Utilisateur Complet](UserGuide.md)
- [FAQ - Questions Frequentes](FAQ.md)
- [GitHub TwinShell](https://github.com/VBlackJack/TwinShell)

---

## 🆘 Besoin d'Aide ?

### Problème : L'application ne démarre pas

1. Vérifiez que **.NET 8 Runtime** est installé
   - Téléchargez-le ici : [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Vérifiez que vous avez **Windows 10 ou 11** (64-bit)

### Problème : Je ne trouve pas une commande

1. Vérifiez que vous n'avez pas de **filtres actifs** (Platform/Level)
2. Cliquez sur **"📋 All Commands"** pour voir toutes les commandes
3. Essayez une recherche plus courte (ex: `dns` au lieu de `dns query`)

### Problème : Le thème ne change pas

1. Assurez-vous de cliquer sur **"Save"** dans les paramètres
2. Redémarrez l'application si nécessaire

---

## 💡 Astuce du Jour

**Utilisez le widget des commandes récentes !**

Sur la page d'accueil, TwinShell affiche vos 5 dernières commandes générées.
Cliquez simplement sur une entrée pour la recopier instantanément !

---

## 🎉 Vous êtes Prêt !

Vous avez maintenant les bases pour utiliser TwinShell efficacement.

**Bon travail avec TwinShell !** 🚀

---

[GitHub TwinShell](https://github.com/VBlackJack/TwinShell) | [Guide Utilisateur Complet](UserGuide.md) | [FAQ](FAQ.md)
