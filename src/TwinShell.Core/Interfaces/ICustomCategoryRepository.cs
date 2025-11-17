using TwinShell.Core.Models;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Repository interface for managing custom categories.
/// </summary>
public interface ICustomCategoryRepository
{
    Task<IEnumerable<CustomCategory>> GetAllAsync();
    Task<CustomCategory?> GetByIdAsync(string id);
    Task<CustomCategory> CreateAsync(CustomCategory category);
    Task UpdateAsync(CustomCategory category);
    Task DeleteAsync(string id);
    Task<IEnumerable<CustomCategory>> GetVisibleCategoriesAsync();
    Task<IEnumerable<string>> GetActionIdsForCategoryAsync(string categoryId);
    Task AddActionToCategoryAsync(string actionId, string categoryId);
    Task RemoveActionFromCategoryAsync(string actionId, string categoryId);
    Task<bool> IsCategorySystemAsync(string categoryId);
    Task<int> GetNextDisplayOrderAsync();
}
