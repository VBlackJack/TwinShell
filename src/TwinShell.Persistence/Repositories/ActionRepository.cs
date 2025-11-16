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
        var entities = await _context.Actions
            .Include(a => a.WindowsCommandTemplate)
            .Include(a => a.LinuxCommandTemplate)
            .ToListAsync();

        return entities.Select(ActionMapper.ToModel);
    }

    public async Task<Core.Models.Action?> GetByIdAsync(string id)
    {
        var entity = await _context.Actions
            .Include(a => a.WindowsCommandTemplate)
            .Include(a => a.LinuxCommandTemplate)
            .FirstOrDefaultAsync(a => a.Id == id);

        return entity != null ? ActionMapper.ToModel(entity) : null;
    }

    public async Task<IEnumerable<Core.Models.Action>> GetByCategoryAsync(string category)
    {
        var entities = await _context.Actions
            .Include(a => a.WindowsCommandTemplate)
            .Include(a => a.LinuxCommandTemplate)
            .Where(a => a.Category == category)
            .ToListAsync();

        return entities.Select(ActionMapper.ToModel);
    }

    public async Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        return await _context.Actions
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task AddAsync(Core.Models.Action action)
    {
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
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Core.Models.Action action)
    {
        var entity = ActionMapper.ToEntity(action);
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
