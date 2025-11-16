using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel wrapper for Action with favorite functionality
/// </summary>
public partial class ActionViewModel : ObservableObject
{
    private readonly IFavoritesService _favoritesService;
    private readonly Action _action;

    public ActionViewModel(Action action, IFavoritesService favoritesService)
    {
        _action = action;
        _favoritesService = favoritesService;
    }

    // Action properties (pass-through)
    public string Id => _action.Id;
    public string Title => _action.Title;
    public string Description => _action.Description;
    public string Category => _action.Category;
    public Platform Platform => _action.Platform;
    public CriticalityLevel Level => _action.Level;
    public List<string> Tags => _action.Tags;
    public string? WindowsCommandTemplateId => _action.WindowsCommandTemplateId;
    public CommandTemplate? WindowsCommandTemplate => _action.WindowsCommandTemplate;
    public string? LinuxCommandTemplateId => _action.LinuxCommandTemplateId;
    public CommandTemplate? LinuxCommandTemplate => _action.LinuxCommandTemplate;
    public List<CommandExample> Examples => _action.Examples;
    public string? Notes => _action.Notes;
    public List<ExternalLink> Links => _action.Links;
    public DateTime CreatedAt => _action.CreatedAt;
    public DateTime UpdatedAt => _action.UpdatedAt;
    public bool IsUserCreated => _action.IsUserCreated;

    // Get the underlying Action model
    public Action GetAction() => _action;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private string _favoriteIcon = "☆";

    [ObservableProperty]
    private string _favoriteTooltip = "Add to favorites";

    public async Task LoadFavoriteStatusAsync()
    {
        IsFavorite = await _favoritesService.IsFavoriteAsync(_action.Id);
        UpdateFavoriteUI();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        var result = await _favoritesService.ToggleFavoriteAsync(_action.Id);

        if (result)
        {
            // Successfully added to favorites
            IsFavorite = true;
            UpdateFavoriteUI();
        }
        else
        {
            // Check if it failed due to limit or was removed
            var stillFavorite = await _favoritesService.IsFavoriteAsync(_action.Id);
            if (!stillFavorite)
            {
                // Successfully removed from favorites
                IsFavorite = false;
                UpdateFavoriteUI();
            }
            else
            {
                // Failed to add (likely limit reached)
                var count = await _favoritesService.GetFavoriteCountAsync();
                System.Windows.MessageBox.Show(
                    $"You have reached the maximum limit of 50 favorites ({count}/50). Please remove some favorites before adding new ones.",
                    "Favorites Limit Reached",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
    }

    private void UpdateFavoriteUI()
    {
        FavoriteIcon = IsFavorite ? "★" : "☆";
        FavoriteTooltip = IsFavorite ? "Remove from favorites" : "Add to favorites";
    }
}
