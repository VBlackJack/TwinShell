using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Constants;
using TwinShell.Core.Enums;
using TwinShell.Core.Helpers;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for command generation functionality.
/// Handles template-based and example-based command generation.
/// </summary>
public partial class CommandGeneratorViewModel : ObservableObject
{
    private readonly ICommandGeneratorService _commandGeneratorService;
    private readonly ILocalizationService _localizationService;
    private readonly IClipboardService _clipboardService;
    private readonly ICommandHistoryService _commandHistoryService;

    [ObservableProperty]
    private string _generatedCommand = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ParameterViewModel> _commandParameters = new();

    [ObservableProperty]
    private bool _isCommandCrossPlatform;

    [ObservableProperty]
    private Platform _selectedPlatformForGenerator = Platform.Windows;

    [ObservableProperty]
    private CommandExample? _selectedExample;

    [ObservableProperty]
    private bool _isUsingExampleMode;

    [ObservableProperty]
    private ObservableCollection<CommandExample> _currentExamples = new();

    [ObservableProperty]
    private bool _hasPlatformSpecificExamples;

    private string? _exampleCommandPattern;
    private ActionModel? _currentAction;

    public CommandGeneratorViewModel(
        ICommandGeneratorService commandGeneratorService,
        ILocalizationService localizationService,
        IClipboardService clipboardService,
        ICommandHistoryService commandHistoryService)
    {
        _commandGeneratorService = commandGeneratorService;
        _localizationService = localizationService;
        _clipboardService = clipboardService;
        _commandHistoryService = commandHistoryService;
    }

    /// <summary>
    /// Load command generator for the specified action
    /// </summary>
    public void LoadForAction(ActionModel? action)
    {
        _currentAction = action;

        if (action == null)
        {
            CommandParameters.Clear();
            GeneratedCommand = string.Empty;
            IsCommandCrossPlatform = false;
            CurrentExamples.Clear();
            return;
        }

        // Reset example mode when loading new action
        SelectedExample = null;
        IsUsingExampleMode = false;
        _exampleCommandPattern = null;

        // Detect if this is a cross-platform command
        IsCommandCrossPlatform = action.Platform == Platform.Both &&
                                 action.WindowsCommandTemplate != null &&
                                 action.LinuxCommandTemplate != null;

        LoadCommandTemplate();
        UpdateCurrentExamples();
    }

    /// <summary>
    /// Reload when platform selection changes (for cross-platform commands)
    /// </summary>
    public void OnPlatformChanged()
    {
        if (_currentAction != null && IsCommandCrossPlatform)
        {
            LoadCommandTemplate();
            UpdateCurrentExamples();
        }
    }

    private void LoadCommandTemplate()
    {
        if (_currentAction == null) return;

        // Determine which template to use
        CommandTemplate? template;
        if (IsCommandCrossPlatform)
        {
            template = SelectedPlatformForGenerator == Platform.Windows
                ? _currentAction.WindowsCommandTemplate
                : _currentAction.LinuxCommandTemplate;
        }
        else
        {
            template = TemplateHelper.GetActiveTemplate(_currentAction);
        }

        if (!TemplateHelper.IsValidTemplate(template))
        {
            CommandParameters.Clear();
            GeneratedCommand = _localizationService.GetString(MessageKeys.ValidationNoCommandTemplate);
            return;
        }

        // Save current parameter values before clearing
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
                Value = savedValues.TryGetValue(param.Name, out var saved)
                    ? saved
                    : (defaults.TryGetValue(param.Name, out var def) ? def : string.Empty)
            };

