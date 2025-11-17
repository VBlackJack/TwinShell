using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly ICommandGeneratorService _commandGeneratorService;
    private readonly IClipboardService _clipboardService;
    private readonly ICommandHistoryService _commandHistoryService;
    private readonly IFavoritesService _favoritesService;
    private readonly IConfigurationService _configurationService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;
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

    /// <summary>
    /// Reference to the ExecutionViewModel (set from MainWindow)
    /// </summary>
    public ExecutionViewModel? ExecutionViewModel { get; set; }

    public MainViewModel(
        IActionService actionService,
        ISearchService searchService,
        ICommandGeneratorService commandGeneratorService,
        IClipboardService clipboardService,
        ICommandHistoryService commandHistoryService,
        IFavoritesService favoritesService,
        IConfigurationService configurationService,
        IDialogService dialogService,
        ILocalizationService localizationService)
    {
        _actionService = actionService;
        _searchService = searchService;
        _commandGeneratorService = commandGeneratorService;
        _clipboardService = clipboardService;
        _commandHistoryService = commandHistoryService;
        _favoritesService = favoritesService;
        _configurationService = configurationService;
        _dialogService = dialogService;
        _localizationService = localizationService;
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

        // Add "Favorites" as first category
        categories.Insert(0, UIConstants.FavoritesCategoryDisplay);
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

    private async Task ApplyFiltersAsync()
    {
        // Use semaphore to prevent concurrent filter operations
        await _filterSemaphore.WaitAsync();
        try
        {
            var filtered = _allActions.AsEnumerable();

            // Favorites filter (special category)
            if (SelectedCategory == UIConstants.FavoritesCategoryDisplay)
            {
                filtered = filtered.Where(a => _favoriteActionIds.Contains(a.Id));
            }
            // Category filter
            else if (!string.IsNullOrEmpty(SelectedCategory))
            {
                filtered = filtered.Where(a => a.Category == SelectedCategory);
            }

            // Show favorites only filter
            if (ShowFavoritesOnly)
            {
                filtered = filtered.Where(a => _favoriteActionIds.Contains(a.Id));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
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

            // PERFORMANCE: Use ReplaceRange() instead of recreating collection - single UI notification
            FilteredActions.ReplaceRange(filtered);
        }
        finally
        {
            _filterSemaphore.Release();
        }
    }

    private void LoadCommandGenerator()
    {
        if (SelectedAction == null)
        {
            CommandParameters.Clear();
            GeneratedCommand = string.Empty;
            return;
        }

        // Determine which template to use (prefer Windows for now)
        var template = TemplateHelper.GetActiveTemplate(SelectedAction);

        if (!TemplateHelper.IsValidTemplate(template))
        {
            CommandParameters.Clear();
            GeneratedCommand = _localizationService.GetString(MessageKeys.ValidationNoCommandTemplate);
            return;
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
                Value = defaults.ContainsKey(param.Name) ? defaults[param.Name] : string.Empty
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

        var template = TemplateHelper.GetActiveTemplate(SelectedAction);
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
