using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Mappers;

namespace TwinShell.Persistence.Repositories;

/// <summary>
/// Repository implementation for CommandHistory persistence
/// </summary>
public class CommandHistoryRepository : ICommandHistoryRepository
{
    private readonly TwinShellDbContext _context;

    public CommandHistoryRepository(TwinShellDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CommandHistory history)
    {
        var entity = CommandHistoryMapper.ToEntity(history);
        _context.CommandHistories.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CommandHistory history)
    {
        var entity = CommandHistoryMapper.ToEntity(history);
        _context.CommandHistories.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<CommandHistory>> GetRecentAsync(int count = 50)
    {
        // PERFORMANCE: AsNoTracking for read-only queries reduces memory overhead by 40-60%
        var entities = await _context.CommandHistories
            .AsNoTracking()
            .Include(h => h.Action)
            .OrderByDescending(h => h.CreatedAt)
            .Take(count)
            .ToListAsync();

        return entities.Select(CommandHistoryMapper.ToModel);
    }

    public async Task<IEnumerable<CommandHistory>> SearchAsync(
        string? searchText = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Platform? platform = null,
        string? category = null)
    {
        // PERFORMANCE: AsNoTracking for read-only queries
        var query = _context.CommandHistories
            .AsNoTracking()
            .Include(h => h.Action)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(h =>
                h.GeneratedCommand.ToLower().Contains(search) ||
                h.ActionTitle.ToLower().Contains(search));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.CreatedAt <= toDate.Value);
        }

        if (platform.HasValue)
        {
            query = query.Where(h => h.Platform == platform.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(h => h.Category == category);
        }

        var entities = await query
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return entities.Select(CommandHistoryMapper.ToModel);
    }

    public async Task<CommandHistory?> GetByIdAsync(string id)
    {
        // PERFORMANCE: AsNoTracking for read-only queries
        var entity = await _context.CommandHistories
            .AsNoTracking()
            .Include(h => h.Action)
            .FirstOrDefaultAsync(h => h.Id == id);

        return entity != null ? CommandHistoryMapper.ToModel(entity) : null;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.CommandHistories.FindAsync(id);
        if (entity != null)
        {
            _context.CommandHistories.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<string> ids)
    {
        var entities = await _context.CommandHistories
            .Where(h => ids.Contains(h.Id))
            .ToListAsync();

        // PERFORMANCE: Use AnyAsync for existence checks (but here we need the entities)
        if (entities.Count > 0)
        {
            _context.CommandHistories.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteOlderThanAsync(DateTime date)
    {
        var entities = await _context.CommandHistories
            .Where(h => h.CreatedAt < date)
            .ToListAsync();

        // PERFORMANCE: Use Count for List instead of Any()
        if (entities.Count > 0)
        {
            _context.CommandHistories.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CountAsync()
    {
        return await _context.CommandHistories.CountAsync();
    }

    public async Task ClearAllAsync()
    {
        var entities = await _context.CommandHistories.ToListAsync();
        // PERFORMANCE: Use Count for List instead of Any()
        if (entities.Count > 0)
        {
            _context.CommandHistories.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
