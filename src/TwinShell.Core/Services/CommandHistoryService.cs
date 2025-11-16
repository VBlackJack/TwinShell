using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing command history
/// </summary>
public class CommandHistoryService : ICommandHistoryService
{
    private readonly ICommandHistoryRepository _repository;

    public CommandHistoryService(ICommandHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> AddCommandAsync(
        string actionId,
        string generatedCommand,
        Dictionary<string, string> parameters,
        Platform platform,
        string actionTitle,
        string category)
    {
        var history = new CommandHistory
        {
            Id = Guid.NewGuid().ToString(),
            ActionId = actionId,
            GeneratedCommand = generatedCommand,
            Parameters = parameters,
            Platform = platform,
            ActionTitle = actionTitle,
            Category = category,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(history);
        return history.Id;
    }

    public async Task UpdateWithExecutionResultsAsync(
        string historyId,
        int exitCode,
        TimeSpan duration,
        bool success)
    {
        var history = await _repository.GetByIdAsync(historyId);
        if (history != null)
        {
            history.IsExecuted = true;
            history.ExitCode = exitCode;
            history.ExecutionDuration = duration;
            history.ExecutionSuccess = success;
            await _repository.UpdateAsync(history);
        }
    }

    public async Task<IEnumerable<CommandHistory>> GetRecentAsync(int count = 50)
    {
        return await _repository.GetRecentAsync(count);
    }

    public async Task<IEnumerable<CommandHistory>> SearchAsync(
        string? searchText = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Platform? platform = null,
        string? category = null)
    {
        return await _repository.SearchAsync(searchText, fromDate, toDate, platform, category);
    }

    public async Task<CommandHistory?> GetByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task DeleteAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task DeleteRangeAsync(IEnumerable<string> ids)
    {
        await _repository.DeleteRangeAsync(ids);
    }

    public async Task CleanupOldEntriesAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        await _repository.DeleteOlderThanAsync(cutoffDate);
    }

    public async Task<int> GetCountAsync()
    {
        return await _repository.CountAsync();
    }

    public async Task ClearAllAsync()
    {
        await _repository.ClearAllAsync();
    }
}
