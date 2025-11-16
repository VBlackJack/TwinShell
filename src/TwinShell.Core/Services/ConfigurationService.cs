using System.Text.Json;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for configuration export/import
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IFavoritesRepository _favoritesRepository;
    private readonly ICommandHistoryRepository _historyRepository;
    private readonly IActionRepository _actionRepository;
    private readonly string _baseExportDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigurationService(
        IFavoritesRepository favoritesRepository,
        ICommandHistoryRepository historyRepository,
        IActionRepository actionRepository)
    {
        _favoritesRepository = favoritesRepository;
        _historyRepository = historyRepository;
        _actionRepository = actionRepository;

        // Configure base directory for exports (sandbox all file operations)
        _baseExportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TwinShell",
            "Exports");

        // Ensure base directory exists
        if (!Directory.Exists(_baseExportDirectory))
        {
            Directory.CreateDirectory(_baseExportDirectory);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ExportToJsonAsync(
        string filePath,
        string? userId = null,
        bool includeHistory = true)
    {
        try
        {
            // SECURITY: Validate file path to prevent path traversal
            if (!IsPathSecure(filePath))
            {
                return (false, "Invalid file path: path must be within the allowed directory");
            }

            // SECURITY: Validate file extension
            if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Invalid file extension: file must be a .json file");
            }

            var config = new UserConfigurationDto
            {
                Version = "1.0",
                ExportDate = DateTime.UtcNow,
                UserId = userId
            };

            // Export favorites
            var favorites = await _favoritesRepository.GetAllAsync(userId);
            config.Favorites = favorites.Select(f => new FavoriteDto
            {
                ActionId = f.ActionId,
                CreatedAt = f.CreatedAt,
                DisplayOrder = f.DisplayOrder
            }).ToList();

            // Export history if requested
            if (includeHistory)
            {
                var history = await _historyRepository.GetRecentAsync(1000); // Last 1000 commands
                config.History = history.Select(h => new CommandHistoryDto
                {
                    ActionId = h.ActionId,
                    GeneratedCommand = h.GeneratedCommand,
                    Parameters = h.Parameters,
                    Platform = h.Platform.ToString(),
                    CreatedAt = h.CreatedAt,
                    Category = h.Category,
                    ActionTitle = h.ActionTitle
                }).ToList();
            }

            // Add default preferences (can be extended later)
            config.Preferences = new Dictionary<string, string>
            {
                { "HistoryRetentionDays", "90" },
                { "MaxFavorites", "50" }
            };

            // Write to file
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, "Export operation failed");
        }
    }

    public async Task<(bool Success, string? ErrorMessage, int FavoritesImported, int HistoryImported)> ImportFromJsonAsync(
        string filePath,
        string? userId = null,
        bool mergeMode = true)
    {
        try
        {
            // SECURITY: Validate file path to prevent path traversal
            if (!IsPathSecure(filePath))
            {
                return (false, "Invalid file path: path must be within the allowed directory", 0, 0);
            }

            // Validate file exists
            if (!File.Exists(filePath))
            {
                return (false, "File not found", 0, 0);
            }

            // SECURITY: Validate file size (max 10MB)
            var fileInfo = new FileInfo(filePath);
            const long maxSize = 10 * 1024 * 1024; // 10 MB
            if (fileInfo.Length > maxSize)
            {
                return (false, "File too large: maximum size is 10MB", 0, 0);
            }

            // Read and parse JSON
            var json = await File.ReadAllTextAsync(filePath);

            // SECURITY: Validate JSON schema before deserialization
            if (!ValidateJsonSchema(json))
            {
                return (false, "Invalid JSON structure", 0, 0);
            }

            var config = JsonSerializer.Deserialize<UserConfigurationDto>(json, JsonOptions);

            if (config == null)
            {
                return (false, "Invalid JSON format", 0, 0);
            }

            // Validate version
            if (!string.IsNullOrEmpty(config.Version) && config.Version != "1.0")
            {
                // Future versions might need migration logic
                return (false, $"Unsupported configuration version: {config.Version}. This application supports version 1.0.", 0, 0);
            }

            int favoritesImported = 0;
            int historyImported = 0;

            // PERFORMANCE: Load all valid action IDs once to avoid N+1 queries
            var allActions = await _actionRepository.GetAllAsync();
            var validActionIds = allActions.Select(a => a.Id).ToHashSet();

            // Get existing favorites if in merge mode
            HashSet<string> existingFavoriteActionIds = new();
            if (mergeMode)
            {
                var existingFavorites = await _favoritesRepository.GetAllAsync(userId);
                existingFavoriteActionIds = existingFavorites.Select(f => f.ActionId).ToHashSet();
            }

            // PERFORMANCE: Get initial count once instead of in loop
            var currentFavoritesCount = await _favoritesRepository.GetCountAsync(userId);

            // Import favorites
            foreach (var favoriteDto in config.Favorites)
            {
                // Skip if already exists in merge mode
                if (mergeMode && existingFavoriteActionIds.Contains(favoriteDto.ActionId))
                {
                    continue;
                }

                // PERFORMANCE: Check existence in memory instead of database query
                if (!validActionIds.Contains(favoriteDto.ActionId))
                {
                    continue; // Skip invalid action IDs
                }

                // PERFORMANCE: Check limit with local counter
                if (currentFavoritesCount >= 50)
                {
                    break; // Stop if limit reached
                }

                var favorite = new UserFavorite
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ActionId = favoriteDto.ActionId,
                    CreatedAt = favoriteDto.CreatedAt,
                    DisplayOrder = favoriteDto.DisplayOrder
                };

                await _favoritesRepository.AddAsync(favorite);
                favoritesImported++;
                currentFavoritesCount++; // Increment local counter
            }

            // Import history
            foreach (var historyDto in config.History)
            {
                // PERFORMANCE: Check existence in memory instead of database query
                if (!validActionIds.Contains(historyDto.ActionId))
                {
                    continue; // Skip invalid action IDs
                }

                // Parse platform
                if (!Enum.TryParse<Platform>(historyDto.Platform, out var platform))
                {
                    platform = Platform.Both;
                }

                var history = new CommandHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ActionId = historyDto.ActionId,
                    GeneratedCommand = historyDto.GeneratedCommand,
                    Parameters = historyDto.Parameters,
                    Platform = platform,
                    CreatedAt = historyDto.CreatedAt,
                    Category = historyDto.Category,
                    ActionTitle = historyDto.ActionTitle
                };

                await _historyRepository.AddAsync(history);
                historyImported++;
            }

            return (true, null, favoritesImported, historyImported);
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON format: {ex.Message}", 0, 0);
        }
        catch (Exception ex)
        {
            return (false, $"Import failed: {ex.Message}", 0, 0);
        }
    }

    public async Task<(bool IsValid, string? ErrorMessage, string? Version)> ValidateConfigurationFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return (false, "File not found", null);
            }

            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<UserConfigurationDto>(json, JsonOptions);

            if (config == null)
            {
                return (false, "Invalid JSON format", null);
            }

            // Basic validation
            if (string.IsNullOrEmpty(config.Version))
            {
                return (false, "Missing version information", null);
            }

            if (config.Favorites == null)
            {
                return (false, "Missing favorites data", config.Version);
            }

            if (config.History == null)
            {
                return (false, "Missing history data", config.Version);
            }

            return (true, null, config.Version);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}", null);
        }
        catch (Exception ex)
        {
            return (false, "Validation error", null);
        }
    }

    /// <summary>
    /// Validates that a file path is within the allowed directory (prevents path traversal)
    /// </summary>
    private bool IsPathSecure(string filePath)
    {
        try
        {
            // Get the absolute path
            var fullPath = Path.GetFullPath(filePath);
            var baseDirectory = Path.GetFullPath(_baseExportDirectory);

            // Check that the path starts with the base directory
            if (!fullPath.StartsWith(baseDirectory + Path.DirectorySeparatorChar) &&
                fullPath != baseDirectory)
            {
                return false;
            }

            // Check for path traversal sequences
            if (fullPath.Contains("..") || fullPath.Contains("~"))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates JSON structure before deserialization (prevents malicious JSON)
    /// </summary>
    private bool ValidateJsonSchema(string json)
    {
        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                // Verify root is an object
                if (root.ValueKind != JsonValueKind.Object)
                    return false;

                // Verify required properties exist
                if (!root.TryGetProperty("Version", out _))
                    return false;

                if (!root.TryGetProperty("Favorites", out var favorites))
                    return false;

                if (favorites.ValueKind != JsonValueKind.Array)
                    return false;

                if (!root.TryGetProperty("History", out var history))
                    return false;

                if (history.ValueKind != JsonValueKind.Array)
                    return false;

                // Validate array lengths (prevent DoS via huge arrays)
                if (favorites.GetArrayLength() > 1000)
                    return false;

                if (history.GetArrayLength() > 10000)
                    return false;

                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
