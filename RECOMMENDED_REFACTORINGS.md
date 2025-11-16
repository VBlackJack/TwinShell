# REFACTORISATIONS RECOMMANDÉES - TWINSHELL

## 1. CONSTANTES CENTRALISÉES

### Créer: `src/TwinShell.Core/Constants/UIConstants.cs`

```csharp
namespace TwinShell.Core.Constants;

public static class UIConstants
{
    // Favorites
    public const string FavoritesCategoryIdentifier = "Favorites";
    public const string FavoritesCategoryDisplay = "⭐ Favorites";
    public const int MaxFavoritesCount = 50;
    
    // Error/Info messages
    public const string NoCommandTemplateAvailable = "NoCommandTemplateAvailable";
    public const string ValidationErrorsOccurred = "ValidationErrorsOccurred";
    public const string InvalidCommand = "InvalidCommand";
}

public static class CommandExecutionConstants
{
    public const int DefaultTimeoutSeconds = 30;
    public const int MaxTimeoutSeconds = 300;
    public const int MinTimeoutSeconds = 1;
    public const int PowerShellGalleryTimeout = 60;
    public const int PowerShellInstallTimeout = 300;
    public const int Timer_UpdateIntervalMs = 100;
}

public static class ValidationConstants
{
    // AutoCleanupDays
    public const int MinAutoCleanupDays = 1;
    public const int MaxAutoCleanupDays = 3650;
    public const int DefaultAutoCleanupDays = 90;
    
    // MaxHistoryItems
    public const int MinHistoryItems = 10;
    public const int MaxHistoryItems = 100000;
    public const int DefaultMaxHistoryItems = 1000;
    
    // RecentCommandsCount
    public const int MinRecentCommandsCount = 1;
    public const int MaxRecentCommandsCount = 50;
    public const int DefaultRecentCommandsCount = 5;
}

public static class AuditConstants
{
    public const string BatchExecutionCategory = "Batch Execution";
    public const string ManualExecutionCategory = "Manual Execution";
}
```

---

## 2. NOTIFICATION SERVICE ABSTRACTION

### Créer: `src/TwinShell.App/Services/WpfNotificationService.cs`

```csharp
using System.Windows;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Services;

public class WpfNotificationService : INotificationService
{
    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowInfo(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool ShowQuestion(string message, string title)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public bool ShowConfirmation(string message, string title, string confirmButtonText = "Yes")
    {
        var result = MessageBox.Show(
            message, 
            title, 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question, 
            MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }
}
```

### Mettre à jour: `src/TwinShell.Core/Interfaces/INotificationService.cs`

```csharp
namespace TwinShell.Core.Interfaces;

public interface INotificationService
{
    void ShowError(string message, string title);
    void ShowWarning(string message, string title);
    void ShowInfo(string message, string title);
    bool ShowQuestion(string message, string title);
    bool ShowConfirmation(string message, string title, string confirmButtonText = "Yes");
}
```

---

## 3. REFACTORISATION DE MainViewModel

### Avant (542 lignes - God Class):
```
MainViewModel
├── Filtering (11 properties)
├── Command Generation
├── Command Execution
├── Favorites Management
├── History Management
├── Config Export/Import
└── Action Selection
```

### Après (3 ViewModels):

#### A. Créer: `src/TwinShell.App/ViewModels/CommandGenerationViewModel.cs`
```csharp
public partial class CommandGenerationViewModel : ObservableObject
{
    private readonly ICommandGeneratorService _commandGeneratorService;
    
    [ObservableProperty]
    private ObservableCollection<ParameterViewModel> _commandParameters = new();
    
    [ObservableProperty]
    private string _generatedCommand = string.Empty;
    
    public CommandGenerationViewModel(ICommandGeneratorService commandGeneratorService)
    {
        _commandGeneratorService = commandGeneratorService;
    }
    
    public void LoadCommandParameters(CommandTemplate template)
    {
        CommandParameters.Clear();
        var defaults = _commandGeneratorService.GetDefaultParameterValues(template);
        
        foreach (var param in template.Parameters)
        {
            var paramVm = new ParameterViewModel
            {
                Name = param.Name,
                Label = param.Label,
                Type = param.Type,
                Required = param.Required,
                Description = param.Description ?? string.Empty,
                Value = defaults.ContainsKey(param.Name) ? defaults[param.Name] : string.Empty
            };
            
            paramVm.ValueChanged += () => GenerateCommand(template);
            CommandParameters.Add(paramVm);
        }
        
        GenerateCommand(template);
    }
    
    public void GenerateCommand(CommandTemplate template)
    {
        if (template == null)
            return;
            
        var paramValues = CommandParameters.ToDictionary(p => p.Name, p => p.Value);
        
        if (_commandGeneratorService.ValidateParameters(template, paramValues, out var errors))
        {
            GeneratedCommand = _commandGeneratorService.GenerateCommand(template, paramValues);
        }
        else
        {
            GeneratedCommand = $"Erreurs: {string.Join(", ", errors)}";
        }
    }
}
```

