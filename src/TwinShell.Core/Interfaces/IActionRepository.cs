using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Repository interface for Action persistence
/// </summary>
public interface IActionRepository
{
    Task<IEnumerable<ActionModel>> GetAllAsync();
    Task<ActionModel?> GetByIdAsync(string id);
    Task<IEnumerable<ActionModel>> GetByCategoryAsync(string category);
    Task<IEnumerable<string>> GetAllCategoriesAsync();
    Task AddAsync(ActionModel action);
    Task UpdateAsync(ActionModel action);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<int> CountAsync();
}
