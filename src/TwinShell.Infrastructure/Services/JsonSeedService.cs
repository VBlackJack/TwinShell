using System.IO;
using System.Text.Json;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Infrastructure.Services;

/// <summary>
/// Service for seeding initial data from JSON files.
/// Supports both legacy single-file format (initial-actions.json) and
/// new multi-file format (actions/*.json) for better maintainability.
/// </summary>
public class JsonSeedService : ISeedService
{
    private readonly IActionRepository _actionRepository;
    private readonly string _seedBasePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonSeedService(IActionRepository actionRepository, string seedFilePath)
    {
        _actionRepository = actionRepository;
        // Store base path (parent directory of seed file or actions folder)
        _seedBasePath = Path.GetDirectoryName(seedFilePath) ?? seedFilePath;
    }

    public async Task SeedAsync()
    {
        // Check if database is empty
        var existingCount = await _actionRepository.CountAsync().ConfigureAwait(false);
        if (existingCount > 0)
        {
            return;
        }

        var allActions = await LoadAllActionsAsync().ConfigureAwait(false);

        if (allActions.Count == 0)
        {
            Console.WriteLine("Warning: No actions found to seed.");
            return;
        }

        // Insert all validated actions
        var validActionsCount = 0;
        var skippedActionsCount = 0;

        foreach (var action in allActions)
        {
            if (!ValidateAction(action))
            {
                Console.WriteLine($"Warning: Skipping invalid action: {action.Id ?? "unknown"}");
                skippedActionsCount++;
                continue;
            }

            action.IsUserCreated = false;
            action.CreatedAt = DateTime.UtcNow;
            action.UpdatedAt = DateTime.UtcNow;

            await _actionRepository.AddAsync(action).ConfigureAwait(false);
            validActionsCount++;
        }

        Console.WriteLine($"Seeding completed: {validActionsCount} actions inserted, {skippedActionsCount} actions skipped.");
    }

    /// <summary>
    /// Loads all actions from available formats.
    /// Priority: actions/*.json (individual files) > initial-actions.json (legacy)
    /// </summary>
    private async Task<List<ActionModel>> LoadAllActionsAsync()
    {
        var actionsDir = Path.Combine(_seedBasePath, "actions");

        // Try new individual-file format first (one JSON per action)
        if (Directory.Exists(actionsDir))
        {
            var jsonFiles = Directory.GetFiles(actionsDir, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("_"))
                .ToArray();

            if (jsonFiles.Length > 0)
            {
                var actions = await LoadActionsFromIndividualFilesAsync(jsonFiles).ConfigureAwait(false);
                if (actions.Count > 0)
                {
                    Console.WriteLine($"Loaded {actions.Count} actions from {jsonFiles.Length} individual files.");
                    return actions;
                }
            }
        }

        // Fallback to legacy single-file format
        var legacyPath = Path.Combine(_seedBasePath, "initial-actions.json");
        if (File.Exists(legacyPath))
        {
            var actions = await LoadActionsFromLegacyFileAsync(legacyPath).ConfigureAwait(false);
            Console.WriteLine($"Loaded {actions.Count} actions from legacy file.");
            return actions;
        }

        Console.WriteLine($"Warning: No seed files found in {_seedBasePath}");
        return new List<ActionModel>();
    }

    /// <summary>
    /// Loads actions from individual JSON files (one action per file).
    /// </summary>
    private async Task<List<ActionModel>> LoadActionsFromIndividualFilesAsync(string[] jsonFiles)
    {
        var allActions = new List<ActionModel>();
        var errorCount = 0;

        foreach (var filePath in jsonFiles.OrderBy(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

                // Security: Limit file size (100KB per action file)
                const int MaxFileSizeBytes = 100 * 1024;
                if (json.Length > MaxFileSizeBytes)
                {
                    Console.WriteLine($"Warning: File {Path.GetFileName(filePath)} too large, skipping.");
                    errorCount++;
                    continue;
                }

                var action = JsonSerializer.Deserialize<ActionModel>(json, JsonOptions);
                if (action != null)
                {
                    allActions.Add(action);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load {Path.GetFileName(filePath)}: {ex.Message}");
                errorCount++;
            }
        }

        if (errorCount > 0)
        {
            Console.WriteLine($"Warning: {errorCount} files failed to load.");
        }

        return allActions;
    }

    /// <summary>
    /// Loads actions from the legacy single-file format (initial-actions.json).
    /// </summary>
    private async Task<List<ActionModel>> LoadActionsFromLegacyFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

        const int MaxJsonSizeBytes = 10 * 1024 * 1024; // 10 MB
        if (json.Length > MaxJsonSizeBytes)
        {
            Console.WriteLine($"Warning: Seed file too large ({json.Length} bytes).");
            return new List<ActionModel>();
        }

        var seedData = JsonSerializer.Deserialize<LegacySeedData>(json, JsonOptions);
        return seedData?.Actions ?? new List<ActionModel>();
    }

    /// <summary>
    /// Validates action data to prevent injection of malicious content
    /// </summary>
    private static bool ValidateAction(ActionModel action)
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(action.Title) ||
            string.IsNullOrWhiteSpace(action.Category))
        {
            return false;
        }

        // Check field lengths to prevent oversized data
        const int MaxTitleLength = 200;
        const int MaxDescriptionLength = 2000;
        const int MaxCategoryLength = 100;
        const int MaxNotesLength = 5000;

        if (action.Title.Length > MaxTitleLength ||
            action.Category.Length > MaxCategoryLength ||
            (action.Description?.Length ?? 0) > MaxDescriptionLength ||
            (action.Notes?.Length ?? 0) > MaxNotesLength)
        {
            return false;
        }

        // Check collections sizes
        const int MaxTagsCount = 20;
        const int MaxExamplesCount = 10;
        const int MaxLinksCount = 10;

        if ((action.Tags?.Count ?? 0) > MaxTagsCount ||
            (action.Examples?.Count ?? 0) > MaxExamplesCount ||
            (action.WindowsExamples?.Count ?? 0) > MaxExamplesCount ||
            (action.LinuxExamples?.Count ?? 0) > MaxExamplesCount ||
            (action.Links?.Count ?? 0) > MaxLinksCount)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Data structure for legacy single-file format (initial-actions.json).
    /// </summary>
    private class LegacySeedData
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public List<ActionModel> Actions { get; set; } = new();
    }
}