#### B. Créer: `src/TwinShell.App/ViewModels/FavoritesManagementViewModel.cs`
```csharp
public partial class FavoritesManagementViewModel : ObservableObject
{
    private readonly IFavoritesService _favoritesService;
    private readonly INotificationService _notificationService;
    private HashSet<string> _favoriteActionIds = new();
    
    public FavoritesManagementViewModel(
        IFavoritesService favoritesService,
        INotificationService notificationService)
    {
        _favoritesService = favoritesService;
        _notificationService = notificationService;
    }
    
    public async Task InitializeAsync()
    {
        await LoadFavoritesAsync();
    }
    
    public async Task LoadFavoritesAsync()
    {
        var favorites = await _favoritesService.GetAllFavoritesAsync();
        _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();
    }
    
    public async Task ToggleFavoriteAsync(Action action)
    {
        if (action == null) return;
        
        var result = await _favoritesService.ToggleFavoriteAsync(action.Id);
        await LoadFavoritesAsync();
        
        if (!result)
        {
            var count = await _favoritesService.GetFavoriteCountAsync();
            if (count >= 50)
            {
                _notificationService.ShowWarning(
                    $"Vous avez atteint la limite maximale de 50 favoris. Veuillez en supprimer avant d'en ajouter de nouveaux.",
                    "Limite de favoris atteinte");
            }
        }
    }
    
    public bool IsActionFavorite(string actionId)
    {
        return _favoriteActionIds.Contains(actionId);
    }
}
```

#### C. Réduire MainViewModel (220 lignes instead of 542):
```csharp
public partial class MainViewModel : ObservableObject
{
    // Services
    private readonly IActionService _actionService;
    private readonly ISearchService _searchService;
    private readonly ICommandHistoryService _commandHistoryService;
    private readonly IConfigurationService _configurationService;
    private readonly INotificationService _notificationService;
    
    // Child ViewModels
    private readonly CommandGenerationViewModel _commandGenerationVm;
    private readonly FavoritesManagementViewModel _favoritesVm;
    
    // Data
    private List<Action> _allActions = new();
    
    // UI Properties (filtering only)
    [ObservableProperty]
    private ObservableCollection<string> _categories = new();
    
    [ObservableProperty]
    private ObservableCollection<Action> _filteredActions = new();
    
    [ObservableProperty]
    private Action? _selectedAction;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private string? _selectedCategory;
    
    // Platform & Level filters...
    
    public MainViewModel(
        IActionService actionService,
        ISearchService searchService,
        ICommandHistoryService commandHistoryService,
        IConfigurationService configurationService,
        INotificationService notificationService,
        CommandGenerationViewModel commandGenerationVm,
        FavoritesManagementViewModel favoritesVm)
    {
        _actionService = actionService;
        _searchService = searchService;
        _commandHistoryService = commandHistoryService;
        _configurationService = configurationService;
        _notificationService = notificationService;
        _commandGenerationVm = commandGenerationVm;
        _favoritesVm = favoritesVm;
    }
    
    // Keep only filtering logic here
    private async Task ApplyFiltersAsync()
    {
        // Simplified filtering logic
    }
    
    partial void OnSelectedActionChanged(Action? value)
    {
        if (value != null)
        {
            _commandGenerationVm.LoadCommandParameters(
                GetActiveTemplate(value));
        }
    }
}
```

---

## 4. REFACTORISATION DE ExecutionViewModel

### Avant (299 lignes - Long method):
```
ExecuteCommandAsync (147 lignes)
├── Validation
├── Dangerous check & confirmation
├── UI setup
├── Timer setup
├── Command execution
├── Output processing
└── History update
```

### Après (Méthodes extraites):

