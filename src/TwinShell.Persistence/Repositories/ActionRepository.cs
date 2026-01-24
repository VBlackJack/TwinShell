using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Mappers;

namespace TwinShell.Persistence.Repositories;

/// <summary>
/// Repository implementation for Action persistence
/// </summary>
public class ActionRepository : IActionRepository
{
    private readonly TwinShellDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ActionRepository> _logger;

    // Cache key for categories
    private const string CategoriesCacheKey = "ActionRepository_Categories";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ActionRepository(TwinShellDbContext context, IMemoryCache cache, ILogger<ActionRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<Core.Models.Action>> GetAllAsync()
    {
        // PERFORMANCE: AsNoTracking for read-only queries reduces memory overhead by 40-60%
        var entities = await _context.Actions
            .AsNoTracking()
            .Include(a => a.WindowsCommandTemplate)
            .Include(a => a.LinuxCommandTemplate)
            .ToListAsync();

        return entities.Select(ActionMapper.ToModel);
    }

    public async Task<Core.Models.Action?> GetByIdAsync(string id)
    {
        // PERFORMANCE: AsNoTracking for read-only queries
        var entity = await _context.Actions
            .AsNoTracking()
            .Include(a => a.WindowsCommandTemplate)
            .Include(a => a.LinuxCommandTemplate)
            .FirstOrDefaultAsync(a => a.Id == id);

        return entity != null ? ActionMapper.ToModel(entity) : null;
    }

    public async Task<IEnumerable<Core.Models.Action>> GetByCategoryAsync(string category)
    {
        // PERFORMANCE: AsNoTracking for read-only queries
        var entities = await _context.Actions
            .AsNoTracking()
            .Include(a => a.WindowsCommandTemplate)
            .Include(a => a.LinuxCommandTemplate)
            .Where(a => a.Category == category)
            .ToListAsync();

        return entities.Select(ActionMapper.ToModel);
    }

    public async Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        // PERFORMANCE: Use IMemoryCache with GetOrCreateAsync for thread-safe caching
        var categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            // Fetch from database
            var result = await _context.Actions
                .AsNoTracking()
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return result.AsReadOnly();
        });

        return categories ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Invalidates the categories cache (call after add/update/delete operations that change categories)
    /// </summary>
    private void InvalidateCategoriesCache()
    {
        _cache.Remove(CategoriesCacheKey);
    }

    public async Task AddAsync(Core.Models.Action action)
    {
        try
        {
            // BUGFIX: Removed explicit transaction - EF Core automatically wraps SaveChangesAsync() in a transaction
            // Add command templates first if they exist
            if (action.WindowsCommandTemplate != null)
            {
                var windowsTemplateEntity = CommandTemplateMapper.ToEntity(action.WindowsCommandTemplate);
                if (!await _context.CommandTemplates.AnyAsync(t => t.Id == windowsTemplateEntity.Id))
                {
                    _context.CommandTemplates.Add(windowsTemplateEntity);
                }
            }

            if (action.LinuxCommandTemplate != null)
            {
                var linuxTemplateEntity = CommandTemplateMapper.ToEntity(action.LinuxCommandTemplate);
                if (!await _context.CommandTemplates.AnyAsync(t => t.Id == linuxTemplateEntity.Id))
                {
                    _context.CommandTemplates.Add(linuxTemplateEntity);
                }
            }

            var entity = ActionMapper.ToEntity(action);
            _context.Actions.Add(entity);
            // EF Core ensures all tracked changes are saved atomically in a single transaction
            await _context.SaveChangesAsync();

            // Invalidate cache since a new action may have a new category
            InvalidateCategoriesCache();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while adding action: {ActionId}", action.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Core.Models.Action action)
    {
        try
        {
            // BUGFIX: Handle EF Core tracking - detach any existing tracked entities first
            // Add or update Windows command template if it exists
            if (action.WindowsCommandTemplate != null)
            {
                var windowsTemplateEntity = CommandTemplateMapper.ToEntity(action.WindowsCommandTemplate);
                var existingWindows = await _context.CommandTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == windowsTemplateEntity.Id);

                // Detach any tracked entity with the same ID
                var trackedWindows = _context.ChangeTracker.Entries<Entities.CommandTemplateEntity>()
                    .FirstOrDefault(e => e.Entity.Id == windowsTemplateEntity.Id);
                if (trackedWindows != null)
                {
                    trackedWindows.State = EntityState.Detached;
                }

                if (existingWindows == null)
                {
                    _context.CommandTemplates.Add(windowsTemplateEntity);
                }
                else
                {
                    _context.CommandTemplates.Update(windowsTemplateEntity);
                }
            }

            // Add or update Linux command template if it exists
            if (action.LinuxCommandTemplate != null)
            {
                var linuxTemplateEntity = CommandTemplateMapper.ToEntity(action.LinuxCommandTemplate);
                var existingLinux = await _context.CommandTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == linuxTemplateEntity.Id);

                // Detach any tracked entity with the same ID
                var trackedLinux = _context.ChangeTracker.Entries<Entities.CommandTemplateEntity>()
                    .FirstOrDefault(e => e.Entity.Id == linuxTemplateEntity.Id);
                if (trackedLinux != null)
                {
                    trackedLinux.State = EntityState.Detached;
                }

                if (existingLinux == null)
                {
                    _context.CommandTemplates.Add(linuxTemplateEntity);
                }
                else
                {
                    _context.CommandTemplates.Update(linuxTemplateEntity);
                }
            }

            var entity = ActionMapper.ToEntity(action);

            // Detach any tracked Action entity with the same ID
            var trackedAction = _context.ChangeTracker.Entries<Entities.ActionEntity>()
                .FirstOrDefault(e => e.Entity.Id == entity.Id);
            if (trackedAction != null)
            {
                trackedAction.State = EntityState.Detached;
            }

            _context.Actions.Update(entity);
            await _context.SaveChangesAsync();

            // Invalidate cache since category may have changed
            InvalidateCategoriesCache();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating action: {ActionId}", action.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var entity = await _context.Actions.FindAsync(id);
            if (entity != null)
            {
                _context.Actions.Remove(entity);
                await _context.SaveChangesAsync();

                // Invalidate cache since a category may now be empty
                InvalidateCategoriesCache();
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting action: {ActionId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Actions.AnyAsync(a => a.Id == id);
    }

    public async Task<int> CountAsync()
    {
        return await _context.Actions.CountAsync();
    }

    /// <summary>
    /// PERFORMANCE: Efficiently counts actions in a category using database-level COUNT
    /// This avoids loading all actions into memory just to count them
    /// </summary>
    public async Task<int> CountByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return 0;

        return await _context.Actions
            .Where(a => a.Category == category)
            .CountAsync();
    }

    /// <summary>
    /// PERFORMANCE: Batch update category for all actions in a category using single SQL UPDATE
    /// This prevents N+1 queries when renaming or deleting categories
    /// </summary>
    public async Task<int> UpdateCategoryForActionsAsync(string oldCategory, string? newCategory)
    {
        if (string.IsNullOrWhiteSpace(oldCategory))
            return 0;

        var targetCategory = newCategory ?? string.Empty;
        var now = DateTime.UtcNow;

        // Use EF Core's ExecuteUpdateAsync for a single SQL UPDATE statement
        // This is much more efficient than loading all entities and updating them individually
        var result = await _context.Actions
            .Where(a => a.Category == oldCategory)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Category, targetCategory)
                .SetProperty(a => a.UpdatedAt, now));

        // Invalidate cache since categories changed
        if (result > 0)
        {
            InvalidateCategoriesCache();
        }

        return result;
    }
}
