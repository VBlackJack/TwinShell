using TwinShell.Core.Enums;

namespace TwinShell.Persistence.Entities;

/// <summary>
/// Database entity for Action
/// </summary>
public class ActionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public CriticalityLevel Level { get; set; }

    /// <summary>
    /// Tags stored as JSON array
    /// </summary>
    public string TagsJson { get; set; } = "[]";

    public string? WindowsCommandTemplateId { get; set; }
    public CommandTemplateEntity? WindowsCommandTemplate { get; set; }

    public string? LinuxCommandTemplateId { get; set; }
    public CommandTemplateEntity? LinuxCommandTemplate { get; set; }

    /// <summary>
    /// Examples stored as JSON
    /// </summary>
    public string ExamplesJson { get; set; } = "[]";

    public string? Notes { get; set; }

    /// <summary>
    /// Links stored as JSON
    /// </summary>
    public string LinksJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsUserCreated { get; set; }
}
