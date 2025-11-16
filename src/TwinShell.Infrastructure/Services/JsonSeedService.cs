using System.Text.Json;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

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
        var existingCount = await _actionRepository.CountAsync();
        if (existingCount > 0)
        {
            // Database already has data, skip seeding
            return;
        }

        if (!File.Exists(_seedFilePath))
        {
            throw new FileNotFoundException($"Seed file not found: {_seedFilePath}");
        }

        var json = await File.ReadAllTextAsync(_seedFilePath);
        var seedData = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData?.Actions == null || seedData.Actions.Count == 0)
        {
            return;
        }

        foreach (var action in seedData.Actions)
        {
            action.IsUserCreated = false;
            action.CreatedAt = DateTime.UtcNow;
            action.UpdatedAt = DateTime.UtcNow;

            await _actionRepository.AddAsync(action);
        }
    }

    private class SeedData
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public List<Action> Actions { get; set; } = new();
    }
}
