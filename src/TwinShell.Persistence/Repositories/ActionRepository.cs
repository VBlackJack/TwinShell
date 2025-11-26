using Microsoft.EntityFrameworkCore;
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

    public ActionRepository(TwinShellDbContext context)
    {
        _context = context;
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
        // PERFORMANCE: AsNoTracking for read-only queries
        return await _context.Actions
            .AsNoTracking()
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task AddAsync(Core.Models.Action action)
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
    }

    public async Task UpdateAsync(Core.Models.Action action)
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
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Actions.FindAsync(id);
        if (entity != null)
        {
            _context.Actions.Remove(entity);
            await _context.SaveChangesAsync();
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
}
