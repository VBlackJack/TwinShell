using TwinShell.Core.Models;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Repository interface for Action persistence
/// </summary>
public interface IActionRepository
{
    Task<IEnumerable<Action>> GetAllAsync();
    Task<Action?> GetByIdAsync(string id);
    Task<IEnumerable<Action>> GetByCategoryAsync(string category);
    Task<IEnumerable<string>> GetAllCategoriesAsync();
    Task AddAsync(Action action);
    Task UpdateAsync(Action action);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<int> CountAsync();
}
