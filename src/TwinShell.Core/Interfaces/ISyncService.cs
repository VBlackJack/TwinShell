namespace TwinShell.Core.Interfaces;

/// <summary>
/// Service for GitOps synchronization of TwinShell data via YAML files.
/// Enables collaborative editing through Git-synchronized folders.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Export all data (actions, batches, templates, categories) to YAML files.
    /// Creates an organized folder structure suitable for Git versioning.
    /// </summary>
    /// <param name="rootFolderPath">Root folder path where YAML files will be created</param>
    /// <returns>Export result with statistics and any errors encountered</returns>
    Task<SyncExportResult> ExportDataToYamlAsync(string rootFolderPath);

    /// <summary>
    /// Import data from YAML files into the local database.
    /// Uses PublicId for matching existing records (upsert logic).
    /// </summary>
    /// <param name="rootFolderPath">Root folder path containing YAML files to import</param>
    /// <returns>Import result with statistics and any errors encountered</returns>
    Task<SyncImportResult> ImportDataFromYamlAsync(string rootFolderPath);

    /// <summary>
    /// Validate YAML files in a folder without importing.
    /// Useful for pre-flight checks before synchronization.
    /// </summary>
    /// <param name="rootFolderPath">Root folder path to validate</param>
    /// <returns>Validation result with details about found files and any issues</returns>
    Task<SyncValidationResult> ValidateFolderAsync(string rootFolderPath);
}

/// <summary>
/// Result of a YAML export operation
/// </summary>
public class SyncExportResult
{
    public bool Success { get; set; }
    public int ActionsExported { get; set; }
    public int BatchesExported { get; set; }
    public int TemplatesExported { get; set; }
    public int CategoriesExported { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int TotalExported => ActionsExported + BatchesExported + TemplatesExported + CategoriesExported;
}

/// <summary>
/// Result of a YAML import operation
/// </summary>
public class SyncImportResult
{
    public bool Success { get; set; }

    public int ActionsCreated { get; set; }
    public int ActionsUpdated { get; set; }
    public int BatchesCreated { get; set; }
    public int BatchesUpdated { get; set; }
    public int TemplatesCreated { get; set; }
    public int TemplatesUpdated { get; set; }
    public int CategoriesCreated { get; set; }
    public int CategoriesUpdated { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int TotalCreated => ActionsCreated + BatchesCreated + TemplatesCreated + CategoriesCreated;
    public int TotalUpdated => ActionsUpdated + BatchesUpdated + TemplatesUpdated + CategoriesUpdated;
}

/// <summary>
/// Result of YAML folder validation
/// </summary>
public class SyncValidationResult
{
    public bool IsValid { get; set; }
    public int ActionFilesFound { get; set; }
    public int BatchFilesFound { get; set; }
    public int TemplateFilesFound { get; set; }
    public int CategoryFilesFound { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int TotalFilesFound => ActionFilesFound + BatchFilesFound + TemplateFilesFound + CategoryFilesFound;
}
