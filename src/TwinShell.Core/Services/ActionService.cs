using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing actions
/// </summary>
public class ActionService : IActionService
{
    private readonly IActionRepository _repository;

    public ActionService(IActionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ActionModel>> GetAllActionsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<ActionModel?> GetActionByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ActionModel>> GetActionsByCategoryAsync(string category)
    {
        return await _repository.GetByCategoryAsync(category);
    }

    public async Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        return await _repository.GetAllCategoriesAsync();
    }

    public async Task<IEnumerable<ActionModel>> FilterActionsAsync(
        IEnumerable<ActionModel> actions,
        Platform? platform = null,
        CriticalityLevel? level = null)
    {
        var filtered = actions.AsEnumerable();

        if (platform.HasValue)
        {
            filtered = filtered.Where(a =>
                a.Platform == platform.Value ||
                a.Platform == Platform.Both);
        }

        if (level.HasValue)
        {
            filtered = filtered.Where(a => a.Level == level.Value);
        }

        return await Task.FromResult(filtered);
    }

    public async Task<ActionModel> CreateActionAsync(ActionModel action)
    {
        action.Id = Guid.NewGuid().ToString();
        action.CreatedAt = DateTime.UtcNow;
        action.UpdatedAt = DateTime.UtcNow;
        action.IsUserCreated = true;

        await _repository.AddAsync(action);
        return action;
    }

    public async Task UpdateActionAsync(ActionModel action)
    {
        action.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(action);
    }

    public async Task DeleteActionAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<int> GetActionCountByCategoryAsync(string category)
    {
        var actions = await _repository.GetByCategoryAsync(category);
        return actions.Count();
    }

    public async Task<bool> RenameCategoryAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
            return false;

        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            return true; // Nothing to do

        var actions = await _repository.GetByCategoryAsync(oldName);
        foreach (var action in actions)
        {
            action.Category = newName;
            action.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(action);
        }

        return true;
    }

    public async Task<bool> DeleteCategoryAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return false;

        var actions = await _repository.GetByCategoryAsync(categoryName);
        foreach (var action in actions)
        {
            action.Category = string.Empty;
            action.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(action);
        }

        return true;
    }
}
