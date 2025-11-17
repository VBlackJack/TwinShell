using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Service for managing actions
/// </summary>
public interface IActionService
{
    Task<IEnumerable<ActionModel>> GetAllActionsAsync();
    Task<ActionModel?> GetActionByIdAsync(string id);
    Task<IEnumerable<ActionModel>> GetActionsByCategoryAsync(string category);
    Task<IEnumerable<string>> GetAllCategoriesAsync();
    Task<IEnumerable<ActionModel>> FilterActionsAsync(
        IEnumerable<ActionModel> actions,
        Platform? platform = null,
        CriticalityLevel? level = null);
    Task<ActionModel> CreateActionAsync(ActionModel action);
    Task UpdateActionAsync(ActionModel action);
    Task DeleteActionAsync(string id);
}
