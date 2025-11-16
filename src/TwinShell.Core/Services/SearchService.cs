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

        var term = searchTerm.ToLowerInvariant();

        var results = actions.Where(a =>
            a.Title.ToLowerInvariant().Contains(term) ||
            a.Description.ToLowerInvariant().Contains(term) ||
            a.Category.ToLowerInvariant().Contains(term) ||
            a.Tags.Any(t => t.ToLowerInvariant().Contains(term)) ||
            (a.Notes != null && a.Notes.ToLowerInvariant().Contains(term)));

        return await Task.FromResult(results);
    }
}
