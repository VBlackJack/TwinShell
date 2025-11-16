using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for managing custom categories.
/// </summary>
public partial class CategoryManagementViewModel : ObservableObject
{
    private readonly ICustomCategoryService _categoryService;

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
    private string _newCategoryIcon = "folder";

    [ObservableProperty]
    private string _newCategoryColor = "#2196F3";

    [ObservableProperty]
    private string? _newCategoryDescription;

    [ObservableProperty]
    private string? _errorMessage;

    // Available icons for selection
    public ObservableCollection<string> AvailableIcons { get; } = new()
    {
        "folder", "star", "tools", "database", "server", "cloud",
        "network", "security", "code", "terminal", "settings", "user",
        "group", "file", "document", "archive", "script", "key",
        "lock", "shield", "monitor", "laptop", "disk", "backup"
    };

    // Available colors for selection
    public ObservableCollection<string> AvailableColors { get; } = new()
    {
        "#2196F3", // Blue
        "#4CAF50", // Green
        "#FFC107", // Amber
        "#F44336", // Red
        "#9C27B0", // Purple
        "#FF9800", // Orange
        "#00BCD4", // Cyan
        "#E91E63", // Pink
        "#795548", // Brown
        "#607D8B", // Blue Grey
        "#673AB7", // Deep Purple
        "#3F51B5"  // Indigo
    };

    public CategoryManagementViewModel(ICustomCategoryService categoryService)
    {
        _categoryService = categoryService;
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
        var categories = await _categoryService.GetAllCategoriesAsync();
        Categories.Clear();

        foreach (var category in categories)
        {
            Categories.Add(new CategoryViewModel(category));
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
        NewCategoryIcon = "folder";
        NewCategoryColor = "#2196F3";
        NewCategoryDescription = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Saves a new category.
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

            var newCategory = await _categoryService.CreateCategoryAsync(
                NewCategoryName,
                NewCategoryIcon,
                NewCategoryColor,
                NewCategoryDescription);

            await LoadCategoriesAsync();

            IsAddMode = false;
            MessageBox.Show($"Category '{newCategory.Name}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            ErrorMessage = "An error occurred while saving the category";
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

        if (SelectedCategory.IsSystemCategory)
        {
            MessageBox.Show("System categories cannot be edited.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsEditMode = true;
        IsAddMode = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Saves changes to the selected category.
    /// </summary>
    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (SelectedCategory == null)
            return;

        try
        {
            ErrorMessage = null;

            var updatedCategory = SelectedCategory.ToModel();
            var success = await _categoryService.UpdateCategoryAsync(updatedCategory);

            if (success)
            {
                await LoadCategoriesAsync();
                IsEditMode = false;
                MessageBox.Show("Category updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ErrorMessage = "Failed to update category.";
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            ErrorMessage = "An error occurred while saving the category";
        }
    }

    /// <summary>
    /// Deletes the selected category.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedCategory == null)
            return;

        if (SelectedCategory.IsSystemCategory)
        {
            MessageBox.Show("System categories cannot be deleted.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete the category '{SelectedCategory.Name}'?\n\nThis will remove all action assignments from this category.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var success = await _categoryService.DeleteCategoryAsync(SelectedCategory.Id);
                if (success)
                {
                    await LoadCategoriesAsync();
                    SelectedCategory = null;
                    MessageBox.Show("Category deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // SECURITY: Don't expose exception details to users
                // BUGFIX: Corrected error message from "saving" to "deleting"
                ErrorMessage = "An error occurred while deleting the category";
            }
        }
    }

    /// <summary>
    /// Toggles the visibility of the selected category.
    /// </summary>
    [RelayCommand]
    private async Task ToggleVisibilityAsync()
    {
        if (SelectedCategory == null)
            return;

        try
        {
            var success = await _categoryService.ToggleCategoryVisibilityAsync(SelectedCategory.Id);
            if (success)
            {
                await LoadCategoriesAsync();
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            ErrorMessage = "An error occurred while saving the category";
        }
    }

    /// <summary>
    /// Moves the selected category up in the display order.
    /// </summary>
    [RelayCommand]
    private async Task MoveUpAsync()
    {
        if (SelectedCategory == null)
            return;

        var index = Categories.IndexOf(SelectedCategory);
        if (index > 0)
        {
            Categories.Move(index, index - 1);
            await SaveOrderAsync();
        }
    }

    /// <summary>
    /// Moves the selected category down in the display order.
    /// </summary>
    [RelayCommand]
    private async Task MoveDownAsync()
    {
        if (SelectedCategory == null)
            return;

        var index = Categories.IndexOf(SelectedCategory);
        if (index < Categories.Count - 1)
        {
            Categories.Move(index, index + 1);
            await SaveOrderAsync();
        }
    }

    /// <summary>
    /// Saves the current category order to the database.
    /// </summary>
    private async Task SaveOrderAsync()
    {
        var categoryIds = Categories.Select(c => c.Id).ToList();
        await _categoryService.ReorderCategoriesAsync(categoryIds);
    }
}
