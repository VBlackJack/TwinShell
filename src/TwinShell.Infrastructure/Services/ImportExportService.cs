using System.IO;
using System.Text.Json;
using TwinShell.Core.Constants;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Infrastructure.Services;

/// <summary>
/// Service for importing and exporting actions to/from JSON files
/// </summary>
public class ImportExportService : IImportExportService
{
    private readonly IActionRepository _actionRepository;

    public ImportExportService(IActionRepository actionRepository)
    {
        _actionRepository = actionRepository;
    }

    public async Task<ExportResult> ExportActionsAsync(string filePath)
    {
        try
        {
            // Get all actions from database
            var actions = (await _actionRepository.GetAllAsync()).ToList();

            // Create export data structure compatible with initial-actions.json format
            var exportData = new
            {
                SchemaVersion = "1.0",
                ExportDate = DateTime.UtcNow,
                TotalActions = actions.Count,
                Actions = actions.Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.Category,
                    Platform = (int)a.Platform,
                    Level = (int)a.Level,
                    a.Tags,
                    // BUGFIX: Use ID field directly instead of navigation property to prevent data loss
                    a.WindowsCommandTemplateId,
                    WindowsCommandTemplate = a.WindowsCommandTemplate != null ? new
                    {
                        a.WindowsCommandTemplate.Id,
                        Platform = (int)a.WindowsCommandTemplate.Platform,
                        a.WindowsCommandTemplate.Name,
                        a.WindowsCommandTemplate.CommandPattern,
                        Parameters = a.WindowsCommandTemplate.Parameters.Select(p => new
                        {
                            p.Name,
                            p.Label,
                            p.Type,
                            p.DefaultValue,
                            p.Required,
                            p.Description
                        })
                    } : null,
                    // BUGFIX: Use ID field directly instead of navigation property to prevent data loss
                    a.LinuxCommandTemplateId,
                    LinuxCommandTemplate = a.LinuxCommandTemplate != null ? new
                    {
                        a.LinuxCommandTemplate.Id,
                        Platform = (int)a.LinuxCommandTemplate.Platform,
                        a.LinuxCommandTemplate.Name,
                        a.LinuxCommandTemplate.CommandPattern,
                        Parameters = a.LinuxCommandTemplate.Parameters.Select(p => new
                        {
                            p.Name,
                            p.Label,
                            p.Type,
                            p.DefaultValue,
                            p.Required,
                            p.Description
                        })
                    } : null,
                    a.Examples,
                    a.Notes,
                    a.Links,
                    a.IsUserCreated,
                    a.CreatedAt,
                    a.UpdatedAt
                }).ToList()
            };

            // Serialize to JSON with formatting
            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Write to file
            await File.WriteAllTextAsync(filePath, json);

            return new ExportResult
            {
                Success = true,
                ActionCount = actions.Count
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                ActionCount = 0,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ImportResult> ImportActionsAsync(string filePath, ImportMode mode)
    {
        try
        {
            // SECURITY: Validate file size to prevent DoS
            var fileInfo = new FileInfo(filePath);
            const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = $"File too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB."
                };
            }

            // Read and parse JSON
            var json = await File.ReadAllTextAsync(filePath);
            var importData = JsonSerializer.Deserialize<ImportData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (importData?.Actions == null || importData.Actions.Count == 0)
            {
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = "Invalid file format or no actions found."
                };
            }

            int imported = 0, updated = 0, skipped = 0;

            // Replace mode: delete all user-created actions first
            if (mode == ImportMode.Replace)
            {
                var existingActions = await _actionRepository.GetAllAsync();
                foreach (var action in existingActions.Where(a => a.IsUserCreated))
                {
                    await _actionRepository.DeleteAsync(action.Id);
                }
            }

