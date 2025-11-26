# Stratégie Cross-Platform pour TwinShell

**Date**: 2025-11-19
**Auteur**: Architecture Review
**Statut**: Recommandation

## 1. État actuel - Limitations

### 1.1 Architecture actuelle
- **Framework UI**: WPF (Windows Presentation Foundation)
- **Runtime**: .NET 8.0-windows
- **Installeur**: Inno Setup 6 (`.iss`)
- **Plateforme cible**: Windows 10+ x64 uniquement

### 1.2 Problématiques identifiées
✅ **Forces**:
- .NET 8 est intrinsèquement cross-platform
- Architecture en couches bien séparée (Core/Persistence/Infrastructure)
- Logique métier indépendante de l'UI (MVVM strict)

⚠️ **Limitations**:
- **WPF est Windows-only** (pas de portage Linux/macOS possible)
- **Inno Setup** ne génère que des installeurs Windows
- Les commandes PowerShell sont majoritairement Windows-centric
- Dépendance à `Environment.SpecialFolder` Windows-specific

## 2. Analyse technique - Inno Setup vs. Alternatives

### 2.1 Inno Setup actuel (build-installer.iss)
```ini
[Setup]
MinVersion=10.0                          # Windows 10+ requis
ArchitecturesAllowed=x64                # x64 uniquement
OutputBaseFilename=TwinShell-v{#MyAppVersion}-Setup-win-x64
```

**Limitations**:
- Aucun support Linux/macOS
- Nécessite compilation Windows pour génération du setup
- Pas de packaging portable (ZIP/tarball)

### 2.2 Alternatives modernes

#### Option A: `dotnet publish` (déjà disponible)
```bash
# Windows (actuel)
dotnet publish -c Release -r win-x64 --self-contained

# Linux (potentiel futur)
dotnet publish -c Release -r linux-x64 --self-contained

# macOS (potentiel futur)
dotnet publish -c Release -r osx-x64 --self-contained
```

**Avantages**:
- ✅ Génère des binaires autonomes (inclut .NET runtime)
- ✅ Portable (ZIP pour distribution)
- ✅ Cross-platform ready

**Limitations**:
- ❌ WPF ne compilera pas pour linux-x64/osx-x64
- ❌ Nécessite migration UI vers framework cross-platform

## 3. Recommandations stratégiques

### 3.1 PHASE 1 - Court terme (IMMÉDIAT)
**Objectif**: Améliorer la distribution Windows sans réécriture

