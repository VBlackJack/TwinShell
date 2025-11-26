using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for managing action categories.
/// </summary>
public partial class CategoryManagementViewModel : ObservableObject
{
    private readonly IActionService _actionService;

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

    public CategoryManagementViewModel(IActionService actionService)
    {
        _actionService = actionService;
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
                ErrorMessage = "Category name is required.";
                return;
            }

            // Check if category already exists
            var existingCategories = await _actionService.GetAllCategoriesAsync();
            if (existingCategories.Any(c => c.Equals(NewCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "A category with this name already exists.";
                return;
            }

            IsAddMode = false;
            MessageBox.Show(
                $"Category name '{NewCategoryName}' is ready to use.\n\nTo use this category, edit an action and assign it to this category.",
                "Category Registered",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            NewCategoryName = string.Empty;
        }
        catch (Exception)
        {
            // SECURITY: Don't expose exception details to users
            ErrorMessage = "An error occurred while processing the category";
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
                ErrorMessage = "Category name cannot be empty.";
                return;
            }

            // Check if new name already exists (and it's different from original)
            if (!SelectedCategory.Name.Equals(SelectedCategory.OriginalName, StringComparison.OrdinalIgnoreCase))
            {
                var existingCategories = await _actionService.GetAllCategoriesAsync();
                if (existingCategories.Any(c => c.Equals(SelectedCategory.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorMessage = "A category with this name already exists.";
                    return;
                }
            }

            var success = await _actionService.RenameCategoryAsync(SelectedCategory.OriginalName, SelectedCategory.Name);

            if (success)
            {
                await LoadCategoriesAsync();
                IsEditMode = false;
                SelectedCategory = null;
                MessageBox.Show("Category renamed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ErrorMessage = "Failed to rename category.";
            }
        }
        catch (Exception)
        {
            // SECURITY: Don't expose exception details to users
            ErrorMessage = "An error occurred while saving the category";
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
            $"Are you sure you want to delete the category '{SelectedCategory.Name}'?\n\nThis will remove the category from all {SelectedCategory.ActionCount} action(s) that use it.",
            "Confirm Delete",
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
                    MessageBox.Show("Category deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                // SECURITY: Don't expose exception details to users
                ErrorMessage = "An error occurred while deleting the category";
            }
        }
    }

}
