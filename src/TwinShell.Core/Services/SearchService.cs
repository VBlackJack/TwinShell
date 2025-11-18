using TwinShell.Core.Helpers;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for searching actions with advanced text normalization.
/// Supports case-insensitive, accent-insensitive, and punctuation-normalized search.
/// Multi-word queries require all words to match (AND logic).
/// </summary>
public class SearchService : ISearchService
{
    /// <summary>
    /// Searches actions based on normalized text matching.
    /// Search fields: Title, Description, Category, Tags, Notes, Command Templates (Name, Pattern).
    ///
    /// Normalization includes:
    /// - Case-insensitive matching
    /// - Accent/diacritic removal (café = cafe, réseau = reseau)
    /// - Punctuation normalization (Get-Service = Get Service = GetService)
    /// - Multi-word search with AND logic (all words must match)
    /// </summary>
    /// <param name="actions">Actions to search</param>
    /// <param name="searchTerm">Search query (can be multi-word)</param>
    /// <returns>Matching actions</returns>
    public async Task<IEnumerable<ActionModel>> SearchAsync(
        IEnumerable<ActionModel> actions,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return actions;
        }

        // Normalize search term and extract tokens for multi-word search
        var normalizedSearchTerm = TextNormalizer.NormalizeForSearch(searchTerm);
        var searchTokens = TextNormalizer.GetSearchTokens(normalizedSearchTerm);

        // If no valid tokens after normalization, return all actions
        if (searchTokens.Length == 0)
        {
            return actions;
        }

        // PERFORMANCE: Single-pass filter with normalized matching
        var results = actions.Where(action => ActionMatchesSearch(action, normalizedSearchTerm, searchTokens));

        return await Task.FromResult(results);
    }

    /// <summary>
    /// Checks if an action matches the search criteria.
    /// Builds a normalized searchable text from all relevant fields and checks token matching.
    /// </summary>
    private bool ActionMatchesSearch(ActionModel action, string normalizedSearchTerm, string[] searchTokens)
    {
        // Build a single normalized searchable string from all fields
        // This is more efficient than checking each field separately with Contains
        var searchableText = BuildNormalizedSearchableText(action);

        // Check if all search tokens are present (AND logic)
        // This allows "AD User" to match "Active Directory User Management"
        return TextNormalizer.ContainsAllTokens(searchableText, searchTokens);
    }

    /// <summary>
    /// Builds a single normalized searchable string from all action fields.
    /// Includes: Title, Description, Category, Tags, Notes, Template Names, Template Patterns.
    /// </summary>
    private string BuildNormalizedSearchableText(ActionModel action)
    {
        var parts = new List<string>
        {
            TextNormalizer.NormalizeForSearch(action.Title),
            TextNormalizer.NormalizeForSearch(action.Description),
            TextNormalizer.NormalizeForSearch(action.Category)
        };

        // Add all tags
        if (action.Tags != null && action.Tags.Count > 0)
        {
            parts.AddRange(action.Tags.Select(TextNormalizer.NormalizeForSearch));
        }

        // Add notes if present
        if (!string.IsNullOrWhiteSpace(action.Notes))
        {
            parts.Add(TextNormalizer.NormalizeForSearch(action.Notes));
        }

        // Add Windows command template fields
        if (action.WindowsCommandTemplate != null)
        {
            if (!string.IsNullOrWhiteSpace(action.WindowsCommandTemplate.Name))
            {
                parts.Add(TextNormalizer.NormalizeForSearch(action.WindowsCommandTemplate.Name));
            }
            if (!string.IsNullOrWhiteSpace(action.WindowsCommandTemplate.CommandPattern))
            {
                parts.Add(TextNormalizer.NormalizeForSearch(action.WindowsCommandTemplate.CommandPattern));
            }
        }

        // Add Linux command template fields
        if (action.LinuxCommandTemplate != null)
        {
            if (!string.IsNullOrWhiteSpace(action.LinuxCommandTemplate.Name))
            {
                parts.Add(TextNormalizer.NormalizeForSearch(action.LinuxCommandTemplate.Name));
            }
            if (!string.IsNullOrWhiteSpace(action.LinuxCommandTemplate.CommandPattern))
            {
                parts.Add(TextNormalizer.NormalizeForSearch(action.LinuxCommandTemplate.CommandPattern));
            }
        }

        // Join all parts with space separator
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}

