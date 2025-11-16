using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Mappers;

namespace TwinShell.Persistence.Repositories;

/// <summary>
/// Repository implementation for UserFavorite persistence
/// </summary>
public class FavoritesRepository : IFavoritesRepository
{
    private readonly TwinShellDbContext _context;

    public FavoritesRepository(TwinShellDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserFavorite favorite)
    {
        var entity = UserFavoriteMapper.ToEntity(favorite);
        _context.UserFavorites.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserFavorite>> GetAllAsync(string? userId = null)
    {
        var query = _context.UserFavorites
            .Include(f => f.Action)
                .ThenInclude(a => a!.WindowsCommandTemplate)
            .Include(f => f.Action)
                .ThenInclude(a => a!.LinuxCommandTemplate)
            .AsQueryable();

        if (userId != null)
        {
            query = query.Where(f => f.UserId == userId);
        }
        else
        {
            query = query.Where(f => f.UserId == null);
        }

        var entities = await query
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        return entities.Select(UserFavoriteMapper.ToModel);
    }

    public async Task<UserFavorite?> GetByActionIdAsync(string actionId, string? userId = null)
    {
        var query = _context.UserFavorites
            .Include(f => f.Action)
            .AsQueryable();

        if (userId != null)
        {
            query = query.Where(f => f.UserId == userId && f.ActionId == actionId);
        }
        else
        {
            query = query.Where(f => f.UserId == null && f.ActionId == actionId);
        }

        var entity = await query.FirstOrDefaultAsync();
        return entity != null ? UserFavoriteMapper.ToModel(entity) : null;
    }

    public async Task<bool> IsFavoriteAsync(string actionId, string? userId = null)
    {
        if (userId != null)
        {
            return await _context.UserFavorites
                .AnyAsync(f => f.UserId == userId && f.ActionId == actionId);
        }
        else
        {
            return await _context.UserFavorites
                .AnyAsync(f => f.UserId == null && f.ActionId == actionId);
        }
    }

    public async Task RemoveAsync(string favoriteId)
    {
        var entity = await _context.UserFavorites.FindAsync(favoriteId);
        if (entity != null)
        {
            _context.UserFavorites.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveByActionIdAsync(string actionId, string? userId = null)
    {
        var query = _context.UserFavorites.AsQueryable();

        if (userId != null)
        {
            query = query.Where(f => f.UserId == userId && f.ActionId == actionId);
        }
        else
        {
            query = query.Where(f => f.UserId == null && f.ActionId == actionId);
        }

        var entity = await query.FirstOrDefaultAsync();
        if (entity != null)
        {
            _context.UserFavorites.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountAsync(string? userId = null)
    {
        if (userId != null)
        {
            return await _context.UserFavorites.CountAsync(f => f.UserId == userId);
        }
        else
        {
            return await _context.UserFavorites.CountAsync(f => f.UserId == null);
        }
    }

    public async Task UpdateDisplayOrderAsync(string favoriteId, int newOrder)
    {
        var entity = await _context.UserFavorites.FindAsync(favoriteId);
        if (entity != null)
        {
            entity.DisplayOrder = newOrder;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearAllAsync(string? userId = null)
    {
        var query = _context.UserFavorites.AsQueryable();

        if (userId != null)
        {
            query = query.Where(f => f.UserId == userId);
        }
        else
        {
            query = query.Where(f => f.UserId == null);
        }

        var entities = await query.ToListAsync();
        if (entities.Any())
        {
            _context.UserFavorites.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
