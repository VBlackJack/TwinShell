using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using TwinShell.Core.Constants;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for managing action categories.
/// </summary>
public partial class CategoryManagementViewModel : ObservableObject
{
    private readonly IActionService _actionService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<CategoryManagementViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> _categories = new();

    [ObservableProperty]
    private CategoryViewModel? _selectedCategory;

    [ObservableProperty]
    private bool _isAddMode;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public CategoryManagementViewModel(
        IActionService actionService,
        ILocalizationService localizationService,
        ILogger<CategoryManagementViewModel> logger)
    {
        _actionService = actionService;
        _localizationService = localizationService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the ViewModel by loading all categories.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
    }

    /// <summary>
    /// Loads all categories from the service.
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        var categoryNames = await _actionService.GetAllCategoriesAsync();
        Categories.Clear();

        foreach (var categoryName in categoryNames.OrderBy(c => c))
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                continue;

            var count = await _actionService.GetActionCountByCategoryAsync(categoryName);
            Categories.Add(new CategoryViewModel(categoryName, count));
        }
    }

    /// <summary>
    /// Enters add mode to create a new category.
    /// </summary>
    [RelayCommand]
    private void StartAdd()
    {
        IsAddMode = true;
        IsEditMode = false;
        NewCategoryName = string.Empty;
        ErrorMessage = null;
    }

    /// <summary>
    /// Saves a new category (not applicable for action-based categories).
    /// Categories are automatically created when assigned to actions.
    /// </summary>
    [RelayCommand]
    private async Task SaveNewAsync()
    {
        try
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                ErrorMessage = _localizationService.GetString(MessageKeys.CategoryNameRequired);
                return;
            }

            // Check if category already exists
            var existingCategories = await _actionService.GetAllCategoriesAsync();
            if (existingCategories.Any(c => c.Equals(NewCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = _localizationService.GetString(MessageKeys.CategoryAlreadyExists);
                return;
            }

            IsAddMode = false;
            MessageBox.Show(
                _localizationService.GetFormattedString(MessageKeys.CategoryReadyToUse, NewCategoryName),
                _localizationService.GetString(MessageKeys.CategoryRegisteredTitle),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            NewCategoryName = string.Empty;
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Error in SaveNewAsync for category: {CategoryName}", NewCategoryName);
            ErrorMessage = _localizationService.GetString(MessageKeys.CategoryProcessingError);
        }
    }

    /// <summary>
    /// Cancels add/edit mode.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsAddMode = false;
        IsEditMode = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Starts editing the selected category.
    /// </summary>
    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedCategory == null)
            return;

        IsEditMode = true;
        IsAddMode = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Saves changes to the selected category (renames it).
    /// </summary>
    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (SelectedCategory == null)
            return;

        try
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(SelectedCategory.Name))
            {
                ErrorMessage = _localizationService.GetString(MessageKeys.CategoryNameEmpty);
                return;
            }

            // Check if new name already exists (and it's different from original)
            if (!SelectedCategory.Name.Equals(SelectedCategory.OriginalName, StringComparison.OrdinalIgnoreCase))
            {
                var existingCategories = await _actionService.GetAllCategoriesAsync();
                if (existingCategories.Any(c => c.Equals(SelectedCategory.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorMessage = _localizationService.GetString(MessageKeys.CategoryAlreadyExists);
                    return;
                }
            }

            var success = await _actionService.RenameCategoryAsync(SelectedCategory.OriginalName, SelectedCategory.Name);

            if (success)
            {
                await LoadCategoriesAsync();
                IsEditMode = false;
                SelectedCategory = null;
                MessageBox.Show(
                    _localizationService.GetString(MessageKeys.CategoryRenamedSuccess),
                    _localizationService.GetString(MessageKeys.Success),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                ErrorMessage = _localizationService.GetString(MessageKeys.CategoryRenameFailed);
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Error in SaveEditAsync for category: {CategoryName}", SelectedCategory?.Name);
            ErrorMessage = _localizationService.GetString(MessageKeys.CategorySaveError);
        }
    }

    /// <summary>
    /// Deletes the selected category by removing it from all actions.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedCategory == null)
            return;

        var result = MessageBox.Show(
            _localizationService.GetFormattedString(MessageKeys.CategoryDeleteConfirmation, SelectedCategory.Name, SelectedCategory.ActionCount),
            _localizationService.GetString(MessageKeys.CategoryDeleteConfirmTitle),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var success = await _actionService.DeleteCategoryAsync(SelectedCategory.Name);
                if (success)
                {
                    await LoadCategoriesAsync();
                    SelectedCategory = null;
                    MessageBox.Show(
                        _localizationService.GetString(MessageKeys.CategoryDeletedSuccess),
                        _localizationService.GetString(MessageKeys.Success),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // SECURITY: Don't expose exception details to users
                _logger.LogError(ex, "Error in DeleteAsync for category: {CategoryName}", SelectedCategory?.Name);
                ErrorMessage = _localizationService.GetString(MessageKeys.CategoryDeleteError);
            }
        }
    }

}
