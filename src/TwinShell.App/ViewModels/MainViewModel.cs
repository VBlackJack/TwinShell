using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IActionService _actionService;
    private readonly ISearchService _searchService;
    private readonly ICommandGeneratorService _commandGeneratorService;
    private readonly IClipboardService _clipboardService;
    private readonly ICommandHistoryService _commandHistoryService;
    private readonly IFavoritesService _favoritesService;
    private readonly IConfigurationService _configurationService;
    private readonly SemaphoreSlim _filterSemaphore = new SemaphoreSlim(1, 1);

    private List<Action> _allActions = new();
    private HashSet<string> _favoriteActionIds = new();

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
    private string _statusMessage = "Ready";

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
        IConfigurationService configurationService)
    {
        _actionService = actionService;
        _searchService = searchService;
        _commandGeneratorService = commandGeneratorService;
        _clipboardService = clipboardService;
        _commandHistoryService = commandHistoryService;
        _favoritesService = favoritesService;
        _configurationService = configurationService;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading actions...";
        try
        {
            await LoadActionsAsync();
            StatusMessage = $"{_allActions.Count} actions loaded";
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
        categories.Insert(0, "⭐ Favorites");
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

    partial void OnSelectedActionChanged(Action? value)
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
            if (SelectedCategory == "⭐ Favorites")
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

            FilteredActions = new ObservableCollection<Action>(filtered);
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
        var template = SelectedAction.WindowsCommandTemplate ?? SelectedAction.LinuxCommandTemplate;

        if (template == null)
        {
            CommandParameters.Clear();
            GeneratedCommand = "Aucun modèle de commande disponible.";
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

        var template = SelectedAction.WindowsCommandTemplate ?? SelectedAction.LinuxCommandTemplate;
        if (template == null)
        {
            return;
        }

        var paramValues = CommandParameters.ToDictionary(p => p.Name, p => p.Value);

        if (_commandGeneratorService.ValidateParameters(template, paramValues, out var errors))
        {
            GeneratedCommand = _commandGeneratorService.GenerateCommand(template, paramValues);
        }
        else
        {
            GeneratedCommand = $"Erreurs de validation:\n{string.Join("\n", errors)}";
        }
    }

    [RelayCommand]
    private async Task CopyCommandAsync()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedCommand) &&
            !GeneratedCommand.StartsWith("Erreurs") &&
            !GeneratedCommand.StartsWith("Aucun") &&
            SelectedAction != null)
        {
            _clipboardService.SetText(GeneratedCommand);

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
        if (string.IsNullOrWhiteSpace(GeneratedCommand) ||
            GeneratedCommand.StartsWith("Erreurs") ||
            GeneratedCommand.StartsWith("Aucun") ||
            SelectedAction == null ||
            ExecutionViewModel == null)
        {
            System.Windows.MessageBox.Show(
                "No valid command to execute",
                "Execution Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Determine platform
        var template = SelectedAction.WindowsCommandTemplate ?? SelectedAction.LinuxCommandTemplate;
        var platform = template == SelectedAction.WindowsCommandTemplate ? Platform.Windows : Platform.Linux;
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
                StatusMessage = $"Removed '{SelectedAction.Title}' from favorites";
            }
            else if (!wasFavorite && isFavoriteNow)
            {
                // Successfully added to favorites
                StatusMessage = $"Added '{SelectedAction.Title}' to favorites";
            }
            else if (!wasFavorite && !isFavoriteNow && !result)
            {
                // Failed to add - check if limit reached
                var count = await _favoritesService.GetFavoriteCountAsync();
                if (count >= 50)
                {
                    System.Windows.MessageBox.Show(
                        $"You have reached the maximum limit of 50 favorites ({count}/50). Please remove some favorites before adding new ones.",
                        "Favorites Limit Reached",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            StatusMessage = "Failed to toggle favorite status";
            System.Windows.MessageBox.Show(
                "An error occurred while updating favorites",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
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
            StatusMessage = "An error occurred while processing your request";
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
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                FileName = $"TwinShell-Config-{DateTime.Now:yyyy-MM-dd}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var result = await _configurationService.ExportToJsonAsync(
                    saveFileDialog.FileName,
                    userId: null,
                    includeHistory: true);

                if (result.Success)
                {
                    System.Windows.MessageBox.Show(
                        $"Configuration exported successfully to:\n{saveFileDialog.FileName}",
                        "Export Successful",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Export failed: {result.ErrorMessage}",
                        "Export Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            System.Windows.MessageBox.Show(
                "An error occurred during export",
                "Export Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
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
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Validate file first
                var validation = await _configurationService.ValidateConfigurationFileAsync(openFileDialog.FileName);

                if (!validation.IsValid)
                {
                    System.Windows.MessageBox.Show(
                        $"Invalid configuration file: {validation.ErrorMessage}",
                        "Validation Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Confirm import
                var confirmResult = System.Windows.MessageBox.Show(
                    $"This will import favorites and history from the selected file.\n" +
                    $"Existing data will be preserved (merge mode).\n\n" +
                    $"Configuration Version: {validation.Version}\n\n" +
                    $"Do you want to continue?",
                    "Confirm Import",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (confirmResult == System.Windows.MessageBoxResult.Yes)
                {
                    var result = await _configurationService.ImportFromJsonAsync(
                        openFileDialog.FileName,
                        userId: null,
                        mergeMode: true);

                    if (result.Success)
                    {
                        System.Windows.MessageBox.Show(
                            $"Configuration imported successfully!\n\n" +
                            $"Favorites imported: {result.FavoritesImported}\n" +
                            $"History entries imported: {result.HistoryImported}",
                            "Import Successful",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        // Reload data
                        await LoadActionsAsync();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            $"Import failed: {result.ErrorMessage}",
                            "Import Error",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            System.Windows.MessageBox.Show(
                "An error occurred during import",
                "Import Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
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

    public event Action? ValueChanged;

    partial void OnValueChanged(string value)
    {
        ValueChanged?.Invoke();
    }
}
