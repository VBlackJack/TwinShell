using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Mappers;

namespace TwinShell.Persistence.Repositories;

/// <summary>
/// Repository implementation for AuditLog persistence
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly TwinShellDbContext _context;

    public AuditLogRepository(TwinShellDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log)
    {
        var entity = AuditLogMapper.ToEntity(log);
        _context.AuditLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100)
    {
        var entities = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();

        return entities.Select(AuditLogMapper.ToModel);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var entities = await _context.AuditLogs
            .Where(a => a.Timestamp >= from && a.Timestamp <= to)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return entities.Select(AuditLogMapper.ToModel);
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.AuditLogs.CountAsync();
    }

    public async Task DeleteOlderThanAsync(DateTime date)
    {
        var entities = await _context.AuditLogs
            .Where(a => a.Timestamp < date)
            .ToListAsync();

        if (entities.Any())
        {
            _context.AuditLogs.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