```csharp
[RelayCommand]
private async Task ExecuteCommandAsync(ExecuteCommandParameter parameter)
{
    if (!ValidateParameter(parameter))
        return;
    
    if (!await ConfirmDangerousCommandAsync(parameter))
        return;
    
    SetupExecutionUI();
    
    try
    {
        var result = await ExecuteWithTimerAsync(parameter);
        await ProcessExecutionResultAsync(parameter, result);
        await UpdateHistoryAsync(parameter, result);
    }
    catch (Exception ex)
    {
        HandleExecutionError(ex);
    }
    finally
    {
        CleanupExecution();
    }
}

private bool ValidateParameter(ExecuteCommandParameter parameter)
{
    if (!string.IsNullOrWhiteSpace(parameter.Command))
        return true;
    
    _notificationService.ShowWarning(
        "Pas de commande valide à exécuter",
        "Erreur d'exécution");
    return false;
}

private async Task<bool> ConfirmDangerousCommandAsync(ExecuteCommandParameter parameter)
{
    if (!parameter.IsDangerous || !parameter.RequireConfirmation)
        return true;
    
    return _notificationService.ShowConfirmation(
        $"⚠️ ATTENTION: Cette commande peut causer des modifications système.\n\nCommande: {parameter.Command}\n\nÊtes-vous sûr?",
        "Confirmation de commande dangereuse");
}

private void SetupExecutionUI()
{
    OutputLines.Clear();
    IsExecuting = true;
    StatusMessage = "Exécution...";
    ExecutionProgress = 0;
}

private async Task<ExecutionResult> ExecuteWithTimerAsync(ExecuteCommandParameter parameter)
{
    _executionCts = new CancellationTokenSource();
    _executionStartTime = DateTime.Now;
    
    StartExecutionTimer();
    
    try
    {
        var settings = await _settingsService.GetSettingsAsync();
        var timeout = Math.Min(Math.Max(settings.ExecutionTimeoutSeconds, 
            CommandExecutionConstants.MinTimeoutSeconds), 
            CommandExecutionConstants.MaxTimeoutSeconds);
        
        LogExecutionStart(parameter, timeout);
        
        var result = await _commandExecutionService.ExecuteAsync(
            parameter.Command,
            parameter.Platform,
            _executionCts.Token,
            timeout,
            onOutputReceived: ProcessOutputLine);
        
        return result;
    }
    finally
    {
        _executionTimer?.Stop();
    }
}

private void StartExecutionTimer()
{
    _executionTimer = new System.Timers.Timer(CommandExecutionConstants.Timer_UpdateIntervalMs);
    _executionTimer.Elapsed += (s, e) =>
    {
        var elapsed = DateTime.Now - _executionStartTime;
        Application.Current.Dispatcher.Invoke(() =>
        {
            ExecutionTime = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        });
    };
    _executionTimer.Start();
}

private void ProcessOutputLine(string text, bool isError)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        AddOutputLine(text, isError);
    });
}

private void LogExecutionStart(ExecuteCommandParameter parameter, int timeout)
{
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Exécution: {parameter.Command}", false);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Plateforme: {parameter.Platform}", false);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Timeout: {timeout}s", false);
    AddOutputLine("", false);
}

private async Task ProcessExecutionResultAsync(ExecuteCommandParameter parameter, ExecutionResult result)
{
    AddExecutionSummary(result);
    StatusMessage = result.Success ? "Exécution complètée avec succès" : "Exécution échouée";
    ExecutionProgress = 100;
}

private void AddExecutionSummary(ExecutionResult result)
{
    AddOutputLine("", false);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ─────────────────────", false);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Code de sortie: {result.ExitCode}", !result.Success);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Durée: {result.Duration.TotalSeconds:F2}s", false);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Statut: {(result.Success ? "✓ SUCCÈS" : "✗ ÉCHOUÉ")}", !result.Success);
    
    if (result.WasCancelled)
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ⚠ Exécution annulée", true);
    else if (result.TimedOut)
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ⚠ Exécution timeout", true);
    
    if (!string.IsNullOrEmpty(result.ErrorMessage))
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Erreur: {result.ErrorMessage}", true);
}

private async Task UpdateHistoryAsync(ExecuteCommandParameter parameter, ExecutionResult result)
{
    if (parameter.ActionId == null)
        return;
    
    var historyId = await _commandHistoryService.AddCommandAsync(
        parameter.ActionId,
        parameter.Command,
        parameter.Parameters,
        parameter.Platform,
        parameter.ActionTitle,
        parameter.Category);
    
    await _commandHistoryService.UpdateWithExecutionResultsAsync(
        historyId,
        result.ExitCode,
        result.Duration,
        result.Success);
}

private void HandleExecutionError(Exception ex)
{
    _executionTimer?.Stop();
    AddOutputLine("", false);
    AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ✗ ERREUR: {ex.Message}", true);
    StatusMessage = "Erreur d'exécution";
}

private void CleanupExecution()
{
    IsExecuting = false;
    _executionCts?.Dispose();
    _executionCts = null;
    _executionTimer?.Stop();
    _executionTimer?.Dispose();
    _executionTimer = null;
}
```

