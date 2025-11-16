using TwinShell.Core.Models;

namespace TwinShell.Core.Interfaces;

/// <summary>
/// Service for searching actions
/// </summary>
public interface ISearchService
{
    Task<IEnumerable<Action>> SearchAsync(
        IEnumerable<Action> actions,
        string searchTerm);
}
