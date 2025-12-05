namespace TwinShell.Core.Interfaces;

/// <summary>
/// Service for Git-based synchronization of TwinShell data.
/// Handles clone, pull, push operations with remote repositories.
/// </summary>
public interface IGitSyncService
{
    /// <summary>
    /// Gets whether a Git repository is configured and ready.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets or sets the current synchronization status message.
    /// </summary>
    string StatusMessage { get; }

    /// <summary>
    /// Event raised when sync status changes.
    /// </summary>
    event EventHandler<GitSyncStatusEventArgs>? StatusChanged;

    /// <summary>
    /// Initializes the local repository. Clones if not exists, or validates existing.
    /// </summary>
    /// <returns>Result of the initialization</returns>
    Task<GitOperationResult> InitializeRepositoryAsync();

    /// <summary>
    /// Pulls latest changes from remote and imports into database.
    /// </summary>
    /// <returns>Result of the pull operation</returns>
    Task<GitOperationResult> PullAndImportAsync();

    /// <summary>
    /// Exports database to YAML, commits and pushes to remote.
    /// </summary>
    /// <param name="commitMessage">Optional commit message</param>
    /// <returns>Result of the push operation</returns>
    Task<GitOperationResult> ExportAndPushAsync(string? commitMessage = null);

    /// <summary>
    /// Performs a full sync: pull, merge, export changes, push.
    /// </summary>
    /// <returns>Result of the sync operation</returns>
    Task<GitOperationResult> FullSyncAsync();

    /// <summary>
    /// Tests the connection to the remote repository.
    /// </summary>
    /// <returns>True if connection is successful</returns>
    Task<GitOperationResult> TestConnectionAsync();

    /// <summary>
    /// Gets the current local repository status (branch, ahead/behind, etc.)
    /// </summary>
    /// <returns>Repository status information</returns>
    Task<GitRepositoryStatus> GetRepositoryStatusAsync();
}

/// <summary>
/// Result of a Git operation
/// </summary>
public class GitOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public int ItemsImported { get; set; }
    public int ItemsExported { get; set; }
    public int CommitsMerged { get; set; }

    public static GitOperationResult Ok(string message = "Operation completed successfully")
        => new() { Success = true, Message = message };

    public static GitOperationResult Fail(string message, string? details = null)
        => new() { Success = false, Message = message, ErrorDetails = details };
}

/// <summary>
/// Status of the local Git repository
/// </summary>
public class GitRepositoryStatus
{
    public bool IsInitialized { get; set; }
    public string? CurrentBranch { get; set; }
    public int CommitsAhead { get; set; }
    public int CommitsBehind { get; set; }
    public bool HasLocalChanges { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public string? LastCommitMessage { get; set; }
    public string? RemoteUrl { get; set; }
}

/// <summary>
/// Event args for sync status changes
/// </summary>
public class GitSyncStatusEventArgs : EventArgs
{
    public string Status { get; set; } = string.Empty;
    public bool IsOperationInProgress { get; set; }
    public double? Progress { get; set; }
}