            // Import actions
            foreach (var actionData in importData.Actions)
            {
                // SECURITY: Validate action data
                if (!ValidateActionData(actionData))
                {
                    skipped++;
                    continue;
                }

                var existingAction = await _actionRepository.GetByIdAsync(actionData.Id);

                if (existingAction != null)
                {
                    // Action exists
                    if (mode == ImportMode.Merge)
                    {
                        // Only update if it's a user-created action
                        if (existingAction.IsUserCreated)
                        {
                            MapToAction(actionData, existingAction);
                            existingAction.UpdatedAt = DateTime.UtcNow;
                            await _actionRepository.UpdateAsync(existingAction);
                            updated++;
                        }
                        else
                        {
                            // SECURITY: Skip system actions to prevent overwriting
                            skipped++;
                        }
                    }
                    else
                    {
                        // Replace mode: action should have been deleted above if user-created
                        // If it still exists, it's a system action - skip
                        skipped++;
                    }
                }
                else
                {
                    // New action - import it
                    var newAction = MapToAction(actionData);
                    // BUGFIX: Preserve IsUserCreated from import data instead of forcing to true
                    // This allows re-importing system actions correctly
                    newAction.IsUserCreated = actionData.IsUserCreated;
                    newAction.CreatedAt = DateTime.UtcNow;
                    newAction.UpdatedAt = DateTime.UtcNow;
                    await _actionRepository.AddAsync(newAction);
                    imported++;
                }
            }

