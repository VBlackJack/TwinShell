using TwinShell.Core.Helpers;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing search history for autocomplete and suggestions.
/// Stores recent searches to improve user experience.
/// </summary>
public class SearchHistoryService : ISearchHistoryService
{
    private readonly ISearchHistoryRepository _repository;

    public SearchHistoryService(ISearchHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task AddSearchAsync(string searchTerm, int resultCount, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return;
        }

        var searchHistory = new SearchHistory
        {
            SearchTerm = searchTerm,
            NormalizedSearchTerm = TextNormalizer.NormalizeForSearch(searchTerm),
            ResultCount = resultCount,
            WasSuccessful = resultCount > 0,
            UserId = userId
        };

        await _repository.AddOrUpdateAsync(searchHistory);
    }

    public async Task<IEnumerable<SearchHistory>> GetRecentSearchesAsync(int limit = 10, string? userId = null)
    {
        return await _repository.GetRecentAsync(limit, userId);
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialTerm, int limit = 5, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(partialTerm))
        {
            // Return recent searches if no partial term provided
            var recent = await _repository.GetRecentAsync(limit, userId);
            return recent.Select(h => h.SearchTerm);
        }

        var matches = await _repository.SearchAsync(partialTerm, limit, userId);
        return matches.Select(h => h.SearchTerm);
    }

    public async Task ClearHistoryAsync(string? userId = null)
    {
        await _repository.ClearAllAsync(userId);
    }

    public async Task DeleteSearchAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<SearchHistory>> GetPopularSearchesAsync(int limit = 10, string? userId = null)
    {
        return await _repository.GetPopularAsync(limit, userId);
    }
}