---

## 5. EXTÉRIORISER LA LOGIQUE DUPLIQUÉE

### PowerShellGalleryService - Extract Common Deserialization:

```csharp
private static List<T> DeserializeJsonSafely<T>(string json) where T : class
{
    try
    {
        // Try as array first
        try
        {
            var items = JsonSerializer.Deserialize<List<T>>(json);
            if (items != null)
                return items;
        }
        catch
        {
            // Try as single object
            var item = JsonSerializer.Deserialize<T>(json);
            if (item != null)
                return new List<T> { item };
        }
    }
    catch (Exception ex)
    {
        // Log exception
        Debug.WriteLine($"Failed to deserialize JSON: {ex.Message}");
    }
    
    return new List<T>();
}
```

### Usage:
```csharp
public async Task<IEnumerable<PowerShellModule>> SearchModulesAsync(string query, int maxResults = 50)
{
    var result = await _commandExecutionService.ExecuteAsync(...);
    
    if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
        return Enumerable.Empty<PowerShellModule>();
    
    var moduleDtos = DeserializeJsonSafely<PowerShellGalleryModuleDto>(result.Stdout);
    return moduleDtos.Select(MapToModule);
}
```

---

## 6. UTILISATION DU SERVICE DE LOCALISATION

### CommandGeneratorService - Avant:
```csharp
errors.Add($"Le paramètre '{parameter.Label}' est requis.");
errors.Add($"Le paramètre '{parameter.Label}' doit être un nombre entier.");
```

### Après:
```csharp
private readonly ILocalizationService _localizationService;

public bool ValidateParameters(
    CommandTemplate template,
    Dictionary<string, string> parameterValues,
    out List<string> errors)
{
    errors = new List<string>();
    
    foreach (var parameter in template.Parameters)
    {
        if (parameter.Required)
        {
            if (!parameterValues.ContainsKey(parameter.Name) ||
                string.IsNullOrWhiteSpace(parameterValues[parameter.Name]))
            {
                errors.Add(_localizationService.GetString(
                    "Validation.RequiredParameter",
                    parameter.Label));
            }
        }
        
        // Type validation...
        switch (parameter.Type.ToLowerInvariant())
        {
            case "int":
            case "integer":
                if (!int.TryParse(value, out _))
                {
                    errors.Add(_localizationService.GetString(
                        "Validation.IntegerParameter",
                        parameter.Label));
                }
                break;
        }
    }
    
    return errors.Count == 0;
}
```

---

## 7. TEMPLATE SELECTION HELPER

### Créer: Helper method pour MainViewModel

```csharp
private CommandTemplate? GetActiveTemplate(Action action)
{
    return action.WindowsCommandTemplate ?? action.LinuxCommandTemplate;
}

private Platform DeterminePlatform(Action action, CommandTemplate template)
{
    return template == action.WindowsCommandTemplate 
        ? Platform.Windows 
        : Platform.Linux;
}
```

### Usage:
```csharp
// Au lieu de répéter 4 fois:
var template = SelectedAction.WindowsCommandTemplate ?? SelectedAction.LinuxCommandTemplate;
var platform = template == SelectedAction.WindowsCommandTemplate ? Platform.Windows : Platform.Linux;

// Utiliser:
var template = GetActiveTemplate(SelectedAction);
var platform = DeterminePlatform(SelectedAction, template);
```

---

## RÉSUMÉ DES REFACTORISATIONS

| # | Fichier | Changement | Durée |
|---|---------|-----------|-------|
| 1 | New: UIConstants.cs | Centraliser les constantes | 2h |
| 2 | New: WpfNotificationService.cs | Implémenter INotificationService | 2h |
| 3 | MainViewModel.cs | Réduire de 542 à 220 lignes | 8h |
| 4 | New: CommandGenerationViewModel.cs | Extraire logique commande | 4h |
| 5 | New: FavoritesManagementViewModel.cs | Extraire gestion favoris | 3h |
| 6 | ExecutionViewModel.cs | Réduire de 299 à 150 lignes | 6h |
| 7 | PowerShellGalleryService.cs | Extraire deserialization | 2h |
| 8 | CommandGeneratorService.cs | Utiliser localisation | 2h |
| 9 | All ViewModels | Injecter INotificationService | 4h |
| 10 | SettingsViewModel.cs | Utiliser ValidationConstants | 1h |

**Total**: ~34 heures