            paramVm.ValueChanged += GenerateCommand;
            CommandParameters.Add(paramVm);
        }

        GenerateCommand();
    }

    [RelayCommand]
    private void SelectWindowsPlatform()
    {
        SelectedPlatformForGenerator = Platform.Windows;
        OnPlatformChanged();
    }

    [RelayCommand]
    private void SelectLinuxPlatform()
    {
        SelectedPlatformForGenerator = Platform.Linux;
        OnPlatformChanged();
    }

    [RelayCommand]
    private void GenerateCommand()
    {
        if (_currentAction == null) return;

        CommandTemplate? template;
        if (IsCommandCrossPlatform)
        {
            template = SelectedPlatformForGenerator == Platform.Windows
                ? _currentAction.WindowsCommandTemplate
                : _currentAction.LinuxCommandTemplate;
        }
        else
        {
            template = TemplateHelper.GetActiveTemplate(_currentAction);
        }

        if (!TemplateHelper.IsValidTemplate(template)) return;

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
            _currentAction != null)
        {
            _clipboardService.SetText(GeneratedCommand);
            Services.SnackBarService.Instance.ShowSuccess("✓ Command copied to clipboard");

            // Save to history
            var template = _currentAction.WindowsCommandTemplate ?? _currentAction.LinuxCommandTemplate;
            var platform = template == _currentAction.WindowsCommandTemplate ? Platform.Windows : Platform.Linux;
            var parameters = CommandParameters.ToDictionary(p => p.Name, p => p.Value);

            await _commandHistoryService.AddCommandAsync(
                _currentAction.Id,
                GeneratedCommand,
                parameters,
                platform,
                _currentAction.Title,
                _currentAction.Category);
        }
    }

    [RelayCommand]
    private void ApplyExample(CommandExample? example)
    {
        if (example == null || _currentAction == null) return;

        // Don't apply placeholder examples
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

        ParseExampleAndCreateParameters(example.Command);
        Services.SnackBarService.Instance.ShowSuccess("Example loaded - modify values as needed");
    }

    [RelayCommand]
    private void ResetToTemplate()
    {
        SelectedExample = null;
        IsUsingExampleMode = false;
        _exampleCommandPattern = null;
        LoadCommandTemplate();
    }

    private void UpdateCurrentExamples()
    {
        CurrentExamples.Clear();
        HasPlatformSpecificExamples = false;

        if (_currentAction == null) return;

        var allExamples = new List<CommandExample>();

        if (_currentAction.Examples?.Any() == true)
            allExamples.AddRange(_currentAction.Examples);
        if (_currentAction.WindowsExamples?.Any() == true)
            allExamples.AddRange(_currentAction.WindowsExamples);
        if (_currentAction.LinuxExamples?.Any() == true)
            allExamples.AddRange(_currentAction.LinuxExamples);

        var selectedPlatform = IsCommandCrossPlatform ? SelectedPlatformForGenerator : _currentAction.Platform;

        var filteredExamples = allExamples.Where(ex =>
            ex.Platform == Platform.Both ||
            ex.Platform == selectedPlatform).ToList();

        HasPlatformSpecificExamples = filteredExamples.Any(ex => ex.Platform != Platform.Both);

        foreach (var example in filteredExamples)
        {
            CurrentExamples.Add(example);
        }
    }

    #region Example Parsing

    private void ParseExampleAndCreateParameters(string exampleCommand)
    {
        if (string.IsNullOrWhiteSpace(exampleCommand)) return;

        IsUsingExampleMode = true;
        CommandParameters.Clear();

        if (IsComplexScript(exampleCommand))
        {
            _exampleCommandPattern = "{0}";

            var paramVm = new ParameterViewModel
            {
                Name = "command",
                Label = "Command",
                Type = "multiline",
                Required = false,
                Description = "Edit this command as needed",
                Value = exampleCommand
            };

            paramVm.ValueChanged += GenerateCommandFromExample;
            CommandParameters.Add(paramVm);
            GenerateCommandFromExample();
            return;
        }

        string commandToParse = exampleCommand;
        string pipeSuffix = string.Empty;

        var pipeIndex = exampleCommand.IndexOf('|');
        if (pipeIndex > 0)
        {
            commandToParse = exampleCommand.Substring(0, pipeIndex).Trim();
            pipeSuffix = " " + exampleCommand.Substring(pipeIndex);
        }

        var semicolonIndex = commandToParse.IndexOf(';');
        if (semicolonIndex > 0)
        {
            pipeSuffix = commandToParse.Substring(semicolonIndex) + pipeSuffix;
            commandToParse = commandToParse.Substring(0, semicolonIndex).Trim();
        }

        if (IsComplexScript(commandToParse))
        {
            _exampleCommandPattern = exampleCommand;
            GeneratedCommand = exampleCommand;
            return;
        }

        bool isPowerShell = IsPowerShellCommand(commandToParse);
        var parameters = new List<(string Name, string Label, string Value, bool IsSwitch)>();
        var patternParts = new List<string>();

        if (isPowerShell)
            ParsePowerShellCommand(commandToParse, parameters, patternParts);
        else
            ParseLinuxCommand(commandToParse, parameters, patternParts);

        _exampleCommandPattern = string.Join(" ", patternParts) + pipeSuffix;

        foreach (var (name, label, value, isSwitch) in parameters)
        {
            if (isSwitch) continue;

            var paramVm = new ParameterViewModel
            {
                Name = name,
                Label = label,
                Type = "string",
                Required = false,
                Description = $"Value for {label}",
                Value = value
            };

            paramVm.ValueChanged += GenerateCommandFromExample;
            CommandParameters.Add(paramVm);
        }

        GenerateCommandFromExample();
    }

    private void GenerateCommandFromExample()
    {
        if (string.IsNullOrEmpty(_exampleCommandPattern)) return;

        try
        {
            var values = CommandParameters.Select(p => p.Value).ToArray();
            GeneratedCommand = string.Format(_exampleCommandPattern, values);
        }
        catch
        {
            GeneratedCommand = _exampleCommandPattern;
        }
    }

    private bool IsComplexScript(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;

        var trimmed = command.TrimStart();

        // PowerShell script patterns
        if (trimmed.StartsWith("foreach", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("for ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("while", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("if ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("if(", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("switch", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("try", StringComparison.OrdinalIgnoreCase))
            return true;

        // Pipeline cmdlets with script blocks
        if ((command.Contains("ForEach-Object", StringComparison.OrdinalIgnoreCase) ||
             command.Contains("Where-Object", StringComparison.OrdinalIgnoreCase)) &&
            command.Contains("{") && command.Contains("}"))
            return true;

        // Range operator
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\.\d+"))
            return true;

        // Variable assignment with semicolon
        if (trimmed.StartsWith("$") && trimmed.Contains(";"))
            return true;

        // powershell -Command
        if (trimmed.StartsWith("powershell", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("-Command"))
            return true;

        // Linux watch/for loops
        if (trimmed.StartsWith("watch ", StringComparison.OrdinalIgnoreCase))
            return true;
        if (trimmed.StartsWith("for ", StringComparison.Ordinal) && trimmed.Contains("; do"))
            return true;

        // Multiple pipes with braces
        int pipeCount = command.Count(c => c == '|');
        int braceCount = command.Count(c => c == '{');
        if (pipeCount >= 2 && braceCount >= 1)
            return true;

        return false;
    }

    private bool IsPowerShellCommand(string command)
    {
        var verbs = new[] { "Get-", "Set-", "New-", "Remove-", "Resolve-", "Test-", "Start-", "Stop-",
                           "Clear-", "Invoke-", "Add-", "Enable-", "Disable-", "Copy-", "Move-",
                           "Rename-", "Export-", "Import-", "Backup-", "Restore-", "Update-",
                           "Install-", "Uninstall-", "Register-", "Unregister-", "Show-", "Hide-" };

        return verbs.Any(v => command.StartsWith(v, StringComparison.OrdinalIgnoreCase));
    }

    private void ParsePowerShellCommand(string command, List<(string Name, string Label, string Value, bool IsSwitch)> parameters, List<string> patternParts)
    {
        var parts = SplitCommandPreservingQuotes(command);
        if (parts.Count == 0) return;

        patternParts.Add(parts[0]);
        int i = 1;
        int paramIndex = 0;

        if (i < parts.Count && !parts[i].StartsWith("-"))
        {
            parameters.Add(("target", "Target", parts[i], false));
            patternParts.Add($"{{{paramIndex}}}");
            paramIndex++;
            i++;
        }

        while (i < parts.Count)
        {
            if (parts[i].StartsWith("-"))
            {
                var paramName = parts[i].TrimStart('-');

                if (i + 1 < parts.Count && !parts[i + 1].StartsWith("-"))
                {
                    parameters.Add((paramName, paramName, parts[i + 1], false));
                    patternParts.Add($"-{paramName} {{{paramIndex}}}");
                    paramIndex++;
                    i += 2;
                }
                else
                {
                    parameters.Add((paramName, paramName, "true", true));
                    patternParts.Add($"-{paramName}");
                    i++;
                }
            }
            else
            {
                parameters.Add(($"arg{paramIndex}", $"Argument {paramIndex + 1}", parts[i], false));
                patternParts.Add($"{{{paramIndex}}}");
                paramIndex++;
                i++;
            }
        }
    }

    private void ParseLinuxCommand(string command, List<(string Name, string Label, string Value, bool IsSwitch)> parameters, List<string> patternParts)
    {
        var parts = SplitCommandPreservingQuotes(command);
        if (parts.Count == 0) return;

        patternParts.Add(parts[0]);
        int paramIndex = 0;

        for (int i = 1; i < parts.Count; i++)
        {
            var part = parts[i];

            if (part.StartsWith("-") && part.Length > 1)
            {
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
                    patternParts.Add(part);
                }
            }
            else
            {
                var label = paramIndex == 0 ? "Target" : $"Argument {paramIndex + 1}";
                parameters.Add(($"arg{paramIndex}", label, part, false));
                patternParts.Add($"{{{paramIndex}}}");
                paramIndex++;
            }
        }
    }

    private List<string> SplitCommandPreservingQuotes(string command)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        char quoteChar = '"';
        int braceDepth = 0;
        int parenDepth = 0;

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

    #endregion

    /// <summary>
    /// Get execution parameters for the current command
    /// </summary>
    public ExecuteCommandParameter? GetExecutionParameter()
    {
        var validationErrorsText = _localizationService.GetString(MessageKeys.ValidationErrors);
        var noTemplateText = _localizationService.GetString(MessageKeys.ValidationNoCommandTemplate);

        if (string.IsNullOrWhiteSpace(GeneratedCommand) ||
            GeneratedCommand.StartsWith(validationErrorsText) ||
            GeneratedCommand.StartsWith(noTemplateText) ||
            _currentAction == null)
        {
            return null;
        }

        var template = TemplateHelper.GetActiveTemplate(_currentAction);
        var platform = TemplateHelper.GetPlatformForTemplate(_currentAction, template!);
        var parameters = CommandParameters.ToDictionary(p => p.Name, p => p.Value);

        return new ExecuteCommandParameter
        {
            Command = GeneratedCommand,
            Platform = platform,
            IsDangerous = _currentAction.Level == CriticalityLevel.Dangerous,
            RequireConfirmation = true,
            ActionId = _currentAction.Id,
            ActionTitle = _currentAction.Title,
            Category = _currentAction.Category,
            Parameters = parameters
        };
    }
}
