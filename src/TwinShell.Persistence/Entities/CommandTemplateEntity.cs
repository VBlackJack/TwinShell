using TwinShell.Core.Enums;

namespace TwinShell.Persistence.Entities;

/// <summary>
/// Database entity for CommandTemplate
/// </summary>
public class CommandTemplateEntity
{
    public string Id { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CommandPattern { get; set; } = string.Empty;

    /// <summary>
    /// Parameters stored as JSON
    /// </summary>
    public string ParametersJson { get; set; } = "[]";
}
