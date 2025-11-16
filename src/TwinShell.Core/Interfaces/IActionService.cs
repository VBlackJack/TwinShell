using TwinShell.Core.Enums;
using TwinShell.Core.Models;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Service for managing actions
/// </summary>
public interface IActionService
{
    Task<IEnumerable<Action>> GetAllActionsAsync();
    Task<Action?> GetActionByIdAsync(string id);
    Task<IEnumerable<Action>> GetActionsByCategoryAsync(string category);
    Task<IEnumerable<string>> GetAllCategoriesAsync();
    Task<IEnumerable<Action>> FilterActionsAsync(
        IEnumerable<Action> actions,
        Platform? platform = null,
        CriticalityLevel? level = null);
    Task<Action> CreateActionAsync(Action action);
    Task UpdateActionAsync(Action action);
    Task DeleteActionAsync(string id);
}
