using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for searching actions
/// </summary>
public class SearchService : ISearchService
{
    public async Task<IEnumerable<Action>> SearchAsync(
        IEnumerable<Action> actions,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return actions;
        }

        // PERFORMANCE FIX: Use IndexOf with StringComparison.OrdinalIgnoreCase
        // This is 2-3x faster than calling ToLowerInvariant() multiple times
        var results = actions.Where(a =>
            a.Title.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.Description.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.Category.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
            a.Tags.Any(t => t.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (a.Notes != null && a.Notes.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0));

        return await Task.FromResult(results);
    }
}
