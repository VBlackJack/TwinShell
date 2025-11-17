# THEME SYSTEM - SOLUTION COMPLÈTE ET PLAN D'IMPLÉMENTATION

**Date:** 2025-11-17
**Application:** TwinShell WPF .NET 8.0
**Branche:** claude/fix-theme-system-011a8DQBugr865XbyEqQ9oue

---

## TABLE DES MATIÈRES

1. [Architecture de la Solution](#architecture-de-la-solution)
2. [Modifications Requises](#modifications-requises)
3. [Ordre d'Exécution Optimal](#ordre-dexécution-optimal)
4. [Plan d'Implémentation Détaillé](#plan-dimplémentation-détaillé)
5. [Régles de Remplacement](#régles-de-remplacement)
6. [Code Exemples](#code-exemples)
7. [Plan de Tests](#plan-de-tests)

---

## ARCHITECTURE DE LA SOLUTION

### Principes Fondamentaux

1. **Séparation des Responsabilités**
   - `DesignTokens.xaml`: Ressources invariantes (spacing, radius, etc.)
   - `LightTheme.xaml` / `DarkTheme.xaml`: Ressources variant avec le thème (couleurs)
   - `ThemeService`: Gestion du chargement dynamique des thèmes
   - Vues: Consommation des ressources via DynamicResource

2. **Chargement Dynamique**
   - Aucun thème chargé en dur dans App.xaml
   - ThemeService charge le thème approprié au démarrage
   - Les ressources se mettent à jour automatiquement via DynamicResource

3. **Timing Optimal**
   - Thème appliqué AVANT la création des contrôles
   - Settings chargés AVANT l'application du thème
   - Window créée APRÈS l'application du thème

---

## MODIFICATIONS REQUISES

### 1. App.xaml - Retrait du Thème Codé en Dur

**Fichier:** `src/TwinShell.App/App.xaml`

#### AVANT (Problématique)
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/DesignTokens.xaml"/>
            <ResourceDictionary Source="Resources/FluentEffects.xaml"/>
            <ResourceDictionary Source="Resources/Styles.xaml"/>
            <ResourceDictionary Source="Resources/Animations.xaml"/>
            <!-- ❌ PROBLÈME: Thème codé en dur -->
            <ResourceDictionary Source="Themes/LightTheme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

#### APRÈS (Solution)
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- 1. Design Tokens (fondation - ne change jamais) -->
            <ResourceDictionary Source="Resources/DesignTokens.xaml"/>

            <!-- 2. Effets Fluent (ne change jamais) -->
            <ResourceDictionary Source="Resources/FluentEffects.xaml"/>

            <!-- 3. LE THÈME SERA CHARGÉ ICI DYNAMIQUEMENT PAR ThemeService -->
            <!--    Ne RIEN mettre ici - ThemeService gère tout -->

            <!-- 4. Styles et animations (après le thème pour pouvoir référencer les couleurs) -->
            <ResourceDictionary Source="Resources/Styles.xaml"/>
            <ResourceDictionary Source="Resources/Animations.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**Raison:**
- Les Styles.xaml et Animations.xaml peuvent référencer des brushes de thème
- Ils doivent être chargés APRÈS le thème
- Mais le chargement est géré dynamiquement par ThemeService

---

### 2. App.xaml.cs - Activation de l'Initialisation du Thème

**Fichier:** `src/TwinShell.App/App.xaml.cs`

#### AVANT (Problématique)
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // ... autres initialisations ...

    // BUGFIX: Skip async theme initialization during startup - will be done after window is shown
    //LogInfo("Initializing theme...");
    //InitializeThemeAsync().GetAwaiter().GetResult();
    //LogInfo("Theme initialized");

    LogInfo("Initializing database...");
    InitializeDatabaseAsync().GetAwaiter().GetResult();

    // Create and show main window
    var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
    mainWindow.Show();
}
```

#### APRÈS (Solution - Option A: Synchrone et Simple)
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Add global exception handlers
    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    DispatcherUnhandledException += OnDispatcherUnhandledException;

    try
    {
        LogInfo("Starting application...");

        // 1. Configure and build services
        var services = new ServiceCollection();
        LogInfo("Configuring services...");
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        LogInfo("Services configured");

        // 2. Initialize theme and localization (SYNCHRONOUSLY)
        LogInfo("Initializing theme and localization...");
        InitializeThemeAndLocalization();
        LogInfo("Theme and localization initialized");

        // 3. Initialize database
        LogInfo("Initializing database...");
        InitializeDatabaseAsync().GetAwaiter().GetResult();
        LogInfo("Database initialized");

        // 4. Create and show main window
        LogInfo("Creating main window...");
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        LogInfo("Main window created");

        LogInfo("Showing main window...");
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Show();
        mainWindow.Activate();
        mainWindow.Topmost = true;
        mainWindow.Topmost = false;
        LogInfo("Main window shown!");
    }
    catch (Exception ex)
    {
        LogError("Startup error", ex);
        MessageBox.Show("Une erreur s'est produite au démarrage de l'application.\n\nVeuillez consulter le fichier startup-error.log pour plus de détails.",
            "Erreur de démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }
}

/// <summary>
/// Initializes theme and localization synchronously.
/// Called BEFORE window creation to ensure proper theme application.
/// </summary>
private void InitializeThemeAndLocalization()
{
    if (_serviceProvider == null)
    {
        throw new InvalidOperationException("Service provider has not been initialized");
    }

    var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
    var themeService = _serviceProvider.GetRequiredService<IThemeService>();
    var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();

    // Load user settings synchronously
    var settings = settingsService.LoadSettingsAsync().GetAwaiter().GetResult();

    // Apply the saved theme SYNCHRONOUSLY
    LogInfo($"Applying theme: {settings.Theme}");
    themeService.ApplyTheme(settings.Theme);
    LogInfo($"Theme applied: {settings.Theme}");

    // Apply the saved language
    try
    {
        LogInfo($"Applying language: {settings.CultureCode}");
        localizationService.ChangeLanguage(settings.CultureCode);
        LogInfo($"Language applied: {settings.CultureCode}");
    }
    catch (Exception ex)
    {
        LogError("Failed to apply language, falling back to French", ex);
        // Fallback to French if culture is invalid
        localizationService.ChangeLanguage("fr");
    }
}
```

**Alternative - Option B: Async avec Dispatcher**
```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // ... handler setup ...

    try
    {
        // 1. Services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // 2. Theme AVANT database (plus rapide)
        await InitializeThemeAndLocalizationAsync();

        // 3. Database
        await InitializeDatabaseAsync();

        // 4. Window (après que tout soit prêt)
        await Dispatcher.InvokeAsync(() =>
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        });
    }
    catch (Exception ex)
    {
        LogError("Startup error", ex);
        Shutdown(1);
    }
}
```

**Recommandation:** Utiliser **Option A (Synchrone)** car:
- Plus simple et plus robuste
- Évite les problèmes de threading
- Le chargement du thème est quasi-instantané
- Pas de risque de race conditions

---

### 3. ThemeService.cs - Améliorations (Optionnelles)

**Fichier:** `src/TwinShell.Core/Services/ThemeService.cs`

#### Améliorations Suggérées

```csharp
public class ThemeService : IThemeService, IDisposable
{
    private Theme _currentTheme = Theme.Light;
    private const string LightThemeUri = "/TwinShell.App;component/Themes/LightTheme.xaml";
    private const string DarkThemeUri = "/TwinShell.App;component/Themes/DarkTheme.xaml";
    private readonly ILogger<ThemeService>? _logger;  // ✨ AJOUT: Logger

    public ThemeService(ILogger<ThemeService>? logger = null)  // ✨ AJOUT: Logger injection
    {
        _logger = logger;

        if (OperatingSystem.IsWindows())
        {
            SystemEvents.UserPreferenceChanged += OnWindowsThemeChanged;
        }
    }

    public void ApplyTheme(Theme theme)
    {
        try  // ✨ AJOUT: Try-catch pour robustesse
        {
            var effectiveTheme = GetEffectiveTheme(theme);
            _currentTheme = theme;

            _logger?.LogInformation($"Applying theme: {theme} (effective: {effectiveTheme})");  // ✨ AJOUT: Log

            // ✨ AJOUT: Validation
            if (Application.Current == null)
            {
                _logger?.LogError("Application.Current is null - cannot apply theme");
                throw new InvalidOperationException("Application.Current is null");
            }

            // Remove existing theme ResourceDictionaries
            RemoveExistingTheme();

            // Get the appropriate theme URI
            var themeUri = effectiveTheme == Theme.Dark ? DarkThemeUri : LightThemeUri;
            _logger?.LogDebug($"Loading theme from: {themeUri}");  // ✨ AJOUT: Log

            // Load and merge the new theme ResourceDictionary
            var themeResourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Add(themeResourceDictionary);
            _logger?.LogInformation($"Theme applied successfully: {theme}");  // ✨ AJOUT: Log
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to apply theme: {theme}");  // ✨ AJOUT: Log
            throw;
        }
    }

    private void RemoveExistingTheme()
    {
        var themesToRemove = Application.Current.Resources.MergedDictionaries
            .Where(d => d.Source != null &&
                       (d.Source.OriginalString.Contains("/Themes/LightTheme.xaml") ||
                        d.Source.OriginalString.Contains("/Themes/DarkTheme.xaml")))
            .ToList();

        _logger?.LogDebug($"Removing {themesToRemove.Count} existing theme dictionaries");  // ✨ AJOUT: Log

        foreach (var theme in themesToRemove)
        {
            Application.Current.Resources.MergedDictionaries.Remove(theme);
        }
    }

    // ... reste du code inchangé ...
}
```

**Modifications à ConfigureServices:**
```csharp
// Dans App.xaml.cs ConfigureServices()
services.AddSingleton<IThemeService, ThemeService>();  // Logger sera injecté automatiquement
```

---

### 4. Vues XAML - Conversion StaticResource → DynamicResource

**Objectif:** Remplacer 655 instances de StaticResource pour les Brushes

#### Règles de Remplacement

**✅ À CONVERTIR (Brushes de couleur):**
```
BackgroundBrush
SurfaceBrush
SurfaceElevatedBrush
SurfaceSunkenBrush
SurfaceOverlayBrush
HoverBackgroundBrush
SelectedBackgroundBrush
ActiveBackgroundBrush
DisabledBackgroundBrush
BorderBrush
BorderHoverBrush
FocusBorderBrush
DividerBrush
TextPrimaryBrush
TextSecondaryBrush
TextTertiaryBrush
TextOnPrimaryBrush
TextDisabledBrush
PrimaryBrush
PrimaryHoverBrush
PrimaryPressedBrush
PrimaryLightBrush
PrimaryDarkBrush
SecondaryBrush
SecondaryHoverBrush
SecondaryLightBrush
SuccessBrush
SuccessHoverBrush
SuccessBackgroundBrush
SuccessBorderBrush
DangerBrush
DangerHoverBrush
DangerBackgroundBrush
DangerBorderBrush
WarningBrush
WarningHoverBrush
WarningBackgroundBrush
WarningBorderBrush
InfoBrush
InfoHoverBrush
InfoBackgroundBrush
InfoBorderBrush
CodeBackgroundBrush
CodeBorderBrush
CodeTextBrush
PlatformBadgeBackgroundBrush
PlatformBadgeBorderBrush
PlatformBadgeTextBrush
DisabledBrush
DisabledTextBrush
```

**❌ NE PAS CONVERTIR (Ressources invariantes):**
```
Spacing.*
BorderThickness.*
Radius.*
FontSize.*
FontFamily.*
FontWeight.*
LineHeight.*
TouchTarget.*
Elevation.*
Duration.*
Easing.*
Opacity.*
ZIndex.*
IconSize.*
Space.*
```

**❌ NE PAS CONVERTIR (Références de styles):**
```
BasedOn="{StaticResource ...}"
Style="{StaticResource ...}"
Converter="{StaticResource ...}"
```

#### Exemples de Conversion

**MainWindow.xaml**
```xml
<!-- AVANT -->
<Window Background="{StaticResource BackgroundBrush}">
    <Border Background="{StaticResource SurfaceBrush}"
            BorderBrush="{StaticResource BorderBrush}">
        <TextBlock Foreground="{StaticResource TextPrimaryBrush}"/>
    </Border>
</Window>

<!-- APRÈS -->
<Window Background="{DynamicResource BackgroundBrush}">
    <Border Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource BorderBrush}">
        <TextBlock Foreground="{DynamicResource TextPrimaryBrush}"/>
    </Border>
</Window>
```

**Styles dans les vues**
```xml
<!-- AVANT -->
<Style TargetType="ComboBox">
    <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
</Style>

<!-- APRÈS -->
<Style TargetType="ComboBox">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
</Style>
```

**IMPORTANT: NE PAS toucher aux fichiers de thème**
```xml
<!-- LightTheme.xaml et DarkTheme.xaml - GARDER StaticResource -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>  <!-- ✓ OK -->
</Style>
```

---

## ORDRE D'EXÉCUTION OPTIMAL

### Diagramme de Flux

```
┌─────────────────────────────────────┐
│ APPLICATION START                   │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ App.xaml chargé                     │
│ - DesignTokens.xaml ✓               │
│ - FluentEffects.xaml ✓              │
│ - [PAS DE THÈME]                    │
│ - Styles.xaml (après)               │
│ - Animations.xaml (après)           │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ OnStartup() appelé                  │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ 1. ConfigureServices()              │
│    - Enregistrer tous les services  │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ 2. BuildServiceProvider()           │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ 3. InitializeThemeAndLocalization() │
│    a. LoadSettings() ✓              │
│    b. ApplyTheme() ✓                │
│    c. ApplyLanguage() ✓             │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ THEME CHARGÉ DANS                   │
│ Application.Resources               │
│ Merged Dictionaries:                │
│ - DesignTokens.xaml                 │
│ - FluentEffects.xaml                │
│ - [LIGHT ou DARK] ← AJOUTÉ ICI     │
│ - Styles.xaml                       │
│ - Animations.xaml                   │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ 4. InitializeDatabase()             │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ 5. CreateMainWindow()               │
│    Les contrôles utilisent          │
│    DynamicResource et trouvent      │
│    les bonnes couleurs ✓            │
└───────────────┬─────────────────────┘
                │
                ▼
┌─────────────────────────────────────┐
│ 6. Show MainWindow                  │
│    Thème correctement appliqué ✓    │
└─────────────────────────────────────┘
```

---

## PLAN D'IMPLÉMENTATION DÉTAILLÉ

### Étape 1: Correction App.xaml (10 min)

**Fichier:** `src/TwinShell.App/App.xaml`

**Modification:**
```xml
<!-- RETIRER CETTE LIGNE -->
<ResourceDictionary Source="Themes/LightTheme.xaml"/>

<!-- Ajouter un commentaire explicatif -->
<!-- Le thème est chargé dynamiquement par ThemeService dans App.xaml.cs -->
<!-- Ne PAS charger de thème ici -->
```

**Validation:**
- Aucune erreur de compilation
- L'application démarre (mais sans couleurs - normal à ce stade)

---

### Étape 2: Correction App.xaml.cs (30 min)

**Fichier:** `src/TwinShell.App/App.xaml.cs`

**Modifications:**

1. **Décommenter et corriger OnStartup:**
```csharp
// AVANT:
//LogInfo("Initializing theme...");
//InitializeThemeAsync().GetAwaiter().GetResult();
//LogInfo("Theme initialized");

// APRÈS:
LogInfo("Initializing theme and localization...");
InitializeThemeAndLocalization();
LogInfo("Theme and localization initialized");
```

2. **Renommer et simplifier InitializeThemeAsync → InitializeThemeAndLocalization:**
```csharp
private void InitializeThemeAndLocalization()
{
    if (_serviceProvider == null)
    {
        throw new InvalidOperationException("Service provider has not been initialized");
    }

    var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
    var themeService = _serviceProvider.GetRequiredService<IThemeService>();
    var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();

    // Load user settings synchronously
    var settings = settingsService.LoadSettingsAsync().GetAwaiter().GetResult();

    // Apply the saved theme
    LogInfo($"Applying theme: {settings.Theme}");
    themeService.ApplyTheme(settings.Theme);

    // Apply the saved language
    try
    {
        localizationService.ChangeLanguage(settings.CultureCode);
    }
    catch
    {
        // Fallback to French if culture is invalid
        localizationService.ChangeLanguage("fr");
    }
}
```

**Validation:**
- L'application démarre correctement
- Le thème Light est appliqué (mais les contrôles n'ont pas encore les couleurs - besoin de DynamicResource)

---

### Étape 3: Amélioration ThemeService.cs (Optionnel - 20 min)

**Fichier:** `src/TwinShell.Core/Services/ThemeService.cs`

**Modifications:**
- Ajouter injection ILogger
- Ajouter logs dans ApplyTheme()
- Ajouter validation Application.Current != null
- Ajouter logs dans RemoveExistingTheme()

**Validation:**
- Vérifier les logs dans startup.log
- Confirmer que le thème est bien chargé

---

### Étape 4: Conversion StaticResource → DynamicResource (2-3h)

**Approche Recommandée: Automatisation avec Regex**

#### Script PowerShell de Remplacement

```powershell
# Script: ConvertToDynamicResource.ps1

$sourceFolder = "src/TwinShell.App"
$brushesToConvert = @(
    'BackgroundBrush',
    'SurfaceBrush',
    'SurfaceElevatedBrush',
    'SurfaceSunkenBrush',
    'SurfaceOverlayBrush',
    'HoverBackgroundBrush',
    'SelectedBackgroundBrush',
    'ActiveBackgroundBrush',
    'DisabledBackgroundBrush',
    'BorderBrush',
    'BorderHoverBrush',
    'FocusBorderBrush',
    'DividerBrush',
    'TextPrimaryBrush',
    'TextSecondaryBrush',
    'TextTertiaryBrush',
    'TextOnPrimaryBrush',
    'TextDisabledBrush',
    'PrimaryBrush',
    'PrimaryHoverBrush',
    'PrimaryPressedBrush',
    'PrimaryLightBrush',
    'PrimaryDarkBrush',
    'SecondaryBrush',
    'SecondaryHoverBrush',
    'SecondaryLightBrush',
    'SuccessBrush',
    'SuccessHoverBrush',
    'SuccessBackgroundBrush',
    'SuccessBorderBrush',
    'DangerBrush',
    'DangerHoverBrush',
    'DangerBackgroundBrush',
    'DangerBorderBrush',
    'WarningBrush',
    'WarningHoverBrush',
    'WarningBackgroundBrush',
    'WarningBorderBrush',
    'InfoBrush',
    'InfoHoverBrush',
    'InfoBackgroundBrush',
    'InfoBorderBrush',
    'CodeBackgroundBrush',
    'CodeBorderBrush',
    'CodeTextBrush',
    'PlatformBadgeBackgroundBrush',
    'PlatformBadgeBorderBrush',
    'PlatformBadgeTextBrush',
    'DisabledBrush',
    'DisabledTextBrush'
)

# Fichiers à traiter (exclure les thèmes)
$xamlFiles = Get-ChildItem -Path $sourceFolder -Filter "*.xaml" -Recurse |
    Where-Object { $_.FullName -notlike "*\Themes\*" }

$totalReplacements = 0

foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    foreach ($brush in $brushesToConvert) {
        # Pattern: {StaticResource BrushName}
        $pattern = "\{StaticResource\s+$brush\}"
        $replacement = "{DynamicResource $brush}"
        $content = $content -replace $pattern, $replacement
    }

    # Compter les changements
    if ($content -ne $originalContent) {
        $replacements = ([regex]::Matches($originalContent, "\{StaticResource")).Count -
                        ([regex]::Matches($content, "\{StaticResource")).Count
        $totalReplacements += $replacements

        Write-Host "✓ $($file.Name): $replacements remplacements" -ForegroundColor Green

        # Sauvegarder
        Set-Content -Path $file.FullName -Value $content -NoNewline
    }
}

Write-Host "`n✅ Total: $totalReplacements remplacements effectués" -ForegroundColor Cyan
```

**Exécution:**
```powershell
cd /path/to/TwinShell
.\ConvertToDynamicResource.ps1
```

#### Validation Manuelle (IMPORTANTE)

Après le script, **VÉRIFIER MANUELLEMENT** quelques fichiers:

1. **MainWindow.xaml**
   - Chercher `{StaticResource BackgroundBrush}` → Ne devrait plus exister
   - Chercher `{DynamicResource BackgroundBrush}` → Devrait exister
   - Chercher `{StaticResource Spacing.MD}` → **Devrait toujours exister** (correct)

2. **SettingsWindow.xaml**
   - Idem

3. **LightTheme.xaml et DarkTheme.xaml**
   - **NE DOIVENT PAS AVOIR CHANGÉ**
   - Toujours `{StaticResource}` à l'intérieur

**Validation par Build:**
```bash
dotnet build
```
- Aucune erreur XAML
- Warnings potentiels (ignorables si ressources trouvées au runtime)

---

### Étape 5: Tests Manuels (1h)

**Voir section "Plan de Tests" ci-dessous**

---

## CODE EXEMPLES

### Exemple Complet: SettingsWindow.xaml

**AVANT:**
```xml
<Window x:Class="TwinShell.App.Views.SettingsWindow"
        Background="{StaticResource BackgroundBrush}">
    <Window.Resources>
        <Style TargetType="ComboBox">
            <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
            <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
            <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
        </Style>
    </Window.Resources>

    <Border Background="{StaticResource SurfaceBrush}"
            BorderBrush="{StaticResource BorderBrush}">
        <TextBlock Text="Theme"
                   Foreground="{StaticResource TextPrimaryBrush}"/>
    </Border>
</Window>
```

**APRÈS:**
```xml
<Window x:Class="TwinShell.App.Views.SettingsWindow"
        Background="{DynamicResource BackgroundBrush}">
    <Window.Resources>
        <Style TargetType="ComboBox">
            <Setter Property="Background" Value="{DynamicResource SurfaceBrush}"/>
            <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
            <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}"/>
        </Style>
    </Window.Resources>

    <Border Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource BorderBrush}">
        <TextBlock Text="Theme"
                   Foreground="{DynamicResource TextPrimaryBrush}"/>
    </Border>
</Window>
```

---

## PLAN DE TESTS

### Test 1: Démarrage avec Light Theme (Par Défaut)

**Prérequis:** Supprimer le fichier de settings ou s'assurer que Theme = Light

**Steps:**
1. Supprimer `%APPDATA%\TwinShell\settings.json`
2. Démarrer l'application
3. Vérifier visuellement

**Résultats Attendus:**
- ✅ Fond blanc (#F5F5F5)
- ✅ Texte noir (#212121)
- ✅ Boutons bleus (#0067C0)
- ✅ Pas d'erreurs dans startup.log

---

### Test 2: Changement Light → Dark via Settings

**Steps:**
1. Démarrer l'application (Light)
2. Ouvrir Settings
3. Changer Theme de "Light" à "Dark"
4. Cliquer "Save"

**Résultats Attendus:**
- ✅ Changement **IMMÉDIAT** des couleurs
- ✅ Fond sombre (#1E1E1E)
- ✅ Texte clair (#EBEBEB)
- ✅ MainWindow change aussi (DynamicResource fonctionne)
- ✅ Aucune exception

---

### Test 3: Changement Dark → System

**Prérequis:** Windows en mode Light

**Steps:**
1. S'assurer que Windows est en Light mode
2. Démarrer l'application en Dark
3. Changer vers "System"
4. Cliquer "Save"

**Résultats Attendus:**
- ✅ Passage immédiat en Light (suit Windows)
- ✅ Fond blanc

---

### Test 4: System Mode - Réaction au Changement Windows

**Steps:**
1. Mettre l'application en mode "System"
2. SANS fermer l'app, changer le thème Windows (Settings > Colors > Dark/Light)
3. Observer l'application TwinShell

**Résultats Attendus:**
- ✅ L'application change **AUTOMATIQUEMENT** de thème
- ✅ Pas besoin de redémarrer
- ✅ Changement fluide et immédiat

---

### Test 5: Persistance après Redémarrage

**Steps:**
1. Changer le thème vers Dark
2. Sauvegarder
3. Fermer l'application
4. Redémarrer l'application

**Résultats Attendus:**
- ✅ L'application démarre en Dark
- ✅ Pas de flash de Light au démarrage
- ✅ Settings conservés

---

### Test 6: Tous les Contrôles Changent

**Steps:**
1. Ouvrir toutes les fenêtres:
   - MainWindow
   - SettingsWindow
   - CategoryManagementWindow
   - ActionEditorWindow
2. Changer le thème
3. Observer tous les contrôles

**Résultats Attendus:**
- ✅ Tous les TextBox changent
- ✅ Tous les Buttons changent
- ✅ Tous les Borders changent
- ✅ ListBox, ScrollBars, ComboBox, etc.

---

### Test 7: Validation des Logs

**Steps:**
1. Supprimer `startup.log`
2. Démarrer l'application
3. Ouvrir `startup.log`

**Logs Attendus:**
```
[2025-11-17 HH:MM:SS] Starting application...
[2025-11-17 HH:MM:SS] Configuring services...
[2025-11-17 HH:MM:SS] Services configured
[2025-11-17 HH:MM:SS] Initializing theme and localization...
[2025-11-17 HH:MM:SS] Applying theme: Light        ← NOUVEAU
[2025-11-17 HH:MM:SS] Theme and localization initialized
[2025-11-17 HH:MM:SS] Initializing database...
[2025-11-17 HH:MM:SS] Database initialized
[2025-11-17 HH:MM:SS] Creating main window...
[2025-11-17 HH:MM:SS] Main window created
[2025-11-17 HH:MM:SS] Showing main window...
[2025-11-17 HH:MM:SS] Main window shown!
```

---

## CHECKLIST FINALE DE VALIDATION

Avant de considérer la tâche comme terminée:

### Code
- [ ] App.xaml ne contient plus `<ResourceDictionary Source="Themes/LightTheme.xaml"/>`
- [ ] App.xaml.cs appelle `InitializeThemeAndLocalization()`
- [ ] App.xaml.cs appelle le thème AVANT `CreateMainWindow()`
- [ ] ThemeService a des logs (optionnel mais recommandé)
- [ ] Aucune erreur de build
- [ ] Aucun warning XAML critique

### Conversions
- [ ] MainWindow.xaml utilise DynamicResource pour les Brushes
- [ ] SettingsWindow.xaml utilise DynamicResource pour les Brushes
- [ ] Toutes les autres vues utilisent DynamicResource
- [ ] LightTheme.xaml et DarkTheme.xaml **NON MODIFIÉS** (StaticResource OK)
- [ ] DesignTokens.xaml **NON MODIFIÉ** (StaticResource OK)

### Tests
- [ ] Test 1: Démarrage Light ✅
- [ ] Test 2: Changement Light → Dark ✅
- [ ] Test 3: Changement Dark → System ✅
- [ ] Test 4: Réaction au changement Windows ✅
- [ ] Test 5: Persistance après redémarrage ✅
- [ ] Test 6: Tous les contrôles changent ✅
- [ ] Test 7: Logs corrects ✅

### Critères de Succès Utilisateur
- [ ] ✅ L'application démarre en Light par défaut
- [ ] ✅ Le changement Light → Dark fonctionne instantanément
- [ ] ✅ Le changement vers System détecte le thème Windows
- [ ] ✅ Le mode System réagit aux changements Windows
- [ ] ✅ Le thème persiste après redémarrage
- [ ] ✅ TOUS les contrôles changent de couleur
- [ ] ✅ Aucune exception ou erreur dans les logs
- [ ] ✅ Le build réussit sans erreurs

---

## RÉSUMÉ DES FICHIERS MODIFIÉS

### Fichiers Critiques (Obligatoires)

1. `src/TwinShell.App/App.xaml` - Retrait thème codé en dur
2. `src/TwinShell.App/App.xaml.cs` - Activation initialisation thème
3. `src/TwinShell.App/MainWindow.xaml` - StaticResource → DynamicResource
4. `src/TwinShell.App/Views/SettingsWindow.xaml` - StaticResource → DynamicResource
5. `src/TwinShell.App/Views/ActionEditorWindow.xaml` - StaticResource → DynamicResource
6. `src/TwinShell.App/Views/CategoryManagementWindow.xaml` - StaticResource → DynamicResource
7. `src/TwinShell.App/Views/OutputPanel.xaml` - StaticResource → DynamicResource
8. `src/TwinShell.App/Views/HistoryPanel.xaml` - StaticResource → DynamicResource
9. `src/TwinShell.App/Views/PowerShellGalleryPanel.xaml` - StaticResource → DynamicResource
10. `src/TwinShell.App/Views/BatchPanel.xaml` - StaticResource → DynamicResource

### Fichiers Optionnels (Améliorations)

11. `src/TwinShell.Core/Services/ThemeService.cs` - Ajout logs et validation

### Fichiers À NE PAS Modifier

- ❌ `src/TwinShell.App/Themes/LightTheme.xaml`
- ❌ `src/TwinShell.App/Themes/DarkTheme.xaml`
- ❌ `src/TwinShell.App/Resources/DesignTokens.xaml`
- ❌ `src/TwinShell.App/Resources/FluentEffects.xaml`
- ❌ `src/TwinShell.App/Resources/Styles.xaml`
- ❌ `src/TwinShell.App/Resources/Animations.xaml`

---

## CONCLUSION

Cette solution corrige **complètement** le système de thèmes en:

1. **Activant l'initialisation** (actuellement commentée)
2. **Retirant le thème codé en dur** dans App.xaml
3. **Convertissant 655 instances** de StaticResource → DynamicResource

**Résultat:** Un système de thèmes **100% fonctionnel** qui:
- Démarre avec le bon thème
- Change instantanément
- Persiste les préférences
- Réagit au thème Windows (mode System)
- Met à jour TOUS les contrôles

**Effort:** 4-6 heures
**Complexité:** Moyenne (automatisation possible)
**Risque:** Faible (changements ciblés et réversibles)

---

**Ready for Implementation! 🚀**
