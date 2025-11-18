using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for creating and editing actions
/// </summary>
public partial class ActionEditorViewModel : ObservableObject
{
    private readonly IActionService _actionService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty] private string _id = Guid.NewGuid().ToString();
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private Platform _selectedPlatform = Platform.Windows;
    [ObservableProperty] private CriticalityLevel _selectedLevel = CriticalityLevel.Info;
    [ObservableProperty] private string _windowsCommandName = string.Empty;
    [ObservableProperty] private string _windowsCommandPattern = string.Empty;
    [ObservableProperty] private string _linuxCommandName = string.Empty;
    [ObservableProperty] private string _linuxCommandPattern = string.Empty;
    [ObservableProperty] private string _tags = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    [ObservableProperty] private ObservableCollection<ParameterEditorViewModel> _windowsParameters = new();
    [ObservableProperty] private ObservableCollection<ParameterEditorViewModel> _linuxParameters = new();

    [ObservableProperty] private ObservableCollection<string> _availableCategories = new();

    [ObservableProperty] private string? _validationError;

    [ObservableProperty] private ObservableCollection<ExampleEditorViewModel> _examples = new();

    private bool _isEditMode;
    private ActionModel? _originalAction;

    /// <summary>
    /// Dialog result (true if saved, false if cancelled)
    /// </summary>
    [ObservableProperty] private bool? _dialogResult;

    public ActionEditorViewModel(
        IActionService actionService,
        ILocalizationService localizationService)
    {
        _actionService = actionService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Initialize for creating a new action
    /// </summary>
    public async Task InitializeForNewActionAsync()
    {
        _isEditMode = false;
        await LoadCategoriesAsync();
    }

    /// <summary>
    /// Initialize for editing an existing action
    /// </summary>
    public async Task InitializeForEditActionAsync(ActionModel action)
    {
        _isEditMode = true;
        _originalAction = action;

        await LoadCategoriesAsync();

        // Load action data
        Id = action.Id;
        Title = action.Title;
        Description = action.Description ?? string.Empty;
        Category = action.Category;
        SelectedPlatform = action.Platform;
        SelectedLevel = action.Level;
        Tags = action.Tags != null ? string.Join(", ", action.Tags) : string.Empty;
        Notes = action.Notes ?? string.Empty;

        // Load Windows command template
        if (action.WindowsCommandTemplate != null)
        {
            WindowsCommandName = action.WindowsCommandTemplate.Name;
            WindowsCommandPattern = action.WindowsCommandTemplate.CommandPattern;

            if (action.WindowsCommandTemplate.Parameters != null)
            {
                WindowsParameters.Clear();
                foreach (var param in action.WindowsCommandTemplate.Parameters)
                {
                    WindowsParameters.Add(new ParameterEditorViewModel
                    {
                        Name = param.Name,
                        Label = param.Label,
                        Type = param.Type,
                        DefaultValue = param.DefaultValue ?? string.Empty,
                        Required = param.Required,
                        Description = param.Description ?? string.Empty
                    });
                }
            }
        }

        // Load Linux command template
        if (action.LinuxCommandTemplate != null)
        {
            LinuxCommandName = action.LinuxCommandTemplate.Name;
            LinuxCommandPattern = action.LinuxCommandTemplate.CommandPattern;

            if (action.LinuxCommandTemplate.Parameters != null)
            {
                LinuxParameters.Clear();
                foreach (var param in action.LinuxCommandTemplate.Parameters)
                {
                    LinuxParameters.Add(new ParameterEditorViewModel
                    {
                        Name = param.Name,
                        Label = param.Label,
                        Type = param.Type,
                        DefaultValue = param.DefaultValue ?? string.Empty,
                        Required = param.Required,
                        Description = param.Description ?? string.Empty
                    });
                }
            }
        }

        // Load examples
        if (action.Examples != null)
        {
            Examples.Clear();
            foreach (var example in action.Examples)
            {
                Examples.Add(new ExampleEditorViewModel
                {
                    Command = example.Command,
                    Description = example.Description ?? string.Empty
                });
            }
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _actionService.GetAllCategoriesAsync();
        AvailableCategories = new ObservableCollection<string>(categories);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // BUGFIX: Validate all required fields before saving
        if (!ValidateInput())
        {
            return;
        }

        try
        {
            var action = new ActionModel
            {
                Id = Id,
                Title = Title.Trim(),
                Description = Description.Trim(),
                Category = Category.Trim(),
                Platform = SelectedPlatform,
                Level = SelectedLevel,
                IsUserCreated = true,
                Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList(),
                Notes = Notes.Trim(),
                Examples = Examples.Select(e => new CommandExample
                {
                    Command = e.Command,
                    Description = e.Description
                }).ToList(),
                Links = new List<ExternalLink>()
            };

            // Create Windows command template if applicable
            if (SelectedPlatform == Platform.Windows || SelectedPlatform == Platform.Both)
            {
                action.WindowsCommandTemplateId = $"{Id}-win";
                action.WindowsCommandTemplate = new CommandTemplate
                {
                    Id = $"{Id}-win",
                    Platform = Platform.Windows,
                    Name = WindowsCommandName.Trim(),
                    CommandPattern = WindowsCommandPattern.Trim(),
                    Parameters = WindowsParameters.Select(p => new TemplateParameter
                    {
                        Name = p.Name,
                        Label = p.Label,
                        Type = p.Type,
                        DefaultValue = p.DefaultValue,
                        Required = p.Required,
                        Description = p.Description
                    }).ToList()
                };
            }

            // Create Linux command template if applicable
            if (SelectedPlatform == Platform.Linux || SelectedPlatform == Platform.Both)
            {
                action.LinuxCommandTemplateId = $"{Id}-linux";
                action.LinuxCommandTemplate = new CommandTemplate
                {
                    Id = $"{Id}-linux",
                    Platform = Platform.Linux,
                    Name = LinuxCommandName.Trim(),
                    CommandPattern = LinuxCommandPattern.Trim(),
                    Parameters = LinuxParameters.Select(p => new TemplateParameter
                    {
                        Name = p.Name,
                        Label = p.Label,
                        Type = p.Type,
                        DefaultValue = p.DefaultValue,
                        Required = p.Required,
                        Description = p.Description
                    }).ToList()
                };
            }

            if (_isEditMode && _originalAction != null)
            {
                // Preserve creation date when updating
                action.CreatedAt = _originalAction.CreatedAt;
                action.UpdatedAt = DateTime.UtcNow;
                await _actionService.UpdateActionAsync(action);
            }
            else
            {
                // New action
                action.CreatedAt = DateTime.UtcNow;
                action.UpdatedAt = DateTime.UtcNow;
                await _actionService.CreateActionAsync(action);
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            ValidationError = $"Erreur lors de la sauvegarde: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
    }

    [RelayCommand]
    private void AddWindowsParameter()
    {
        WindowsParameters.Add(new ParameterEditorViewModel
        {
            Name = $"param{WindowsParameters.Count + 1}",
            Label = "Nouveau paramètre",
            Type = "string",
            Required = false
        });
    }

    [RelayCommand]
    private void RemoveWindowsParameter(ParameterEditorViewModel param)
    {
        WindowsParameters.Remove(param);
    }

    [RelayCommand]
    private void AddLinuxParameter()
    {
        LinuxParameters.Add(new ParameterEditorViewModel
        {
            Name = $"param{LinuxParameters.Count + 1}",
            Label = "Nouveau paramètre",
            Type = "string",
            Required = false
        });
    }

    [RelayCommand]
    private void RemoveLinuxParameter(ParameterEditorViewModel param)
    {
        LinuxParameters.Remove(param);
    }

    [RelayCommand]
    private void AddExample()
    {
        Examples.Add(new ExampleEditorViewModel
        {
            Command = string.Empty,
            Description = "Description de l'exemple"
        });
    }

    [RelayCommand]
    private void RemoveExample(ExampleEditorViewModel example)
    {
        Examples.Remove(example);
    }

    /// <summary>
    /// BUGFIX: Comprehensive validation of all required fields
    /// </summary>
    private bool ValidateInput()
    {
        ValidationError = null;

        // Validate title
        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationError = "Le titre est requis.";
            return false;
        }

        if (Title.Length > 200)
        {
            ValidationError = "Le titre ne peut pas dépasser 200 caractères.";
            return false;
        }

        // Validate category
        if (string.IsNullOrWhiteSpace(Category))
        {
            ValidationError = "La catégorie est requise.";
            return false;
        }

        if (Category.Length > 100)
        {
            ValidationError = "La catégorie ne peut pas dépasser 100 caractères.";
            return false;
        }

        // Validate description length
        if (Description.Length > 2000)
        {
            ValidationError = "La description ne peut pas dépasser 2000 caractères.";
            return false;
        }

        // Validate notes length
        if (Notes.Length > 5000)
        {
            ValidationError = "Les notes ne peuvent pas dépasser 5000 caractères.";
            return false;
        }

        // Validate Windows command
        if (SelectedPlatform == Platform.Windows || SelectedPlatform == Platform.Both)
        {
            if (string.IsNullOrWhiteSpace(WindowsCommandPattern))
            {
                ValidationError = "La commande Windows est requise pour cette plateforme.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(WindowsCommandName))
            {
                ValidationError = "Le nom de la commande Windows est requis.";
                return false;
            }
        }

        // Validate Linux command
        if (SelectedPlatform == Platform.Linux || SelectedPlatform == Platform.Both)
        {
            if (string.IsNullOrWhiteSpace(LinuxCommandPattern))
            {
                ValidationError = "La commande Linux est requise pour cette plateforme.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(LinuxCommandName))
            {
                ValidationError = "Le nom de la commande Linux est requis.";
                return false;
            }
        }

        // Validate parameter names are unique and not empty
        foreach (var param in WindowsParameters)
        {
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                ValidationError = "Tous les paramètres Windows doivent avoir un nom.";
                return false;
            }
        }

        if (WindowsParameters.GroupBy(p => p.Name).Any(g => g.Count() > 1))
        {
            ValidationError = "Les noms de paramètres Windows doivent être uniques.";
            return false;
        }

        foreach (var param in LinuxParameters)
        {
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                ValidationError = "Tous les paramètres Linux doivent avoir un nom.";
                return false;
            }
        }

        if (LinuxParameters.GroupBy(p => p.Name).Any(g => g.Count() > 1))
        {
            ValidationError = "Les noms de paramètres Linux doivent être uniques.";
            return false;
        }

        return true;
    }
}

/// <summary>
/// ViewModel for editing a parameter
/// </summary>
public partial class ParameterEditorViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private string _type = "string";
    [ObservableProperty] private string _defaultValue = string.Empty;
    [ObservableProperty] private bool _required;
    [ObservableProperty] private string _description = string.Empty;
}

/// <summary>
/// ViewModel for editing a command example
/// </summary>
public partial class ExampleEditorViewModel : ObservableObject
{
    [ObservableProperty] private string _command = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
}
