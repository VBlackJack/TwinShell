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
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnFilterWindowsChanged(bool value) => _ = ApplyFiltersAsync();
    partial void OnFilterLinuxChanged(bool value) => _ = ApplyFiltersAsync();
    partial void OnFilterBothChanged(bool value) => _ = ApplyFiltersAsync();
    partial void OnFilterInfoChanged(bool value) => _ = ApplyFiltersAsync();
    partial void OnFilterRunChanged(bool value) => _ = ApplyFiltersAsync();
    partial void OnFilterDangerousChanged(bool value) => _ = ApplyFiltersAsync();
    partial void OnShowFavoritesOnlyChanged(bool value) => _ = ApplyFiltersAsync();

    partial void OnSelectedActionChanged(Action? value)
    {
        if (value != null)
        {
            LoadCommandGenerator();
        }
    }

    private async Task ApplyFiltersAsync()
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

        var result = await _favoritesService.ToggleFavoriteAsync(SelectedAction.Id);

        // Reload favorites
        var favorites = await _favoritesService.GetAllFavoritesAsync();
        _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();

        // Refresh the filtered list
        await ApplyFiltersAsync();

        // Show notification if limit reached
        if (!result)
        {
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

    /// <summary>
    /// Check if the selected action is a favorite
    /// </summary>
    public bool IsSelectedActionFavorite()
    {
        return SelectedAction != null && _favoriteActionIds.Contains(SelectedAction.Id);
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
            System.Windows.MessageBox.Show(
                $"An error occurred: {ex.Message}",
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
            System.Windows.MessageBox.Show(
                $"An error occurred: {ex.Message}",
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
