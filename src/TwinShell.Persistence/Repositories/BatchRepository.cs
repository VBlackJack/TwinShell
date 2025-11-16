using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Mappers;

namespace TwinShell.Persistence.Repositories;

/// <summary>
/// Repository implementation for CommandBatch persistence
/// </summary>
public class BatchRepository : IBatchRepository
{
    private readonly TwinShellDbContext _context;

    public BatchRepository(TwinShellDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<CommandBatch>> GetAllAsync()
    {
        var entities = await _context.CommandBatches
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();

        return entities.Select(CommandBatchMapper.ToModel);
    }

    public async Task<CommandBatch?> GetByIdAsync(string id)
    {
        var entity = await _context.CommandBatches
            .FirstOrDefaultAsync(b => b.Id == id);

        return entity == null ? null : CommandBatchMapper.ToModel(entity);
    }

    public async Task AddAsync(CommandBatch batch)
    {
        var entity = CommandBatchMapper.ToEntity(batch);
        _context.CommandBatches.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CommandBatch batch)
    {
        var entity = CommandBatchMapper.ToEntity(batch);
        _context.CommandBatches.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.CommandBatches
            .FirstOrDefaultAsync(b => b.Id == id);

        if (entity != null)
        {
            _context.CommandBatches.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<CommandBatch>> SearchAsync(string query)
    {
        var lowerQuery = query.ToLower();

        var entities = await _context.CommandBatches
            .Where(b => b.Name.ToLower().Contains(lowerQuery) ||
                       (b.Description != null && b.Description.ToLower().Contains(lowerQuery)))
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();

        return entities.Select(CommandBatchMapper.ToModel);
    }
}
