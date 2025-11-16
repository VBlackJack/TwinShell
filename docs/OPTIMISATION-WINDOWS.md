# Guide d'optimisation Windows - TwinShell

Guide complet pour optimiser et nettoyer votre installation Windows à l'aide de TwinShell.

## Table des matières

1. [Debloating Windows](#debloating-windows)
2. [Précautions et recommandations](#précautions-et-recommandations)
3. [Rollback et récupération](#rollback-et-récupération)
4. [FAQ et troubleshooting](#faq-et-troubleshooting)

---

## Debloating Windows

Le debloating consiste à supprimer les applications préinstallées et composants Windows non essentiels pour:
- Libérer de l'espace disque
- Améliorer les performances système
- Réduire la consommation de ressources
- Améliorer la confidentialité

### ⚠️ AVERTISSEMENT IMPORTANT

**Les actions de debloating peuvent être irréversibles!**

Avant de procéder:
1. **Créez un point de restauration système**
2. **Sauvegardez vos données importantes**
3. **Documentez vos actions pour pouvoir les annuler si nécessaire**
4. **Testez d'abord sur une machine virtuelle ou de test**

### Catégories d'actions disponibles

TwinShell propose 22 actions de debloating organisées en 4 catégories:

#### 1. 🧹 Bloatware Tiers (3 actions)

Actions pour supprimer les applications tierces préinstallées.

| Action ID | Description | Level | Notes |
|-----------|-------------|-------|-------|
| WIN-DEBLOAT-001 | Supprimer tous les bloatwares tiers | Run | Candy Crush, Spotify, Disney+, Netflix |
| WIN-DEBLOAT-002 | Supprimer les extensions média | Run | HEIF, VP9, WebP, AV1 codecs |
| WIN-DEBLOAT-003 | Lister les applications tierces | Info | Commande d'information uniquement |

**Recommandation:** Commencez par WIN-DEBLOAT-003 pour identifier les apps installées avant de supprimer.

#### 2. 📱 Applications Microsoft (5 actions)

Actions pour supprimer les applications Microsoft non essentielles.

| Action ID | Description | Level | Apps concernées |
|-----------|-------------|-------|-----------------|
| WIN-DEBLOAT-101 | Supprimer les apps Microsoft inutiles | Run | 38+ apps (3D Builder, Alarms, BingNews, etc.) |
| WIN-DEBLOAT-102 | Supprimer uniquement les apps de jeux | Run | Solitaire, Candy Crush, Xbox Game Bar |
| WIN-DEBLOAT-103 | Supprimer les apps de communication | Run | Skype, People, Mail, Calendar |
| WIN-DEBLOAT-104 | Supprimer les apps météo/actualités | Run | Météo, Actualités, Sports, Finance |
| WIN-DEBLOAT-105 | Liste personnalisée d'apps | Run | Spécifier votre propre liste |

**Recommandation:** Utilisez WIN-DEBLOAT-102/103/104 pour une suppression ciblée, ou WIN-DEBLOAT-101 pour un nettoyage complet.

#### 3. ⚠️ Composants Système (6 actions) - TOUTES DANGEROUS

**ATTENTION:** Ces actions sont irréversibles et peuvent affecter les fonctionnalités Windows!

| Action ID | Description | Impact | Conséquences |
|-----------|-------------|--------|--------------|
| WIN-DEBLOAT-201 | Désinstaller Microsoft Store | 🔴 Critique | Impossible d'installer des apps du Store |
| WIN-DEBLOAT-202 | Désinstaller OneDrive (complet) | 🔴 Critique | Perte de la synchro cloud, nettoyage registre |
| WIN-DEBLOAT-203 | Désinstaller Microsoft Edge | 🔴 Critique | Certaines fonctions Windows 11 affectées |
| WIN-DEBLOAT-204 | Désinstaller Copilot | 🟡 Modéré | Perte de l'assistant IA Windows |
| WIN-DEBLOAT-205 | Supprimer Xbox (tous composants) | 🟡 Modéré | Perte Game Bar et services Xbox |
| WIN-DEBLOAT-206 | Supprimer Widgets | 🟡 Modéré | Retire les Widgets de la barre des tâches |

**Recommandation:**
- **NE PAS UTILISER** sans sauvegarde complète du système
- **Tester d'abord** sur une VM ou machine de test
- **Créer un point de restauration** avant chaque action
- **Documenter** chaque action effectuée

#### 4. ⚙️ Fonctionnalités Windows (4 actions)

Actions pour désactiver des fonctionnalités Windows optionnelles.

| Action ID | Description | Level | Impact |
|-----------|-------------|-------|--------|
| WIN-DEBLOAT-301 | Désactiver Consumer Features | Run | Bloque l'installation auto d'apps suggérées |
| WIN-DEBLOAT-302 | Désactiver Recall | Run | Désactive l'enregistrement d'activité IA |
| WIN-DEBLOAT-303 | Désactiver Internet Explorer | Run | Désactive IE11 (legacy) |
| WIN-DEBLOAT-304 | Désactiver Hyper-V | Run | Désactive la virtualisation |

**Recommandation:** WIN-DEBLOAT-301 est fortement recommandé pour éviter la réinstallation de bloatware.

#### 5. 🌐 Optimisation Edge (4 actions)

Actions pour optimiser Microsoft Edge (si vous le conservez).

| Action ID | Description | Bénéfice |
|-----------|-------------|----------|
| WIN-DEBLOAT-401 | Désactiver les recommandations Edge | Moins de distractions |
| WIN-DEBLOAT-402 | Désactiver le shopping assistant | Pas de notifications de coupons |
| WIN-DEBLOAT-403 | Désactiver la télémétrie Edge | Amélioration de la confidentialité |
| WIN-DEBLOAT-404 | Désactiver le crypto wallet | Désactive les fonctionnalités Web3 |

**Recommandation:** Appliquez toutes les actions Edge si vous utilisez Edge mais souhaitez une expérience plus "propre".

---

## Précautions et recommandations

### Avant de commencer

#### 1. Créer un point de restauration système

```powershell
# Via TwinShell ou manuellement
Enable-ComputerRestore -Drive "C:\"
Checkpoint-Computer -Description "Avant debloating TwinShell" -RestorePointType "MODIFY_SETTINGS"
```

#### 2. Sauvegarder le registre

Les actions de debloating modifient le registre Windows. Sauvegardez-le:

```powershell
# Exporter le registre complet
reg export HKLM C:\Backup\HKLM_backup.reg /y
reg export HKCU C:\Backup\HKCU_backup.reg /y
```

#### 3. Documenter votre configuration

Avant de supprimer des apps, listez-les:

```powershell
# Utiliser WIN-DEBLOAT-003
Get-AppxPackage | Select-Object Name,Version,Publisher | Export-Csv C:\Backup\installed_apps.csv
```

### Ordre recommandé d'exécution

Pour un debloating complet et sécurisé:

1. **Information** → WIN-DEBLOAT-003 (lister les apps tierces)
2. **Bloatware tiers** → WIN-DEBLOAT-001 (supprimer bloatware)
3. **Apps Microsoft** → WIN-DEBLOAT-101/102/103/104 (au choix)
4. **Fonctionnalités** → WIN-DEBLOAT-301 (désactiver Consumer Features)
5. **Optimisation Edge** → WIN-DEBLOAT-401/402/403/404 (si Edge conservé)
6. **Composants système** → WIN-DEBLOAT-201-206 (**DANGER - uniquement si nécessaire**)

### Profils d'utilisation recommandés

#### Profil "Sécurisé" (Recommandé pour la plupart des utilisateurs)

Actions à exécuter:
- WIN-DEBLOAT-001 (Bloatware tiers)
- WIN-DEBLOAT-102 (Apps de jeux)
- WIN-DEBLOAT-104 (Apps météo/actualités)
- WIN-DEBLOAT-301 (Consumer Features)
- WIN-DEBLOAT-401/402/403 (Optimisation Edge)

Gain estimé: 2-5 GB d'espace, amélioration modérée des performances.

#### Profil "Avancé" (Pour utilisateurs expérimentés)

Actions supplémentaires:
- WIN-DEBLOAT-101 (Toutes les apps Microsoft)
- WIN-DEBLOAT-302 (Recall)
- WIN-DEBLOAT-303 (Internet Explorer)
- WIN-DEBLOAT-206 (Widgets)

Gain estimé: 5-10 GB d'espace, amélioration significative des performances.

#### Profil "Extrême" (⚠️ DANGER - Experts uniquement)

Actions supplémentaires:
- WIN-DEBLOAT-202 (OneDrive)
- WIN-DEBLOAT-204 (Copilot)
- WIN-DEBLOAT-205 (Xbox)

**NE PAS UTILISER:**
- WIN-DEBLOAT-201 (Microsoft Store) - Très difficile à restaurer
- WIN-DEBLOAT-203 (Microsoft Edge) - Peut casser Windows 11

Gain estimé: 10-15 GB d'espace, performances maximales, **mais risque élevé**.

---

## Rollback et récupération

### Méthodes de récupération par ordre de préférence

#### 1. Restauration système (Recommandé)

Si vous avez créé un point de restauration:

```powershell
# Lister les points de restauration
Get-ComputerRestorePoint

# Restaurer (via l'interface graphique)
rstrui.exe
```

#### 2. Réinstallation via Microsoft Store

Pour les apps Microsoft supprimées:

```powershell
# Réinstaller une app spécifique (si Store disponible)
Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*AppName*"} | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml"}
```

#### 3. Réinstallation de composants critiques

##### Microsoft Store

```powershell
# Via PowerShell (nécessite connexion Internet)
wsreset.exe
Get-AppxPackage *WindowsStore* -AllUsers | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml"}
```

##### OneDrive

```powershell
# Télécharger et réinstaller OneDrive
Start-Process "https://go.microsoft.com/fwlink/?linkid=844652"
```

##### Microsoft Edge

```powershell
# Télécharger Edge depuis le site officiel
Start-Process "https://www.microsoft.com/edge"
```

#### 4. Annulation de modifications registre

Si vous avez sauvegardé le registre:

```powershell
# Restaurer une clé spécifique
reg import C:\Backup\HKLM_backup.reg
reg import C:\Backup\HKCU_backup.reg
```

#### 5. Réparation Windows (Dernier recours)

Si le système est instable:

```powershell
# Scan et réparation des fichiers système
sfc /scannow
DISM /Online /Cleanup-Image /RestoreHealth
```

Ou via les Paramètres Windows:
- Paramètres → Mise à jour et sécurité → Récupération → Réinitialiser ce PC

---

## FAQ et troubleshooting

### Questions fréquentes

#### Q: Quelles actions sont réversibles?

**R:** Les actions de niveau "Info" et "Run" sont généralement réversibles:
- Les apps peuvent être réinstallées via le Microsoft Store
- Les paramètres de registre peuvent être restaurés
- Les fonctionnalités Windows peuvent être réactivées

Les actions "Dangerous" (niveau 2) sont **difficilement** réversibles.

#### Q: Puis-je réinstaller les apps supprimées?

**R:** Oui, si le Microsoft Store est encore installé:
- Ouvrez le Microsoft Store
- Recherchez l'application
- Cliquez sur "Installer"

**Attention:** WIN-DEBLOAT-201 supprime le Store lui-même!

#### Q: Les actions de debloating affectent-elles les mises à jour Windows?

**R:** Non, les mises à jour Windows fonctionnent normalement. Cependant:
- Certaines apps supprimées peuvent être réinstallées lors de mises à jour majeures
- Utilisez WIN-DEBLOAT-301 (Consumer Features) pour éviter cela

#### Q: Quelle est la différence entre "supprimer" et "désactiver"?

**R:**
- **Supprimer** (Remove-AppxPackage): Désinstalle complètement l'application
- **Désactiver** (registre): L'application reste installée mais inactive

Les désactivations sont plus faciles à annuler.

#### Q: Combien d'espace disque puis-je libérer?

**R:** Dépend du profil choisi:
- Profil "Sécurisé": 2-5 GB
- Profil "Avancé": 5-10 GB
- Profil "Extrême": 10-15 GB

#### Q: Le debloating améliore-t-il vraiment les performances?

**R:** Oui, mais l'impact dépend de votre matériel:
- **Disque:** Moins d'apps = plus d'espace libre
- **RAM:** Moins de services en arrière-plan
- **CPU:** Moins de processus actifs
- **Réseau:** Moins de télémétrie et synchronisation

Sur un PC moderne, l'amélioration est modérée. Sur un PC ancien, elle peut être significative.

### Problèmes courants

#### Problème: "Accès refusé" lors de l'exécution

**Solution:**
```powershell
# Exécuter PowerShell en tant qu'administrateur
# Clic droit sur l'icône PowerShell → "Exécuter en tant qu'administrateur"
```

#### Problème: L'application n'a pas été supprimée

**Causes possibles:**
1. L'app est en cours d'exécution
2. L'app est protégée par le système
3. Permissions insuffisantes

**Solution:**
```powershell
# 1. Fermer tous les processus liés
Get-Process | Where-Object {$_.Name -like "*AppName*"} | Stop-Process -Force

# 2. Essayer avec -AllUsers
Get-AppxPackage *AppName* -AllUsers | Remove-AppxPackage

# 3. Utiliser l'option provisioned (empêche la réinstallation)
Get-AppxProvisionedPackage -Online | Where-Object {$_.DisplayName -like "*AppName*"} | Remove-AppxProvisionedPackage -Online
```

#### Problème: Le Microsoft Store ne fonctionne plus

**Solution:**
```powershell
# Réinitialiser le cache du Store
wsreset.exe

# Réenregistrer le Store
Get-AppxPackage *WindowsStore* | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppxManifest.xml"}
```

#### Problème: OneDrive continue de démarrer

**Solution:**
```powershell
# Désactiver OneDrive au démarrage
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v OneDrive /f
reg delete "HKLM\Software\Microsoft\Windows\CurrentVersion\Run" /v OneDrive /f

# Désactiver OneDrive dans l'Explorateur
reg add "HKCR\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}" /v System.IsPinnedToNameSpaceTree /t REG_DWORD /d 0 /f
```

#### Problème: Windows 11 devient instable après suppression d'Edge

**Solution:**
⚠️ **C'est pourquoi WIN-DEBLOAT-203 est DANGEROUS!**

Windows 11 utilise Edge pour certaines fonctionnalités système. Options:
1. Restaurer le point de restauration système
2. Réinstaller Edge depuis le site officiel
3. Utiliser les actions WIN-DEBLOAT-401-404 au lieu de supprimer Edge

#### Problème: Les widgets reviennent après chaque mise à jour

**Solution:**
```powershell
# Désactiver via GPO (plus persistant)
reg add "HKLM\Software\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f

# Désactiver le service
Get-Service -Name "WebExperienceHostPlugin" | Set-Service -StartupType Disabled
```

### Obtenir de l'aide

Si vous rencontrez des problèmes:

1. **Vérifiez les logs d'erreur:**
   - Event Viewer → Windows Logs → Application
   - Recherchez les erreurs liées à l'app supprimée

2. **Consultez la documentation Microsoft:**
   - https://learn.microsoft.com/en-us/powershell/module/appx/

3. **Créez une issue sur GitHub:**
   - https://github.com/VBlackJack/TwinShell/issues
   - Incluez:
     - Action exécutée (WIN-DEBLOAT-XXX)
     - Message d'erreur complet
     - Version de Windows (Win 10/11, build)
     - Logs d'erreur si disponibles

---

## Ressources supplémentaires

### Scripts de référence

Les scripts PowerShell utilisés par TwinShell sont disponibles dans:
- `/data/seed/initial-actions.json` - Définitions des actions
- Documentation Microsoft sur Remove-AppxPackage
- Documentation Microsoft sur les GPO Windows

### Outils complémentaires

Pour un debloating plus avancé, considérez:
- **Chris Titus Tech Windows Utility** - Interface graphique pour debloating
- **O&O ShutUp10++** - Désactivation de fonctionnalités Windows
- **BleachBit** - Nettoyage de fichiers temporaires

**Note:** TwinShell se concentre sur les actions via PowerShell pour une traçabilité maximale.

### Bonnes pratiques

1. **Testez toujours sur une VM ou machine de test d'abord**
2. **Documentez chaque action effectuée**
3. **Créez un point de restauration avant chaque session de debloating**
4. **Sauvegardez vos données importantes**
5. **Lisez les notes de chaque action avant de l'exécuter**
6. **Ne supprimez pas ce que vous ne comprenez pas**
7. **Privilégiez la désactivation à la suppression quand c'est possible**

---

## Changelog

### Sprint 6 - Novembre 2025
- Ajout de 22 actions de debloating Windows
- 4 catégories: Bloatware, Apps Microsoft, Composants système, Fonctionnalités
- Actions de niveau Dangerous pour composants critiques
- Documentation complète avec FAQ et troubleshooting

---

**Dernière mise à jour:** Novembre 2025
**Version:** 1.0
**Auteur:** TwinShell Team
