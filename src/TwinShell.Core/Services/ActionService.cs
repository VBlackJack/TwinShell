using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

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

    public async Task<IEnumerable<Action>> GetAllActionsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Action?> GetActionByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Action>> GetActionsByCategoryAsync(string category)
    {
        return await _repository.GetByCategoryAsync(category);
    }

    public async Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        return await _repository.GetAllCategoriesAsync();
    }

    public async Task<IEnumerable<Action>> FilterActionsAsync(
        IEnumerable<Action> actions,
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

    public async Task<Action> CreateActionAsync(Action action)
    {
        action.Id = Guid.NewGuid().ToString();
        action.CreatedAt = DateTime.UtcNow;
        action.UpdatedAt = DateTime.UtcNow;
        action.IsUserCreated = true;

        await _repository.AddAsync(action);
        return action;
    }

    public async Task UpdateActionAsync(Action action)
    {
        action.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(action);
    }

    public async Task DeleteActionAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }
}
