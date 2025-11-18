using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.Collections;
using TwinShell.Core.Constants;
using TwinShell.Core.Enums;
using TwinShell.Core.Helpers;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

// BUGFIX: Implemented IDisposable to properly dispose of SemaphoreSlim and prevent resource leaks
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IActionService _actionService;
    private readonly ISearchService _searchService;
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ICommandGeneratorService _commandGeneratorService;
    private readonly IClipboardService _clipboardService;
    private readonly ICommandHistoryService _commandHistoryService;
    private readonly IFavoritesService _favoritesService;
    private readonly IConfigurationService _configurationService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;
    private readonly IImportExportService _importExportService;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _filterSemaphore = new SemaphoreSlim(1, 1);
    private bool _disposed = false;

    private List<ActionModel> _allActions = new();
    private HashSet<string> _favoriteActionIds = new();

    [ObservableProperty]
    private ObservableCollection<string> _categories = new();

    [ObservableProperty]
    private ObservableRangeCollection<ActionModel> _filteredActions = new();

    [ObservableProperty]
    private ActionModel? _selectedAction;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _selectedCategory;

    [ObservableProperty]
    private bool _filterWindows = true;

    [ObservableProperty]
    private bool _filterLinux = true;

    [ObservableProperty]
    private bool _filterBoth = true;

    [ObservableProperty]
    private bool _filterInfo = true;

    [ObservableProperty]
    private bool _filterRun = true;

    [ObservableProperty]
    private bool _filterDangerous = true;

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private string _generatedCommand = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ParameterViewModel> _commandParameters = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = UIConstants.DefaultStatusMessage;

    [ObservableProperty]
    private bool _isCommandCrossPlatform;

    [ObservableProperty]
    private Platform _selectedPlatformForGenerator = Platform.Windows;

    [ObservableProperty]
    private int _searchResultCount;

    [ObservableProperty]
    private string _searchTime = string.Empty;

    [ObservableProperty]
    private bool _showSearchMetrics;

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

    /// <summary>
    /// Reference to the ExecutionViewModel (set from MainWindow)
    /// </summary>
    public ExecutionViewModel? ExecutionViewModel { get; set; }

    public MainViewModel(
        IActionService actionService,
        ISearchService searchService,
        ISearchHistoryService searchHistoryService,
        ICommandGeneratorService commandGeneratorService,
        IClipboardService clipboardService,
        ICommandHistoryService commandHistoryService,
        IFavoritesService favoritesService,
        IConfigurationService configurationService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IImportExportService importExportService,
        IServiceProvider serviceProvider)
    {
        _actionService = actionService;
        _searchService = searchService;
        _searchHistoryService = searchHistoryService;
        _commandGeneratorService = commandGeneratorService;
        _clipboardService = clipboardService;
        _commandHistoryService = commandHistoryService;
        _favoritesService = favoritesService;
        _configurationService = configurationService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _importExportService = importExportService;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = _localizationService.GetString(MessageKeys.Loading);
        try
        {
            await LoadActionsAsync();
            StatusMessage = _localizationService.GetFormattedString(MessageKeys.StatusActionsLoaded, _allActions.Count);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadActionsAsync()
    {
        _allActions = (await _actionService.GetAllActionsAsync()).ToList();

        // Load favorites
        var favorites = await _favoritesService.GetAllFavoritesAsync();
        _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();

        var categories = (await _actionService.GetAllCategoriesAsync()).ToList();

        // Add special categories at the beginning
        categories.Insert(0, UIConstants.FavoritesCategoryDisplay);
        categories.Insert(0, UIConstants.AllCategoryDisplay);
        Categories = new ObservableCollection<string>(categories);

        await ApplyFiltersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        SafeExecuteAsync(ApplyFiltersAsync);
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        SafeExecuteAsync(ApplyFiltersAsync);
    }

    partial void OnFilterWindowsChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterLinuxChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterBothChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterInfoChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterRunChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterDangerousChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnShowFavoritesOnlyChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);

    partial void OnSelectedActionChanged(ActionModel? value)
    {
        if (value != null)
        {
            LoadCommandGenerator();
        }
    }

    partial void OnSelectedPlatformForGeneratorChanged(Platform value)
    {
        // Regenerate command when platform selection changes
        if (SelectedAction != null && IsCommandCrossPlatform)
        {
            LoadCommandGenerator();
        }
    }

    private async Task ApplyFiltersAsync()
    {
        // Use semaphore to prevent concurrent filter operations
        await _filterSemaphore.WaitAsync();
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var filtered = _allActions.AsEnumerable();

            // UX: When search is active, ignore category filter to show all matching results
            var hasActiveSearch = !string.IsNullOrWhiteSpace(SearchText);

            // Favorites filter (special category)
            if (!hasActiveSearch && SelectedCategory == UIConstants.FavoritesCategoryDisplay)
            {
                filtered = filtered.Where(a => _favoriteActionIds.Contains(a.Id));
            }
            // All category (special category - shows everything, no filter)
            else if (!hasActiveSearch && SelectedCategory == UIConstants.AllCategoryDisplay)
            {
                // No filtering - show all actions
            }
            // Category filter (only apply if no active search)
            else if (!hasActiveSearch && !string.IsNullOrEmpty(SelectedCategory))
            {
                filtered = filtered.Where(a => a.Category == SelectedCategory);
            }

            // Show favorites only filter
            if (ShowFavoritesOnly)
            {
                filtered = filtered.Where(a => _favoriteActionIds.Contains(a.Id));
            }

            // Search filter (applies across all categories when active)
            if (hasActiveSearch)
            {
                filtered = await _searchService.SearchAsync(filtered, SearchText);
            }

            // Platform filter
            var platformFilters = new List<Platform>();
            if (FilterWindows) platformFilters.Add(Platform.Windows);
            if (FilterLinux) platformFilters.Add(Platform.Linux);
            if (FilterBoth) platformFilters.Add(Platform.Both);

            if (platformFilters.Count > 0 && platformFilters.Count < 3)
            {
                filtered = filtered.Where(a => platformFilters.Contains(a.Platform));
            }

            // Level filter
            var levelFilters = new List<CriticalityLevel>();
            if (FilterInfo) levelFilters.Add(CriticalityLevel.Info);
            if (FilterRun) levelFilters.Add(CriticalityLevel.Run);
            if (FilterDangerous) levelFilters.Add(CriticalityLevel.Dangerous);

            if (levelFilters.Count > 0 && levelFilters.Count < 3)
            {
                filtered = filtered.Where(a => levelFilters.Contains(a.Level));
            }

            // Materialize results and stop timing
            var results = filtered.ToList();
            sw.Stop();

            // Update search metrics
            if (hasActiveSearch)
            {
                SearchResultCount = results.Count;
                SearchTime = sw.ElapsedMilliseconds < 1000
                    ? $"{sw.ElapsedMilliseconds}ms"
                    : $"{sw.Elapsed.TotalSeconds:F2}s";
                ShowSearchMetrics = true;

                // Save to search history (async, don't await to avoid blocking UI)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _searchHistoryService.AddSearchAsync(SearchText, results.Count);
                    }
                    catch
                    {
                        // Silently ignore search history errors to not disrupt user experience
                    }
                });

                // Update search suggestions
                await UpdateSearchSuggestionsAsync();
            }
            else
            {
                ShowSearchMetrics = false;
                SearchSuggestions.Clear();
            }

            // PERFORMANCE: Use ReplaceRange() instead of recreating collection - single UI notification
            FilteredActions.ReplaceRange(results);
        }
        finally
        {
            _filterSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates search suggestions based on current search text
    /// </summary>
    private async Task UpdateSearchSuggestionsAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
            {
                SearchSuggestions.Clear();
                return;
            }

            var suggestions = await _searchHistoryService.GetSearchSuggestionsAsync(SearchText, limit: 5);
            var filteredSuggestions = suggestions
                .Where(s => !s.Equals(SearchText, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            SearchSuggestions.Clear();
            foreach (var suggestion in filteredSuggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        }
        catch
        {
            // Silently ignore errors to not disrupt user experience
            SearchSuggestions.Clear();
        }
    }

    private void LoadCommandGenerator()
    {
        if (SelectedAction == null)
        {
            CommandParameters.Clear();
            GeneratedCommand = string.Empty;
            IsCommandCrossPlatform = false;
            return;
        }

        // Detect if this is a cross-platform command (platform: 2 / Both)
        IsCommandCrossPlatform = SelectedAction.Platform == Platform.Both &&
                                 SelectedAction.WindowsCommandTemplate != null &&
                                 SelectedAction.LinuxCommandTemplate != null;

        // Determine which template to use
        CommandTemplate? template;
        if (IsCommandCrossPlatform)
        {
            // Use the selected platform for cross-platform commands
            template = SelectedPlatformForGenerator == Platform.Windows
                ? SelectedAction.WindowsCommandTemplate
                : SelectedAction.LinuxCommandTemplate;
        }
        else
        {
            // Use the default logic for single-platform commands
            template = TemplateHelper.GetActiveTemplate(SelectedAction);
        }

        if (!TemplateHelper.IsValidTemplate(template))
        {
            CommandParameters.Clear();
            GeneratedCommand = _localizationService.GetString(MessageKeys.ValidationNoCommandTemplate);
            return;
        }

        // BUGFIX: Save current parameter values before clearing to preserve user input when switching platforms
        var savedValues = new Dictionary<string, string>();
        foreach (var param in CommandParameters)
        {
            if (!string.IsNullOrEmpty(param.Value))
            {
                savedValues[param.Name] = param.Value;
            }
        }

        // Load parameters
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
                // BUGFIX: Restore saved value if exists, otherwise use default
                Value = savedValues.ContainsKey(param.Name)
                    ? savedValues[param.Name]
                    : (defaults.ContainsKey(param.Name) ? defaults[param.Name] : string.Empty)
            };

            paramVm.ValueChanged += () => GenerateCommand();
            CommandParameters.Add(paramVm);
        }

        GenerateCommand();
    }

    [RelayCommand]
    private void GenerateCommand()
    {
        if (SelectedAction == null)
        {
            return;
        }

        // BUGFIX: Use the same template selection logic as LoadCommandGenerator
        // to respect platform selection for cross-platform commands
        CommandTemplate? template;
        if (IsCommandCrossPlatform)
        {
            template = SelectedPlatformForGenerator == Platform.Windows
                ? SelectedAction.WindowsCommandTemplate
                : SelectedAction.LinuxCommandTemplate;
        }
        else
        {
            template = TemplateHelper.GetActiveTemplate(SelectedAction);
        }

        if (!TemplateHelper.IsValidTemplate(template))
        {
            return;
        }

        var paramValues = CommandParameters.ToDictionary(p => p.Name, p => p.Value);

        if (_commandGeneratorService.ValidateParameters(template!, paramValues, out var errors))
        {
            GeneratedCommand = _commandGeneratorService.GenerateCommand(template!, paramValues);
        }
        else
        {
            var validationHeader = _localizationService.GetString(MessageKeys.ValidationErrors);
            GeneratedCommand = $"{validationHeader}\n{string.Join("\n", errors)}";
        }
    }

    [RelayCommand]
    private async Task CopyCommandAsync()
    {
        var validationErrorsText = _localizationService.GetString(MessageKeys.ValidationErrors);
        var noTemplateText = _localizationService.GetString(MessageKeys.ValidationNoCommandTemplate);

        if (!string.IsNullOrWhiteSpace(GeneratedCommand) &&
            !GeneratedCommand.StartsWith(validationErrorsText) &&
            !GeneratedCommand.StartsWith(noTemplateText) &&
            SelectedAction != null)
        {
            _clipboardService.SetText(GeneratedCommand);

            // Show success notification
            Services.SnackBarService.Instance.ShowSuccess("✓ Command copied to clipboard");

            // Save to history
            var template = SelectedAction.WindowsCommandTemplate ?? SelectedAction.LinuxCommandTemplate;
            var platform = template == SelectedAction.WindowsCommandTemplate ? Platform.Windows : Platform.Linux;
            var parameters = CommandParameters.ToDictionary(p => p.Name, p => p.Value);

            await _commandHistoryService.AddCommandAsync(
                SelectedAction.Id,
                GeneratedCommand,
                parameters,
                platform,
                SelectedAction.Title,
                SelectedAction.Category
            );
        }
    }

    /// <summary>
    /// Execute the generated command (Sprint 4)
    /// </summary>
    [RelayCommand]
    private async Task ExecuteCommandAsync()
    {
        var validationErrorsText = _localizationService.GetString(MessageKeys.ValidationErrors);
        var noTemplateText = _localizationService.GetString(MessageKeys.ValidationNoCommandTemplate);

        if (string.IsNullOrWhiteSpace(GeneratedCommand) ||
            GeneratedCommand.StartsWith(validationErrorsText) ||
            GeneratedCommand.StartsWith(noTemplateText) ||
            SelectedAction == null ||
            ExecutionViewModel == null)
        {
            _dialogService.ShowWarning(
                _localizationService.GetString(MessageKeys.ValidationNoValidCommand),
                _localizationService.GetString(MessageKeys.ExecutionError));
            return;
        }

        // Determine platform
        var template = TemplateHelper.GetActiveTemplate(SelectedAction);
        var platform = TemplateHelper.GetPlatformForTemplate(SelectedAction, template!);
        var parameters = CommandParameters.ToDictionary(p => p.Name, p => p.Value);

        // Create execution parameter
        var executeParameter = new ExecuteCommandParameter
        {
            Command = GeneratedCommand,
            Platform = platform,
            IsDangerous = SelectedAction.Level == CriticalityLevel.Dangerous,
            RequireConfirmation = true,
            ActionId = SelectedAction.Id,
            ActionTitle = SelectedAction.Title,
            Category = SelectedAction.Category,
            Parameters = parameters
        };

        // Execute the command via ExecutionViewModel
        await ExecutionViewModel.ExecuteCommandCommand.ExecuteAsync(executeParameter);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
        FilterWindows = true;
        FilterLinux = true;
        FilterBoth = true;
        FilterInfo = true;
        FilterRun = true;
        FilterDangerous = true;
        ShowFavoritesOnly = false;
    }

    /// <summary>
    /// Toggle favorite status for the selected action
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (SelectedAction == null) return;

        try
        {
            // Check if currently a favorite before toggling
            var wasFavorite = _favoriteActionIds.Contains(SelectedAction.Id);

            // Toggle and get the result (true if added, false if removed or limit reached)
            var result = await _favoritesService.ToggleFavoriteAsync(SelectedAction.Id);

            // Reload favorites to get current state
            var favorites = await _favoritesService.GetAllFavoritesAsync();
            _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();

            // Determine actual action taken based on result
            var isFavoriteNow = _favoriteActionIds.Contains(SelectedAction.Id);

            // Refresh the filtered list
            await ApplyFiltersAsync();

            // Show appropriate notification based on actual state change
            if (wasFavorite && !isFavoriteNow)
            {
                // Successfully removed from favorites
                StatusMessage = _localizationService.GetFormattedString(MessageKeys.FavoriteRemoved, SelectedAction.Title);
            }
            else if (!wasFavorite && isFavoriteNow)
            {
                // Successfully added to favorites
                StatusMessage = _localizationService.GetFormattedString(MessageKeys.FavoriteAdded, SelectedAction.Title);
            }
            else if (!wasFavorite && !isFavoriteNow && !result)
            {
                // Failed to add - check if limit reached
                var count = await _favoritesService.GetFavoriteCountAsync();
                if (count >= UIConstants.MaxFavoritesCount)
                {
                    _dialogService.ShowWarning(
                        _localizationService.GetFormattedString(MessageKeys.FavoritesLimitReachedMessage, UIConstants.MaxFavoritesCount, count),
                        _localizationService.GetString(MessageKeys.FavoritesLimitReached));
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            StatusMessage = _localizationService.GetString(MessageKeys.FavoritesToggleFailed);
            _dialogService.ShowError(
                _localizationService.GetString(MessageKeys.FavoritesUpdateError),
                _localizationService.GetString(MessageKeys.CommonError));
        }
    }

    /// <summary>
    /// Check if the selected action is a favorite
    /// </summary>
    public bool IsSelectedActionFavorite()
    {
        return SelectedAction != null && _favoriteActionIds.Contains(SelectedAction.Id);
    }

    /// <summary>
    /// Safely executes async methods from synchronous event handlers
    /// BUGFIX: Changed hardcoded error message to use localization service
    /// </summary>
    private async void SafeExecuteAsync(Func<Task> asyncMethod)
    {
        try
        {
            await asyncMethod();
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            StatusMessage = _localizationService.GetString(MessageKeys.CommonErrorProcessing);
        }
    }

    /// <summary>
    /// Disposes resources used by the MainViewModel
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _filterSemaphore?.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Add a new action
    /// </summary>
    [RelayCommand]
    private async Task AddNewActionAsync()
    {
        try
        {
            var editorVm = ActivatorUtilities.CreateInstance<ActionEditorViewModel>(_serviceProvider);
            await editorVm.InitializeForNewActionAsync();

            var window = new Views.ActionEditorWindow(editorVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                // Reload actions
                await LoadActionsAsync();
                StatusMessage = "✓ Nouvelle commande ajoutée avec succès";
                Services.SnackBarService.Instance.ShowSuccess("✓ Nouvelle commande ajoutée avec succès");
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _dialogService.ShowError(
                "Une erreur s'est produite lors de la création de la commande.",
                "Erreur");
        }
    }

    /// <summary>
    /// Edit the selected action
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private async Task EditActionAsync()
    {
        if (SelectedAction == null) return;

        try
        {
            var editorVm = ActivatorUtilities.CreateInstance<ActionEditorViewModel>(_serviceProvider);
            await editorVm.InitializeForEditActionAsync(SelectedAction);

            var window = new Views.ActionEditorWindow(editorVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                // Reload actions
                await LoadActionsAsync();
                StatusMessage = "✓ Commande mise à jour avec succès";
                Services.SnackBarService.Instance.ShowSuccess("✓ Commande mise à jour avec succès");
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _dialogService.ShowError(
                "Une erreur s'est produite lors de la modification de la commande.",
                "Erreur");
        }
    }

    /// <summary>
    /// Delete the selected action
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private async Task DeleteActionAsync()
    {
        if (SelectedAction == null) return;

        var warningMessage = SelectedAction.IsUserCreated
            ? $"Voulez-vous vraiment supprimer la commande '{SelectedAction.Title}' ?\n\nCette action est irréversible."
            : $"Voulez-vous vraiment supprimer la commande système '{SelectedAction.Title}' ?\n\nCette action est irréversible.\n\nNote: Pour restaurer une commande système supprimée, vous devrez supprimer la base de données.";

        var confirmed = _dialogService.ShowQuestion(warningMessage, "Confirmer la suppression");

        if (confirmed)
        {
            try
            {
                await _actionService.DeleteActionAsync(SelectedAction.Id);
                await LoadActionsAsync();
                StatusMessage = "✓ Commande supprimée";
                Services.SnackBarService.Instance.ShowSuccess("✓ Commande supprimée");
            }
            catch (Exception ex)
            {
                // SECURITY: Don't expose exception details to users
                _dialogService.ShowError(
                    "Une erreur s'est produite lors de la suppression de la commande.",
                    "Erreur");
            }
        }
    }

    /// <summary>
    /// Check if the selected action can be edited or deleted
    /// </summary>
    private bool CanEditOrDelete() => SelectedAction != null;

    /// <summary>
    /// Export actions database to JSON file
    /// </summary>
    [RelayCommand]
    private async Task ExportActionsAsync()
    {
        try
        {
            var fileName = _dialogService.ShowSaveFileDialog(
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                ".json",
                $"twinshell-actions-{DateTime.Now:yyyy-MM-dd-HHmmss}.json");

            if (fileName != null)
            {
                var result = await _importExportService.ExportActionsAsync(fileName);

                if (result.Success)
                {
                    _dialogService.ShowSuccess(
                        $"{result.ActionCount} commandes exportées vers:\n{fileName}",
                        "Export réussi");
                }
                else
                {
                    _dialogService.ShowError(
                        $"Erreur lors de l'export:\n{result.ErrorMessage}",
                        "Échec de l'export");
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _dialogService.ShowError(
                "Une erreur s'est produite lors de l'export.",
                "Erreur");
        }
    }

    /// <summary>
    /// Import actions from JSON file
    /// </summary>
    [RelayCommand]
    private async Task ImportActionsAsync()
    {
        try
        {
            var fileName = _dialogService.ShowOpenFileDialog(
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                ".json");

            if (fileName != null)
            {
                // Validate file first
                var validation = await _importExportService.ValidateImportFileAsync(fileName);

                if (!validation.IsValid)
                {
                    _dialogService.ShowWarning(
                        $"Fichier invalide:\n{validation.ErrorMessage}",
                        "Validation échouée");
                    return;
                }

                // Ask user for import mode
                var mergeMode = _dialogService.ShowQuestion(
                    $"Fichier valide contenant {validation.ActionCount} commandes.\n\n" +
                    "Mode d'import:\n" +
                    "• OUI = Fusionner (ajouter nouvelles, mettre à jour existantes)\n" +
                    "• NON = Remplacer (supprimer toutes vos commandes puis importer)\n\n" +
                    "Voulez-vous FUSIONNER avec vos commandes existantes ?",
                    "Choisir le mode d'import");

                var mode = mergeMode ? ImportMode.Merge : ImportMode.Replace;

                // Confirm if Replace mode
                if (mode == ImportMode.Replace)
                {
                    var confirmReplace = _dialogService.ShowQuestion(
                        "⚠ ATTENTION ⚠\n\n" +
                        "Le mode REMPLACEMENT va supprimer TOUTES vos commandes personnelles existantes.\n" +
                        "Les commandes système seront conservées.\n\n" +
                        "Êtes-vous ABSOLUMENT SÛR de vouloir continuer ?",
                        "Confirmer le remplacement");

                    if (!confirmReplace)
                    {
                        return;
                    }
                }

                // Perform import
                var result = await _importExportService.ImportActionsAsync(fileName, mode);

                if (result.Success)
                {
                    await LoadActionsAsync();
                    _dialogService.ShowSuccess(
                        $"Import terminé:\n" +
                        $"• {result.Imported} commandes importées\n" +
                        $"• {result.Updated} commandes mises à jour\n" +
                        $"• {result.Skipped} commandes ignorées",
                        "Import réussi");
                }
                else
                {
                    _dialogService.ShowError(
                        $"Erreur lors de l'import:\n{result.ErrorMessage}",
                        "Échec de l'import");
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _dialogService.ShowError(
                "Une erreur s'est produite lors de l'import.",
                "Erreur");
        }
    }

    /// <summary>
    /// Export configuration to JSON file
    /// </summary>
    [RelayCommand]
    private async Task ExportConfigurationAsync()
    {
        try
        {
            var fileName = _dialogService.ShowSaveFileDialog(
                DatabaseConstants.JsonFileFilter,
                DatabaseConstants.JsonFileExtension,
                $"{DatabaseConstants.ConfigurationFileName}-{DateTime.Now:yyyy-MM-dd}{DatabaseConstants.JsonFileExtension}");

            if (fileName != null)
            {
                var result = await _configurationService.ExportToJsonAsync(
                    fileName,
                    userId: null,
                    includeHistory: true);

                if (result.Success)
                {
                    _dialogService.ShowSuccess(
                        _localizationService.GetFormattedString(MessageKeys.ConfigExportSuccessMessage, fileName),
                        _localizationService.GetString(MessageKeys.ConfigExportSuccess));
                }
                else
                {
                    _dialogService.ShowError(
                        _localizationService.GetFormattedString(MessageKeys.ConfigExportErrorMessage, result.ErrorMessage),
                        _localizationService.GetString(MessageKeys.ConfigExportError));
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _dialogService.ShowError(
                _localizationService.GetString(MessageKeys.ConfigExportErrorGeneric),
                _localizationService.GetString(MessageKeys.ConfigExportError));
        }
    }

    /// <summary>
    /// Import configuration from JSON file
    /// </summary>
    [RelayCommand]
    private async Task ImportConfigurationAsync()
    {
        try
        {
            var fileName = _dialogService.ShowOpenFileDialog(
                DatabaseConstants.JsonFileFilter,
                DatabaseConstants.JsonFileExtension);

            if (fileName != null)
            {
                // Validate file first
                var validation = await _configurationService.ValidateConfigurationFileAsync(fileName);

                if (!validation.IsValid)
                {
                    _dialogService.ShowWarning(
                        _localizationService.GetFormattedString(MessageKeys.ConfigValidationErrorMessage, validation.ErrorMessage),
                        _localizationService.GetString(MessageKeys.ConfigValidationError));
                    return;
                }

                // Confirm import
                var confirmed = _dialogService.ShowQuestion(
                    _localizationService.GetFormattedString(MessageKeys.ConfigImportConfirmationMessage, validation.Version),
                    _localizationService.GetString(MessageKeys.ConfigImportConfirmation));

                if (confirmed)
                {
                    var result = await _configurationService.ImportFromJsonAsync(
                        fileName,
                        userId: null,
                        mergeMode: true);

                    if (result.Success)
                    {
                        _dialogService.ShowSuccess(
                            _localizationService.GetFormattedString(MessageKeys.ConfigImportSuccessMessage, result.FavoritesImported, result.HistoryImported),
                            _localizationService.GetString(MessageKeys.ConfigImportSuccess));

                        // Reload data
                        await LoadActionsAsync();
                    }
                    else
                    {
                        _dialogService.ShowError(
                            _localizationService.GetFormattedString(MessageKeys.ConfigImportErrorMessage, result.ErrorMessage),
                            _localizationService.GetString(MessageKeys.ConfigImportError));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _dialogService.ShowError(
                _localizationService.GetString(MessageKeys.ConfigImportErrorGeneric),
                _localizationService.GetString(MessageKeys.ConfigImportError));
        }
    }
}

public partial class ParameterViewModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public event System.Action? ValueChanged;

    partial void OnValueChanged(string value)
    {
        ValueChanged?.Invoke();
    }
}
