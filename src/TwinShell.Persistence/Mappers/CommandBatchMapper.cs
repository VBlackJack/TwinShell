using System.Text.Json;
using TwinShell.Core.Models;
using TwinShell.Persistence.Entities;

namespace TwinShell.Persistence.Mappers;

/// <summary>
/// Maps between CommandBatch domain model and CommandBatchEntity
/// </summary>
public static class CommandBatchMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static CommandBatchEntity ToEntity(CommandBatch batch)
    {
        return new CommandBatchEntity
        {
            Id = batch.Id,
            Name = batch.Name,
            Description = batch.Description,
            ExecutionMode = batch.ExecutionMode,
            CommandsJson = JsonSerializer.Serialize(batch.Commands, JsonOptions),
            TagsJson = JsonSerializer.Serialize(batch.Tags, JsonOptions),
            CreatedAt = batch.CreatedAt,
            UpdatedAt = batch.UpdatedAt,
            LastExecutedAt = batch.LastExecutedAt,
            IsUserCreated = batch.IsUserCreated
        };
    }

    public static CommandBatch ToModel(CommandBatchEntity entity)
    {
        return new CommandBatch
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ExecutionMode = entity.ExecutionMode,
            Commands = JsonSerializer.Deserialize<List<BatchCommandItem>>(entity.CommandsJson, JsonOptions) ?? new List<BatchCommandItem>(),
            Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson, JsonOptions) ?? new List<string>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            LastExecutedAt = entity.LastExecutedAt,
            IsUserCreated = entity.IsUserCreated
        };
    }
}