#### Actions recommandées:
1. **Conserver Inno Setup** pour l'installeur Windows officiel
2. **Ajouter distribution portable** (ZIP) via GitHub Actions
   ```yaml
   # Ajout au workflow CI/CD
   - name: Create portable package
     run: |
       dotnet publish src/TwinShell.App/TwinShell.App.csproj `
         -c Release -r win-x64 --self-contained `
         -o ./portable
       Compress-Archive -Path ./portable/* `
         -DestinationPath TwinShell-v$VERSION-Portable-win-x64.zip
   ```
3. **Documenter** les Runtime Identifiers supportés dans README

#### Bénéfices:
- Distribution sans installation (USB, réseaux restreints)
- Test facile sans privilèges admin
- Préparation infrastructure pour futurs RID

### 3.2 PHASE 2 - Moyen terme (3-6 mois)
**Objectif**: Refactoring UI pour cross-platform readiness

#### Migration UI recommandée: **Avalonia UI**

**Pourquoi Avalonia ?**
- ✅ Syntaxe quasi-identique à WPF (XAML compatible)
- ✅ Supporte Windows, Linux, macOS, WebAssembly
- ✅ MVVM natif (CommunityToolkit.Mvvm compatible)
- ✅ Migration progressive possible (Views une par une)
- ✅ Mature et production-ready (GitHub Desktop l'utilise)

**Plan de migration**:
```
TwinShell/
├── src/
│   ├── TwinShell.Core/           # ✅ Aucune modification (déjà cross-platform)
│   ├── TwinShell.Persistence/    # ✅ SQLite fonctionne partout
│   ├── TwinShell.Infrastructure/ # ⚠️ À adapter (ClipboardService, etc.)
│   ├── TwinShell.App.WPF/        # 🔄 Renommer projet actuel
│   └── TwinShell.App.Avalonia/   # 🆕 Nouveau projet cross-platform
```

**Effort estimé**: 40-60 heures (migration UI uniquement)

#### Packaging multi-plateforme:
- **Windows**: Inno Setup + MSIX (Microsoft Store)
- **Linux**: AppImage, .deb (Debian/Ubuntu), .rpm (Fedora/RHEL)
- **macOS**: .dmg + éventuel notarization Apple

### 3.3 PHASE 3 - Long terme (6-12 mois)
**Objectif**: Support natif Bash/zsh pour Linux/macOS

#### Évolutions fonctionnelles:
1. **Adapter le catalogue de commandes**:
   - PowerShell → Disponible aussi sur Linux/macOS (pwsh)
   - Ajouter équivalents Bash/zsh pour commandes système
   - Catégorie "Cross-platform" pour commandes universelles

2. **Storage paths cross-platform**:
   ```csharp
   // Actuel (Windows-specific)
   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
   // → C:\Users\<user>\AppData\Local\TwinShell

   // Cross-platform (à implémenter)
   var appDataPath = OperatingSystem.IsWindows()
       ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
       : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
   // Windows: C:\Users\<user>\AppData\Local\TwinShell
   // Linux:   /home/<user>/.local/share/TwinShell
   // macOS:   /Users/<user>/.local/share/TwinShell
   ```

3. **CI/CD multi-OS**:
   ```yaml
   strategy:
     matrix:
       os: [windows-latest, ubuntu-latest, macos-latest]
       include:
         - os: windows-latest
           rid: win-x64
         - os: ubuntu-latest
           rid: linux-x64
         - os: macos-latest
           rid: osx-x64
   ```

## 4. Décision immédiate recommandée

### ✅ ACTION PRIORITAIRE (à implémenter maintenant):
Modifier le workflow GitHub Actions pour générer **2 artefacts Windows**:
1. **Installeur** (via Inno Setup) → pour distribution officielle
2. **Portable** (ZIP) → pour utilisateurs avancés, tests, USB

### Code à ajouter au `.github/workflows/dotnet-desktop.yml`:

```yaml
    # Après l'étape "Publish application"
    - name: Create portable ZIP package
      run: Compress-Archive -Path ./publish/* -DestinationPath ./TwinShell-v${{ github.ref_name }}-Portable-win-x64.zip

    - name: Upload portable package
      uses: actions/upload-artifact@v4
      with:
        name: TwinShell-Portable-win-x64
        path: ./TwinShell-v*.zip
        retention-days: 30

    # FUTUR: Génération de l'installeur Inno Setup (nécessite setup ISCC.exe)
    # - name: Build Inno Setup installer
    #   run: iscc.exe build-installer.iss
    #   if: startsWith(github.ref, 'refs/tags/v')
```

## 5. Bénéfices attendus

### Court terme (Phase 1):
- ✅ Distribution portable sans installation
- ✅ Infrastructure CI/CD complète
- ✅ Préparation pour futurs RID

### Moyen terme (Phase 2):
- ✅ Support Linux/macOS (millions d'utilisateurs potentiels)
- ✅ Positionnement "Universal Command Library"
- ✅ Communauté open-source élargie

### Long terme (Phase 3):
- ✅ Catalogue unifié PowerShell/Bash/Zsh
- ✅ Leadership sur le marché des outils DevOps multi-OS
- ✅ Adoption entreprise (environnements hybrides Windows/Linux)

## 6. Risques et mitigation

| Risque | Impact | Probabilité | Mitigation |
|--------|--------|-------------|------------|
| Migration Avalonia échoue | Élevé | Faible | POC sur 1 vue isolée avant full migration |
| Perte fonctionnalités WPF | Moyen | Moyen | Audit complet features WPF vs. Avalonia |
| Fragmentation codebase | Moyen | Élevé | Maintenir architecture en couches stricte |
| Commandes Linux non pertinentes | Faible | Faible | Curration communautaire, votes utilisateurs |

## 7. Conclusion

**Recommandation finale**: Implémenter **Phase 1 immédiatement** (portable ZIP), planifier **Phase 2** (Avalonia) dans 3-6 mois si adoption Windows confirmée.

TwinShell possède déjà une architecture saine pour le cross-platform. La migration UI vers Avalonia est le seul verrou technique majeur, et elle est **faisable et maîtrisée** avec un effort raisonnable.

---

**Prochaines actions**:
- [ ] Ajouter génération ZIP portable au workflow CI/CD
- [ ] Créer POC Avalonia (MainWindow uniquement)
- [ ] Documenter les commandes PowerShell portables Linux/macOS
- [ ] Analyser effort migration complète Avalonia
