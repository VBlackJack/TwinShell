using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MainViewModel> _logger;
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

    /// <summary>
    /// Command to select Windows platform for cross-platform actions
    /// </summary>
    [RelayCommand]
    private void SelectWindowsPlatform()
    {
        SelectedPlatformForGenerator = Platform.Windows;
    }

    /// <summary>
    /// Command to select Linux platform for cross-platform actions
    /// </summary>
    [RelayCommand]
    private void SelectLinuxPlatform()
    {
        SelectedPlatformForGenerator = Platform.Linux;
    }

    /// <summary>
    /// Currently selected example (for highlighting in UI)
    /// </summary>
    [ObservableProperty]
    private CommandExample? _selectedExample;

    /// <summary>
    /// Flag indicating if we're currently using an example-based generator
    /// </summary>
    [ObservableProperty]
    private bool _isUsingExampleMode;

    /// <summary>
    /// The command pattern extracted from the example (e.g., "Resolve-DnsName {0} -Type {1} -Server {2}")
    /// </summary>
    private string? _exampleCommandPattern;

    /// <summary>
    /// Apply an example to the command generator.
    /// Parses the example and creates dynamic parameters based on its structure.
    /// For complex commands (pipes, scripts), copies directly to clipboard instead.
    /// </summary>
    [RelayCommand]
    private void ApplyExample(CommandExample? example)
    {
        if (example == null || SelectedAction == null)
            return;

        // Don't apply placeholder examples (e.g., "<domainName> <recordType>")
        if (example.Command.Contains("<") && example.Command.Contains(">"))
        {
            Services.SnackBarService.Instance.ShowWarning("Template example - fill parameters manually");
            return;
        }

        SelectedExample = example;

        // Switch platform if example has a specific platform
        if (example.Platform != Platform.Both && IsCommandCrossPlatform)
        {
            SelectedPlatformForGenerator = example.Platform;
        }

        // Parse the example and create dynamic parameters
        ParseExampleAndCreateParameters(example.Command);

        // Show feedback
        Services.SnackBarService.Instance.ShowSuccess("Example loaded - modify values as needed");
    }


    /// <summary>
    /// Parse an example command and create dynamic parameters based on its structure.
    /// Handles PowerShell-style commands (Verb-Noun value -Param value) and Linux commands.
    /// For complex commands with pipes, parses the first command and preserves the rest.
    /// For script-like commands (foreach, if, while, etc.), displays as-is without parsing.
    /// </summary>
    private void ParseExampleAndCreateParameters(string exampleCommand)
    {
        if (string.IsNullOrWhiteSpace(exampleCommand))
            return;

        IsUsingExampleMode = true;
        CommandParameters.Clear();

        // Check if this is a complex script that shouldn't be parsed
        if (IsComplexScript(exampleCommand))
        {
            // Create a single editable "Command" field for complex scripts
            _exampleCommandPattern = "{0}"; // Simple placeholder for the whole command

            var paramVm = new ParameterViewModel
            {
                Name = "command",
                Label = "Command",
                Type = "multiline",
                Required = false,
                Description = "Edit this command as needed",
                Value = exampleCommand
            };

            paramVm.ValueChanged += () => GenerateCommandFromExample();
            CommandParameters.Add(paramVm);

            GenerateCommandFromExample();
            return;
        }

        // Check for pipes - parse first command, keep rest as suffix
        string commandToParse = exampleCommand;
        string pipeSuffix = string.Empty;

        var pipeIndex = exampleCommand.IndexOf('|');
        if (pipeIndex > 0)
        {
            commandToParse = exampleCommand.Substring(0, pipeIndex).Trim();
            pipeSuffix = " " + exampleCommand.Substring(pipeIndex); // Keep the | and everything after
        }

        // Check for semicolons - parse first command, keep rest as suffix
        var semicolonIndex = commandToParse.IndexOf(';');
        if (semicolonIndex > 0)
        {
            pipeSuffix = commandToParse.Substring(semicolonIndex) + pipeSuffix;
            commandToParse = commandToParse.Substring(0, semicolonIndex).Trim();
        }

        // If the first part is also a complex script, don't parse
        if (IsComplexScript(commandToParse))
        {
            _exampleCommandPattern = exampleCommand;
            GeneratedCommand = exampleCommand;
            return;
        }

        // Detect if it's a PowerShell command or Linux command
        bool isPowerShell = IsPowerShellCommand(commandToParse);

        var parameters = new List<(string Name, string Label, string Value, bool IsSwitch)>();
        var patternParts = new List<string>();

        if (isPowerShell)
        {
            ParsePowerShellCommand(commandToParse, parameters, patternParts);
        }
        else
        {
            ParseLinuxCommand(commandToParse, parameters, patternParts);
        }

        // Store the pattern for regeneration (with pipe suffix if any)
        _exampleCommandPattern = string.Join(" ", patternParts) + pipeSuffix;

        // Create parameter view models
        foreach (var (name, label, value, isSwitch) in parameters)
        {
            if (isSwitch) continue; // Skip switches for now, they're in the pattern

            var paramVm = new ParameterViewModel
            {
                Name = name,
                Label = label,
                Type = "string",
                Required = false,
                Description = $"Value for {label}",
                Value = value
            };

            paramVm.ValueChanged += () => GenerateCommandFromExample();
            CommandParameters.Add(paramVm);
        }

        // Generate the initial command
        GenerateCommandFromExample();
    }

    /// <summary>
    /// Check if a command is a complex script that shouldn't be parsed into parameters.
    /// These include: foreach loops, if statements, while loops, variable assignments with scripts,
    /// powershell -Command with embedded scripts, watch commands, etc.
    /// </summary>
    private bool IsComplexScript(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var trimmed = command.TrimStart();

        // PowerShell script patterns - statements
        if (trimmed.StartsWith("foreach", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("for ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("while", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("if ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("if(", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("switch", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("try", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // PowerShell pipeline cmdlets with script blocks (ForEach-Object, Where-Object, etc.)
        if (command.Contains("ForEach-Object", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Where-Object", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Select-Object", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Sort-Object", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Group-Object", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Measure-Object", StringComparison.OrdinalIgnoreCase))
        {
            // If it has script blocks, it's complex
            if (command.Contains("{") && command.Contains("}"))
            {
                return true;
            }
        }

        // PowerShell range operator (1..100, $start..$end)
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\.\d+"))
        {
            return true;
        }

        // Variable assignment followed by complex logic (semicolon indicates multi-statement script)
        if (trimmed.StartsWith("$") && trimmed.Contains(";"))
        {
            return true;
        }

        // Commands starting with powercfg followed by complex logic
        if (trimmed.StartsWith("powercfg", StringComparison.OrdinalIgnoreCase) && trimmed.Contains(";"))
        {
            return true;
        }

        // Register-ScheduledTask with embedded XML/script
        if (command.Contains("Register-ScheduledTask", StringComparison.OrdinalIgnoreCase) &&
            (command.Contains("(Get-Content") || command.Contains("-Xml")))
        {
            return true;
        }

        // powershell -Command "..." with embedded script
        if (trimmed.StartsWith("powershell", StringComparison.OrdinalIgnoreCase) &&
            trimmed.Contains("-Command"))
        {
            return true;
        }

        // Linux watch command with embedded command
        if (trimmed.StartsWith("watch ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Linux for/while loops
        if (trimmed.StartsWith("for ", StringComparison.Ordinal) && trimmed.Contains("; do"))
        {
            return true;
        }

        // Commands with script blocks containing multiple pipes
        int pipeCount = command.Count(c => c == '|');
        int braceCount = command.Count(c => c == '{');
        if (pipeCount >= 2 && braceCount >= 1)
        {
            return true;
        }

        // Commands with unbalanced quotes/braces (complex multi-part scripts)
        int braceOpen = command.Count(c => c == '{');
        int braceClose = command.Count(c => c == '}');
        int parenOpen = command.Count(c => c == '(');
        int parenClose = command.Count(c => c == ')');

        // If things are unbalanced, it's likely a complex script
        if (braceOpen != braceClose || parenOpen != parenClose)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if command looks like a PowerShell command
    /// </summary>
    private bool IsPowerShellCommand(string command)
    {
        var verbs = new[] { "Get-", "Set-", "New-", "Remove-", "Resolve-", "Test-", "Start-", "Stop-",
                           "Clear-", "Invoke-", "Add-", "Enable-", "Disable-", "Copy-", "Move-",
                           "Rename-", "Export-", "Import-", "Backup-", "Restore-", "Update-",
                           "Install-", "Uninstall-", "Register-", "Unregister-", "Show-", "Hide-" };

        return verbs.Any(v => command.StartsWith(v, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parse PowerShell command: CommandName Target -Param1 Value1 -Param2 Value2 -Switch
    /// </summary>
    private void ParsePowerShellCommand(string command, List<(string Name, string Label, string Value, bool IsSwitch)> parameters, List<string> patternParts)
    {
        var parts = SplitCommandPreservingQuotes(command);
        if (parts.Count == 0) return;

        // First part is the command name
        patternParts.Add(parts[0]);

        int i = 1;
        int paramIndex = 0;

        // Check for positional argument (target) before any -Parameter
        if (i < parts.Count && !parts[i].StartsWith("-"))
        {
            parameters.Add(("target", "Target", parts[i], false));
            patternParts.Add($"{{{paramIndex}}}");
            paramIndex++;
            i++;
        }

        // Parse named parameters
        while (i < parts.Count)
        {
            if (parts[i].StartsWith("-"))
            {
                var paramName = parts[i].TrimStart('-');

                // Check if next part is a value or another parameter (switch)
                if (i + 1 < parts.Count && !parts[i + 1].StartsWith("-"))
                {
                    // Parameter with value
                    parameters.Add((paramName, paramName, parts[i + 1], false));
                    patternParts.Add($"-{paramName} {{{paramIndex}}}");
                    paramIndex++;
                    i += 2;
                }
                else
                {
                    // Switch parameter (no value)
                    parameters.Add((paramName, paramName, "true", true));
                    patternParts.Add($"-{paramName}");
                    i++;
                }
            }
            else
            {
                // Unexpected positional argument
                parameters.Add(($"arg{paramIndex}", $"Argument {paramIndex + 1}", parts[i], false));
                patternParts.Add($"{{{paramIndex}}}");
                paramIndex++;
                i++;
            }
        }
    }

    /// <summary>
    /// Parse Linux command: command arg1 arg2 -flag value
    /// </summary>
    private void ParseLinuxCommand(string command, List<(string Name, string Label, string Value, bool IsSwitch)> parameters, List<string> patternParts)
    {
        var parts = SplitCommandPreservingQuotes(command);
        if (parts.Count == 0) return;

        patternParts.Add(parts[0]); // Command name

        int paramIndex = 0;
        for (int i = 1; i < parts.Count; i++)
        {
            var part = parts[i];

            if (part.StartsWith("-") && part.Length > 1)
            {
                // Check if it's a flag with value
                if (i + 1 < parts.Count && !parts[i + 1].StartsWith("-"))
                {
                    var flagName = part.TrimStart('-');
                    parameters.Add((flagName, flagName, parts[i + 1], false));
                    patternParts.Add($"{part} {{{paramIndex}}}");
                    paramIndex++;
                    i++;
                }
                else
                {
                    // Just a flag
                    patternParts.Add(part);
                }
            }
            else
            {
                // Positional argument
                var label = paramIndex == 0 ? "Target" : $"Argument {paramIndex + 1}";
                parameters.Add(($"arg{paramIndex}", label, part, false));
                patternParts.Add($"{{{paramIndex}}}");
                paramIndex++;
            }
        }
    }

    /// <summary>
    /// Split a command string preserving quoted strings, script blocks, and subexpressions
    /// </summary>
    private List<string> SplitCommandPreservingQuotes(string command)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        char quoteChar = '"';
        int braceDepth = 0;  // Track nested braces {}
        int parenDepth = 0;  // Track nested parentheses ()

        foreach (var c in command)
        {
            if ((c == '"' || c == '\'') && !inQuotes && braceDepth == 0 && parenDepth == 0)
            {
                inQuotes = true;
                quoteChar = c;
                current.Append(c);
            }
            else if (c == quoteChar && inQuotes)
            {
                inQuotes = false;
                current.Append(c);
            }
            else if (c == '{' && !inQuotes)
            {
                braceDepth++;
                current.Append(c);
            }
            else if (c == '}' && !inQuotes)
            {
                braceDepth = Math.Max(0, braceDepth - 1);
                current.Append(c);
            }
            else if (c == '(' && !inQuotes)
            {
                parenDepth++;
                current.Append(c);
            }
            else if (c == ')' && !inQuotes)
            {
                parenDepth = Math.Max(0, parenDepth - 1);
                current.Append(c);
            }
            else if (c == ' ' && !inQuotes && braceDepth == 0 && parenDepth == 0)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts;
    }

    /// <summary>
    /// Generate command from example pattern with current parameter values
    /// </summary>
    private void GenerateCommandFromExample()
    {
        if (string.IsNullOrEmpty(_exampleCommandPattern))
            return;

        try
        {
            var values = CommandParameters.Select(p => p.Value).ToArray();
            GeneratedCommand = string.Format(_exampleCommandPattern, values);
        }
        catch
        {
            // If format fails, just show the pattern
            GeneratedCommand = _exampleCommandPattern;
        }
    }

    /// <summary>
    /// Reset to template-based generator (exit example mode)
    /// </summary>
    [RelayCommand]
    private void ResetToTemplate()
    {
        SelectedExample = null;
        IsUsingExampleMode = false;
        _exampleCommandPattern = null;
        LoadCommandGenerator();
    }

    [ObservableProperty]
    private int _searchResultCount;

    [ObservableProperty]
    private string _searchTime = string.Empty;

    [ObservableProperty]
    private bool _showSearchMetrics;

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

    /// <summary>
    /// Currently displayed examples based on platform selection
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CommandExample> _currentExamples = new();

    /// <summary>
    /// Indicates if the current examples are platform-specific (not generic fallback)
    /// Used to show/hide platform badge in UI
    /// </summary>
    [ObservableProperty]
    private bool _hasPlatformSpecificExamples;

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
        IServiceProvider serviceProvider,
        ILogger<MainViewModel> logger)
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
        _logger = logger;
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
            UpdateCurrentExamples();
        }
        else
        {
            CurrentExamples.Clear();
        }
    }

    partial void OnSelectedPlatformForGeneratorChanged(Platform value)
    {
        // Regenerate command and examples when platform selection changes
        if (SelectedAction != null && IsCommandCrossPlatform)
        {
            LoadCommandGenerator();
            UpdateCurrentExamples();
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
                // BUGFIX: Capture search text to avoid closure issues with property changes
                var searchTextCopy = SearchText;
                var resultCount = results.Count;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _searchHistoryService.AddSearchAsync(searchTextCopy, resultCount);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't disrupt user experience
                        System.Diagnostics.Debug.WriteLine($"Search history save failed: {ex.Message}");
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

    /// <summary>
    /// Updates the current examples based on selected action and platform.
    /// Filters examples by Platform property: shows examples where Platform == Both OR Platform == SelectedPlatform
    /// </summary>
    private void UpdateCurrentExamples()
    {
        CurrentExamples.Clear();
        HasPlatformSpecificExamples = false;

        if (SelectedAction == null)
        {
            return;
        }

        // Collect all examples from all sources
        var allExamples = new List<CommandExample>();

        if (SelectedAction.Examples?.Any() == true)
        {
            allExamples.AddRange(SelectedAction.Examples);
        }

        if (SelectedAction.WindowsExamples?.Any() == true)
        {
            allExamples.AddRange(SelectedAction.WindowsExamples);
        }

        if (SelectedAction.LinuxExamples?.Any() == true)
        {
            allExamples.AddRange(SelectedAction.LinuxExamples);
        }

        // Filter examples based on selected platform
        // Show examples where Platform == Both OR Platform == SelectedPlatform
        var selectedPlatform = IsCommandCrossPlatform ? SelectedPlatformForGenerator : SelectedAction.Platform;

        var filteredExamples = allExamples.Where(ex =>
            ex.Platform == Platform.Both ||
            ex.Platform == selectedPlatform).ToList();

        // Check if we have platform-specific examples (not just generic Both)
        HasPlatformSpecificExamples = filteredExamples.Any(ex => ex.Platform != Platform.Both);

        foreach (var example in filteredExamples)
        {
            CurrentExamples.Add(example);
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
        if (template == null) return;

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
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error toggling favorite for action: {ActionId}", SelectedAction?.Id);
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
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error in SafeExecuteAsync");
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
                var successMessage = $"✓ {_localizationService.GetString("MessageActionCreated")}";
                StatusMessage = successMessage;
                Services.SnackBarService.Instance.ShowSuccess(successMessage);
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error creating new action");
            _dialogService.ShowError(
                _localizationService.GetString("MessageActionCreateError"),
                _localizationService.GetString("DialogTitleError"));
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
                var successMessage = $"✓ {_localizationService.GetString("MessageActionUpdated")}";
                StatusMessage = successMessage;
                Services.SnackBarService.Instance.ShowSuccess(successMessage);
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error updating action: {ActionId}", SelectedAction?.Id);
            _dialogService.ShowError(
                _localizationService.GetString("MessageActionUpdateError"),
                _localizationService.GetString("DialogTitleError"));
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
            ? _localizationService.GetFormattedString("MessageConfirmDeleteAction", SelectedAction.Title)
            : _localizationService.GetFormattedString("MessageConfirmDeleteSystemAction", SelectedAction.Title);

        var confirmed = _dialogService.ShowQuestion(warningMessage, _localizationService.GetString("DialogTitleConfirmDelete"));

        if (confirmed)
        {
            try
            {
                await _actionService.DeleteActionAsync(SelectedAction.Id);
                await LoadActionsAsync();
                var successMessage = $"✓ {_localizationService.GetString("MessageActionDeleted")}";
                StatusMessage = successMessage;
                Services.SnackBarService.Instance.ShowSuccess(successMessage);
            }
            catch (Exception ex)
            {
                // Log exception for debugging, but don't expose details to users
                _logger.LogError(ex, "Error deleting action: {ActionId}", SelectedAction?.Id);
                _dialogService.ShowError(
                    _localizationService.GetString("MessageActionDeleteError"),
                    _localizationService.GetString("DialogTitleError"));
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
                        _localizationService.GetFormattedString("MessageActionsExportedTo", result.ActionCount, fileName),
                        _localizationService.GetString("DialogTitleExportSuccess"));
                }
                else
                {
                    _dialogService.ShowError(
                        _localizationService.GetFormattedString("MessageExportErrorDetails", result.ErrorMessage ?? "Unknown"),
                        _localizationService.GetString("DialogTitleExportFailed"));
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error exporting actions");
            _dialogService.ShowError(
                _localizationService.GetString("MessageExportError"),
                _localizationService.GetString("DialogTitleError"));
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
                        _localizationService.GetFormattedString("MessageInvalidFile", validation.ErrorMessage ?? "Unknown"),
                        _localizationService.GetString("DialogTitleValidationFailed"));
                    return;
                }

                // Ask user for import mode
                var mergeMode = _dialogService.ShowQuestion(
                    _localizationService.GetFormattedString("MessageImportModeChoice", validation.ActionCount),
                    _localizationService.GetString("DialogTitleChooseImportMode"));

                var mode = mergeMode ? ImportMode.Merge : ImportMode.Replace;

                // Confirm if Replace mode
                if (mode == ImportMode.Replace)
                {
                    var confirmReplace = _dialogService.ShowQuestion(
                        _localizationService.GetString("MessageConfirmReplace"),
                        _localizationService.GetString("DialogTitleConfirmReplace"));

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
                        _localizationService.GetFormattedString("MessageImportCompleted", result.Imported, result.Updated, result.Skipped),
                        _localizationService.GetString("DialogTitleImportSuccess"));
                }
                else
                {
                    _dialogService.ShowError(
                        _localizationService.GetFormattedString("MessageImportErrorDetails", result.ErrorMessage ?? "Unknown"),
                        _localizationService.GetString("DialogTitleImportFailed"));
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error importing actions");
            _dialogService.ShowError(
                _localizationService.GetString("MessageImportError"),
                _localizationService.GetString("DialogTitleError"));
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
                        _localizationService.GetFormattedString(MessageKeys.ConfigExportErrorMessage, result.ErrorMessage ?? "Unknown error"),
                        _localizationService.GetString(MessageKeys.ConfigExportError));
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error exporting configuration");
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
                        _localizationService.GetFormattedString(MessageKeys.ConfigValidationErrorMessage, validation.ErrorMessage ?? "Unknown error"),
                        _localizationService.GetString(MessageKeys.ConfigValidationError));
                    return;
                }

                // Confirm import
                var confirmed = _dialogService.ShowQuestion(
                    _localizationService.GetFormattedString(MessageKeys.ConfigImportConfirmationMessage, validation.Version ?? "Unknown"),
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
                            _localizationService.GetFormattedString(MessageKeys.ConfigImportErrorMessage, result.ErrorMessage ?? "Unknown error"),
                            _localizationService.GetString(MessageKeys.ConfigImportError));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception for debugging, but don't expose details to users
            _logger.LogError(ex, "Error importing configuration");
            _dialogService.ShowError(
                _localizationService.GetString(MessageKeys.ConfigImportErrorGeneric),
                _localizationService.GetString(MessageKeys.ConfigImportError));
        }
    }

    /// <summary>
    /// Switch to light theme
    /// </summary>
    [RelayCommand]
    private async Task SetLightTheme()
    {
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

        themeService.ApplyTheme(Theme.Light);

        var settings = await settingsService.LoadSettingsAsync();
        settings.Theme = Theme.Light;
        await settingsService.SaveSettingsAsync(settings);
    }

    /// <summary>
    /// Switch to dark theme
    /// </summary>
    [RelayCommand]
    private async Task SetDarkTheme()
    {
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

        themeService.ApplyTheme(Theme.Dark);

        var settings = await settingsService.LoadSettingsAsync();
        settings.Theme = Theme.Dark;
        await settingsService.SaveSettingsAsync(settings);
    }

}

public partial class ParameterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private bool _required;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public event System.Action? ValueChanged;

    partial void OnValueChanged(string value)
    {
        ValueChanged?.Invoke();
    }
}
