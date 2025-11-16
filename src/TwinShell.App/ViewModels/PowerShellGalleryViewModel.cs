using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for PowerShell Gallery integration
/// </summary>
public partial class PowerShellGalleryViewModel : ObservableObject
{
    private readonly IPowerShellGalleryService _galleryService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PowerShellModule> _modules = new();

    [ObservableProperty]
    private PowerShellModule? _selectedModule;

    [ObservableProperty]
    private ObservableCollection<PowerShellCommand> _commands = new();

    [ObservableProperty]
    private PowerShellCommand? _selectedCommand;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _isLoadingCommands;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private string _statusMessage = "Search for PowerShell modules in the PowerShell Gallery";

    public PowerShellGalleryViewModel(
        IPowerShellGalleryService galleryService,
        INotificationService notificationService)
    {
        _galleryService = galleryService ?? throw new ArgumentNullException(nameof(galleryService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    [RelayCommand]
    private async Task SearchModulesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            _notificationService.ShowWarning("Please enter a search query");
            return;
        }

        try
        {
            IsSearching = true;
            StatusMessage = $"Searching for modules matching '{SearchQuery}'...";

            var results = await _galleryService.SearchModulesAsync(SearchQuery, maxResults: 50);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Modules.Clear();
                foreach (var module in results)
                {
                    Modules.Add(module);
                }
            });

            StatusMessage = $"Found {Modules.Count} modules";

            if (Modules.Count == 0)
            {
                _notificationService.ShowInfo("No modules found matching your search");
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _notificationService.ShowError("Search failed");
            StatusMessage = "Search failed";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task LoadModuleCommandsAsync()
    {
        if (SelectedModule == null)
        {
            _notificationService.ShowWarning("Please select a module");
            return;
        }

        try
        {
            IsLoadingCommands = true;
            StatusMessage = $"Loading commands from {SelectedModule.Name}...";

            // Check if module is installed
            var isInstalled = await _galleryService.IsModuleInstalledAsync(SelectedModule.Name);

            if (!isInstalled)
            {
                _notificationService.ShowWarning($"Module '{SelectedModule.Name}' is not installed. Please install it first.");
                StatusMessage = $"Module not installed";
                return;
            }

            var commands = await _galleryService.GetModuleCommandsAsync(SelectedModule.Name);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Commands.Clear();
                foreach (var command in commands)
                {
                    Commands.Add(command);
                }
            });

            StatusMessage = $"Loaded {Commands.Count} commands from {SelectedModule.Name}";
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _notificationService.ShowError("Failed to load commands");
            StatusMessage = "Failed to load commands";
        }
        finally
        {
            IsLoadingCommands = false;
        }
    }

    [RelayCommand]
    private async Task InstallModuleAsync()
    {
        if (SelectedModule == null)
        {
            _notificationService.ShowWarning("Please select a module to install");
            return;
        }

        try
        {
            IsInstalling = true;
            StatusMessage = $"Installing {SelectedModule.Name}... This may take a few minutes.";

            var success = await _galleryService.InstallModuleAsync(SelectedModule.Name);

            if (success)
            {
                _notificationService.ShowSuccess($"Module '{SelectedModule.Name}' installed successfully");
                StatusMessage = $"Module installed successfully";
                SelectedModule.IsInstalled = true;
            }
            else
            {
                _notificationService.ShowError($"Failed to install module '{SelectedModule.Name}'");
                StatusMessage = "Installation failed";
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _notificationService.ShowError("Installation failed");
            StatusMessage = "Installation failed";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    [RelayCommand]
    private async Task ImportCommandAsActionAsync()
    {
        if (SelectedCommand == null)
        {
            _notificationService.ShowWarning("Please select a command to import");
            return;
        }

        try
        {
            StatusMessage = $"Importing {SelectedCommand.Name}...";

            // Get full help information
            var commandHelp = await _galleryService.GetCommandHelpAsync(SelectedCommand.Name);

            if (commandHelp == null)
            {
                _notificationService.ShowWarning($"Could not retrieve help information for '{SelectedCommand.Name}'");
                return;
            }

            // Import as action
            var action = await _galleryService.ImportCommandAsActionAsync(commandHelp);

            _notificationService.ShowSuccess($"Command '{SelectedCommand.Name}' imported as action");
            StatusMessage = $"Command imported successfully";
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _notificationService.ShowError("Import failed");
            StatusMessage = "Import failed";
        }
    }
}
