using System.Text.Json;
using TwinShell.Core.Models;
using TwinShell.Persistence.Entities;

namespace TwinShell.Persistence.Mappers;

/// <summary>
/// Maps between CommandTemplate domain model and CommandTemplateEntity
/// </summary>
public static class CommandTemplateMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static CommandTemplateEntity ToEntity(CommandTemplate template)
    {
        return new CommandTemplateEntity
        {
            Id = template.Id,
            Platform = template.Platform,
            Name = template.Name,
            CommandPattern = template.CommandPattern,
            ParametersJson = JsonSerializer.Serialize(template.Parameters, JsonOptions)
        };
    }

    public static CommandTemplate ToModel(CommandTemplateEntity entity)
    {
        return new CommandTemplate
        {
            Id = entity.Id,
            Platform = entity.Platform,
            Name = entity.Name,
            CommandPattern = entity.CommandPattern,
            Parameters = JsonSerializer.Deserialize<List<TemplateParameter>>(entity.ParametersJson, JsonOptions) ?? new List<TemplateParameter>()
        };
    }
}
