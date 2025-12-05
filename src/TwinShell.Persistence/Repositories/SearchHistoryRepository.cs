using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Helpers;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Persistence.Mappers;

namespace TwinShell.Persistence.Repositories;

/// <summary>
/// Repository implementation for SearchHistory persistence
/// </summary>
public class SearchHistoryRepository : ISearchHistoryRepository
{
    private readonly TwinShellDbContext _context;

    public SearchHistoryRepository(TwinShellDbContext context)
    {
        _context = context;
    }

    public async Task<SearchHistory> AddOrUpdateAsync(SearchHistory searchHistory)
    {
        // Normalize the search term for consistent storage
        searchHistory.NormalizedSearchTerm = TextNormalizer.NormalizeForSearch(searchHistory.SearchTerm);

        // Try to find existing entry
        var existing = await GetByNormalizedTermAsync(searchHistory.NormalizedSearchTerm, searchHistory.UserId);

        if (existing != null)
        {
            // Update existing entry
            existing.SearchCount++;
            existing.ResultCount = searchHistory.ResultCount;
            existing.LastSearchedAt = DateTime.UtcNow;
            existing.WasSuccessful = searchHistory.WasSuccessful;

            var entity = SearchHistoryMapper.ToEntity(existing);
            _context.SearchHistories.Update(entity);
        }
        else
        {
            // Add new entry
            searchHistory.Id = Guid.NewGuid().ToString();
            searchHistory.CreatedAt = DateTime.UtcNow;
            searchHistory.LastSearchedAt = DateTime.UtcNow;

            var entity = SearchHistoryMapper.ToEntity(searchHistory);
            _context.SearchHistories.Add(entity);
        }

        await _context.SaveChangesAsync();
        return existing ?? searchHistory;
    }

    public async Task<IEnumerable<SearchHistory>> GetRecentAsync(int limit = 10, string? userId = null)
    {
        var query = _context.SearchHistories
            .AsNoTracking()
            .Where(h => h.WasSuccessful);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(h => h.UserId == userId);
        }

        var entities = await query
            .OrderByDescending(h => h.LastSearchedAt)
            .Take(limit)
            .ToListAsync();

        return entities.Select(SearchHistoryMapper.ToModel);
    }

    public async Task<IEnumerable<SearchHistory>> GetPopularAsync(int limit = 10, string? userId = null)
    {
        var query = _context.SearchHistories
            .AsNoTracking()
            .Where(h => h.WasSuccessful);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(h => h.UserId == userId);
        }

        var entities = await query
            .OrderByDescending(h => h.SearchCount)
            .ThenByDescending(h => h.LastSearchedAt)
            .Take(limit)
            .ToListAsync();

        return entities.Select(SearchHistoryMapper.ToModel);
    }

    public async Task<IEnumerable<SearchHistory>> SearchAsync(string partialTerm, int limit = 5, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(partialTerm))
        {
            return Enumerable.Empty<SearchHistory>();
        }

        var normalizedPartial = TextNormalizer.NormalizeForSearch(partialTerm);
        var search = $"%{normalizedPartial}%";

        var query = _context.SearchHistories
            .AsNoTracking()
            .Where(h => h.WasSuccessful);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(h => h.UserId == userId);
        }

        var entities = await query
            .Where(h => EF.Functions.Like(h.NormalizedSearchTerm, search))
            .OrderByDescending(h => h.SearchCount)
            .ThenByDescending(h => h.LastSearchedAt)
            .Take(limit)
            .ToListAsync();

        return entities.Select(SearchHistoryMapper.ToModel);
    }

    public async Task<SearchHistory?> GetByNormalizedTermAsync(string normalizedSearchTerm, string? userId = null)
    {
        var query = _context.SearchHistories
            .AsNoTracking()
            .Where(h => h.NormalizedSearchTerm == normalizedSearchTerm);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(h => h.UserId == userId);
        }
        else
        {
            query = query.Where(h => h.UserId == null);
        }

        var entity = await query.FirstOrDefaultAsync();
        return entity != null ? SearchHistoryMapper.ToModel(entity) : null;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.SearchHistories.FindAsync(id);
        if (entity != null)
        {
            _context.SearchHistories.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearAllAsync(string? userId = null)
    {
        var query = _context.SearchHistories.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(h => h.UserId == userId);
        }

        _context.SearchHistories.RemoveRange(query);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOlderThanAsync(int daysToKeep)
    {
        if (daysToKeep <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysToKeep), "Value must be positive");
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        var entitiesToDelete = await _context.SearchHistories
            .Where(h => h.LastSearchedAt < cutoffDate)
            .ToListAsync();

        _context.SearchHistories.RemoveRange(entitiesToDelete);
        await _context.SaveChangesAsync();
    }
}