            return new ImportResult
            {
                Success = true,
                Imported = imported,
                Updated = updated,
                Skipped = skipped,
                Message = $"{imported} imported, {updated} updated, {skipped} skipped."
            };
        }
        catch (JsonException ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Invalid JSON format: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}"
            };
        }
    }

    public async Task<ValidationResult> ValidateImportFileAsync(string filePath)
    {
        try
        {
            // Check file exists
            if (!File.Exists(filePath))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "File not found."
                };
            }

            // Check file size
            var fileInfo = new FileInfo(filePath);
            const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB."
                };
            }

            // Parse JSON
            var json = await File.ReadAllTextAsync(filePath);
            var importData = JsonSerializer.Deserialize<ImportData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (importData?.Actions == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid file format: missing 'actions' array."
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                SchemaVersion = importData.SchemaVersion ?? "1.0",
                ActionCount = importData.Actions.Count
            };
        }
        catch (JsonException ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid JSON: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// SECURITY: Validates action data to prevent injection of malicious content
    /// Uses centralized validation constants for consistency
    /// </summary>
    private static bool ValidateActionData(ActionModel action)
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(action.Title) ||
            string.IsNullOrWhiteSpace(action.Category))
        {
            return false;
        }

        // SECURITY: Check field lengths to prevent oversized data
        // BUGFIX: Use centralized constants for validation consistency
        if (action.Title.Length > ValidationConstants.MaxActionTitleLength ||
            action.Category.Length > ValidationConstants.MaxActionCategoryLength ||
            (action.Description?.Length ?? 0) > ValidationConstants.MaxActionDescriptionLength ||
            (action.Notes?.Length ?? 0) > ValidationConstants.MaxActionNotesLength)
        {
            return false;
        }

        // SECURITY: Check collections sizes
        if ((action.Tags?.Count ?? 0) > ValidationConstants.MaxActionTagsCount ||
            (action.Examples?.Count ?? 0) > ValidationConstants.MaxActionExamplesCount ||
            (action.Links?.Count ?? 0) > ValidationConstants.MaxActionLinksCount)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Maps import data to an Action model (new instance)
    /// </summary>
    private static ActionModel MapToAction(ActionModel source)
    {
        var action = new ActionModel
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description ?? string.Empty,
            Category = source.Category,
            Platform = source.Platform,
            Level = source.Level,
            Tags = source.Tags ?? new List<string>(),
            Notes = source.Notes,
            Examples = source.Examples ?? new List<CommandExample>(),
            Links = source.Links ?? new List<ExternalLink>()
        };

        // Map Windows command template
        if (source.WindowsCommandTemplate != null)
        {
            action.WindowsCommandTemplateId = source.WindowsCommandTemplate.Id;
            action.WindowsCommandTemplate = new CommandTemplate
            {
                Id = source.WindowsCommandTemplate.Id,
                Platform = source.WindowsCommandTemplate.Platform,
                Name = source.WindowsCommandTemplate.Name,
                CommandPattern = source.WindowsCommandTemplate.CommandPattern,
                Parameters = source.WindowsCommandTemplate.Parameters?.Select(p => new TemplateParameter
                {
                    Name = p.Name,
                    Label = p.Label,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue,
                    Required = p.Required,
                    Description = p.Description
                }).ToList() ?? new List<TemplateParameter>()
            };
        }

        // Map Linux command template
        if (source.LinuxCommandTemplate != null)
        {
            action.LinuxCommandTemplateId = source.LinuxCommandTemplate.Id;
            action.LinuxCommandTemplate = new CommandTemplate
            {
                Id = source.LinuxCommandTemplate.Id,
                Platform = source.LinuxCommandTemplate.Platform,
                Name = source.LinuxCommandTemplate.Name,
                CommandPattern = source.LinuxCommandTemplate.CommandPattern,
                Parameters = source.LinuxCommandTemplate.Parameters?.Select(p => new TemplateParameter
                {
                    Name = p.Name,
                    Label = p.Label,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue,
                    Required = p.Required,
                    Description = p.Description
                }).ToList() ?? new List<TemplateParameter>()
            };
        }

        return action;
    }

    /// <summary>
    /// Maps import data to an existing Action model (update in place)
    /// </summary>
    private static void MapToAction(ActionModel source, ActionModel destination)
    {
        destination.Title = source.Title;
        destination.Description = source.Description ?? string.Empty;
        destination.Category = source.Category;
        destination.Platform = source.Platform;
        destination.Level = source.Level;
        destination.Tags = source.Tags ?? new List<string>();
        destination.Notes = source.Notes;
        destination.Examples = source.Examples ?? new List<CommandExample>();
        destination.Links = source.Links ?? new List<ExternalLink>();

        // Update Windows command template
        if (source.WindowsCommandTemplate != null)
        {
            destination.WindowsCommandTemplateId = source.WindowsCommandTemplate.Id;
            destination.WindowsCommandTemplate = new CommandTemplate
            {
                Id = source.WindowsCommandTemplate.Id,
                Platform = source.WindowsCommandTemplate.Platform,
                Name = source.WindowsCommandTemplate.Name,
                CommandPattern = source.WindowsCommandTemplate.CommandPattern,
                Parameters = source.WindowsCommandTemplate.Parameters?.Select(p => new TemplateParameter
                {
                    Name = p.Name,
                    Label = p.Label,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue,
                    Required = p.Required,
                    Description = p.Description
                }).ToList() ?? new List<TemplateParameter>()
            };
        }
        else
        {
            destination.WindowsCommandTemplateId = null;
            destination.WindowsCommandTemplate = null;
        }

        // Update Linux command template
        if (source.LinuxCommandTemplate != null)
        {
            destination.LinuxCommandTemplateId = source.LinuxCommandTemplate.Id;
            destination.LinuxCommandTemplate = new CommandTemplate
            {
                Id = source.LinuxCommandTemplate.Id,
                Platform = source.LinuxCommandTemplate.Platform,
                Name = source.LinuxCommandTemplate.Name,
                CommandPattern = source.LinuxCommandTemplate.CommandPattern,
                Parameters = source.LinuxCommandTemplate.Parameters?.Select(p => new TemplateParameter
                {
                    Name = p.Name,
                    Label = p.Label,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue,
                    Required = p.Required,
                    Description = p.Description
                }).ToList() ?? new List<TemplateParameter>()
            };
        }
        else
        {
            destination.LinuxCommandTemplateId = null;
            destination.LinuxCommandTemplate = null;
        }
    }

    /// <summary>
    /// Data structure for JSON import (compatible with initial-actions.json)
    /// </summary>
    private class ImportData
    {
        public string? SchemaVersion { get; set; }
        public List<ActionModel> Actions { get; set; } = new();
    }
}
