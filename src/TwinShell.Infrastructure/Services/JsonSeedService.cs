using System.IO;
using System.Text.Json;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Infrastructure.Services;

/// <summary>
/// Service for seeding initial data from JSON file
/// </summary>
public class JsonSeedService : ISeedService
{
    private readonly IActionRepository _actionRepository;
    private readonly string _seedFilePath;

    public JsonSeedService(IActionRepository actionRepository, string seedFilePath)
    {
        _actionRepository = actionRepository;
        _seedFilePath = seedFilePath;
    }

    public async Task SeedAsync()
    {
        // Check if database is empty
        // BUGFIX: ConfigureAwait(false) prevents deadlock when called from UI thread
        var existingCount = await _actionRepository.CountAsync().ConfigureAwait(false);
        if (existingCount > 0)
        {
            // Database already has data, skip seeding
            return;
        }

        // BUGFIX: Return early with warning instead of throwing FileNotFoundException
        // This allows the application to start even if the seed file is missing
        if (!File.Exists(_seedFilePath))
        {
            Console.WriteLine($"Warning: Seed file not found: {_seedFilePath}. Skipping data seeding.");
            return;
        }

        var json = await File.ReadAllTextAsync(_seedFilePath).ConfigureAwait(false);

        // SECURITY: Limit JSON size to prevent DoS
        const int MaxJsonSizeBytes = 10 * 1024 * 1024; // 10 MB
        if (json.Length > MaxJsonSizeBytes)
        {
            Console.WriteLine($"Warning: Seed file too large ({json.Length} bytes). Maximum allowed is {MaxJsonSizeBytes} bytes.");
            return;
        }

        var seedData = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData?.Actions == null || seedData.Actions.Count == 0)
        {
            return;
        }

        // SECURITY: Validate and sanitize each action before insertion
        var validActionsCount = 0;
        var skippedActionsCount = 0;

        foreach (var action in seedData.Actions)
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

    private class SeedData
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public List<ActionModel> Actions { get; set; } = new();
    }
}
