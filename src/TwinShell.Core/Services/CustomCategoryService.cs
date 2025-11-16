using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Repositories;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing custom user-defined categories.
/// </summary>
public class CustomCategoryService : ICustomCategoryService
{
    private readonly ICustomCategoryRepository _repository;
    private const int MaxCategoriesLimit = 50; // Limit to prevent excessive categories

    public CustomCategoryService(ICustomCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CustomCategory>> GetAllCategoriesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<CustomCategory>> GetVisibleCategoriesAsync()
    {
        return await _repository.GetVisibleCategoriesAsync();
    }

    public async Task<CustomCategory?> GetCategoryByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<CustomCategory> CreateCategoryAsync(string name, string iconKey = "folder", string colorHex = "#2196F3", string? description = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));

        // Check uniqueness
        var isUnique = await ValidateCategoryNameAsync(name);
        if (!isUnique)
            throw new InvalidOperationException($"A category with the name '{name}' already exists");

        // Check category count limit
        var allCategories = await _repository.GetAllAsync();
        if (allCategories.Count() >= MaxCategoriesLimit)
            throw new InvalidOperationException($"Maximum number of categories ({MaxCategoriesLimit}) reached");

        // Create category
        var category = new CustomCategory
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            IconKey = iconKey,
            ColorHex = colorHex,
            Description = description,
            IsSystemCategory = false,
            IsHidden = false,
            DisplayOrder = await _repository.GetNextDisplayOrderAsync(),
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(category);
    }

    public async Task<bool> UpdateCategoryAsync(CustomCategory category)
    {
        // Check if category exists
        var existing = await _repository.GetByIdAsync(category.Id);
        if (existing == null)
            return false;

        // Prevent modification of system categories
        if (existing.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be modified");

        // Validate name uniqueness (excluding current category)
        var isUnique = await ValidateCategoryNameAsync(category.Name, category.Id);
        if (!isUnique)
            throw new InvalidOperationException($"A category with the name '{category.Name}' already exists");

        await _repository.UpdateAsync(category);
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(string id)
    {
        // Check if category exists and is not a system category
        var isSystem = await _repository.IsCategorySystemAsync(id);
        if (isSystem)
            throw new InvalidOperationException("System categories cannot be deleted");

        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> ToggleCategoryVisibilityAsync(string id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return false;

        category.IsHidden = !category.IsHidden;
        category.ModifiedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);
        return true;
    }

    public async Task ReorderCategoriesAsync(IEnumerable<string> categoryIdsInOrder)
    {
        var categories = await _repository.GetAllAsync();
        var categoryDict = categories.ToDictionary(c => c.Id);

        int order = 0;
        foreach (var categoryId in categoryIdsInOrder)
        {
            if (categoryDict.TryGetValue(categoryId, out var category))
            {
                category.DisplayOrder = order++;
                category.ModifiedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(category);
            }
        }
    }

    public async Task AddActionToCategoryAsync(string actionId, string categoryId)
    {
        if (string.IsNullOrEmpty(actionId) || string.IsNullOrEmpty(categoryId))
            throw new ArgumentException("ActionId and CategoryId cannot be empty");

        await _repository.AddActionToCategoryAsync(actionId, categoryId);
    }

    public async Task RemoveActionFromCategoryAsync(string actionId, string categoryId)
    {
        await _repository.RemoveActionFromCategoryAsync(actionId, categoryId);
    }

    public async Task<IEnumerable<string>> GetActionsInCategoryAsync(string categoryId)
    {
        return await _repository.GetActionIdsForCategoryAsync(categoryId);
    }

    public async Task<bool> ValidateCategoryNameAsync(string name, string? excludeCategoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // BUGFIX: Use AnyAsync instead of GetAllAsync for better performance
        // Check if any category exists with the same name (case-insensitive) except the excluded one
        var allCategories = await _repository.GetAllAsync();
        return !allCategories.Any(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            c.Id != excludeCategoryId);
    }
}
