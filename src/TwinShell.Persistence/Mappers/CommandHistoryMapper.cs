using System.Text.Json;
using TwinShell.Core.Models;
using TwinShell.Persistence.Entities;

namespace TwinShell.Persistence.Mappers;

/// <summary>
/// Maps between CommandHistory domain model and CommandHistoryEntity
/// </summary>
public static class CommandHistoryMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static CommandHistoryEntity ToEntity(CommandHistory history)
    {
        return new CommandHistoryEntity
        {
            Id = history.Id,
            UserId = history.UserId,
            ActionId = history.ActionId,
            GeneratedCommand = history.GeneratedCommand,
            ParametersJson = JsonSerializer.Serialize(history.Parameters, JsonOptions),
            Platform = history.Platform,
            CreatedAt = history.CreatedAt,
            Category = history.Category,
            ActionTitle = history.ActionTitle
        };
    }

    public static CommandHistory ToModel(CommandHistoryEntity entity)
    {
        var history = new CommandHistory
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ActionId = entity.ActionId,
            GeneratedCommand = entity.GeneratedCommand,
            Parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.ParametersJson, JsonOptions)
                ?? new Dictionary<string, string>(),
            Platform = entity.Platform,
            CreatedAt = entity.CreatedAt,
            Category = entity.Category,
            ActionTitle = entity.ActionTitle
        };

        if (entity.Action != null)
        {
            history.Action = ActionMapper.ToModel(entity.Action);
        }

        return history;
    }
}
