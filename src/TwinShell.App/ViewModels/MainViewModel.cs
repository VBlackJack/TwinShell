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

    private List<Action> _allActions = new();

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
    private string _generatedCommand = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ParameterViewModel> _commandParameters = new();

    public MainViewModel(
        IActionService actionService,
        ISearchService searchService,
        ICommandGeneratorService commandGeneratorService,
        IClipboardService clipboardService)
    {
        _actionService = actionService;
        _searchService = searchService;
        _commandGeneratorService = commandGeneratorService;
        _clipboardService = clipboardService;
    }

    public async Task InitializeAsync()
    {
        await LoadActionsAsync();
    }

    private async Task LoadActionsAsync()
    {
        _allActions = (await _actionService.GetAllActionsAsync()).ToList();

        var categories = await _actionService.GetAllCategoriesAsync();
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

        // Category filter
        if (!string.IsNullOrEmpty(SelectedCategory))
        {
            filtered = filtered.Where(a => a.Category == SelectedCategory);
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
    private void CopyCommand()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedCommand) &&
            !GeneratedCommand.StartsWith("Erreurs") &&
            !GeneratedCommand.StartsWith("Aucun"))
        {
            _clipboardService.SetText(GeneratedCommand);
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
