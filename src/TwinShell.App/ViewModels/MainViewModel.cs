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

    public MainViewModel(
        IActionService actionService,
        ISearchService searchService,
        ICommandGeneratorService commandGeneratorService,
        IClipboardService clipboardService,
        ICommandHistoryService commandHistoryService,
        IFavoritesService favoritesService)
    {
        _actionService = actionService;
        _searchService = searchService;
        _commandGeneratorService = commandGeneratorService;
        _clipboardService = clipboardService;
        _commandHistoryService = commandHistoryService;
        _favoritesService = favoritesService;
    }

    public async Task InitializeAsync()
    {
        await LoadActionsAsync();
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
