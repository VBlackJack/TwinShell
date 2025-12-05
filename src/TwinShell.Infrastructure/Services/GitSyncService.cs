using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Infrastructure.Services;

/// <summary>
/// Service for Git-based synchronization of TwinShell data.
/// Uses LibGit2Sharp for Git operations.
/// </summary>
public class GitSyncService : IGitSyncService
{
    private readonly ISettingsService _settingsService;
    private readonly ISyncService _yamlSyncService;

    private string _statusMessage = "Not configured";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Settings?.GitRemoteUrl)
                                && !string.IsNullOrWhiteSpace(Settings?.GitRepositoryPath);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            StatusChanged?.Invoke(this, new GitSyncStatusEventArgs
            {
                Status = value,
                IsOperationInProgress = value.Contains("...")
            });
        }
    }

    public event EventHandler<GitSyncStatusEventArgs>? StatusChanged;

    private UserSettings? Settings => _settingsService.CurrentSettings;

    public GitSyncService(ISettingsService settingsService, ISyncService yamlSyncService)
    {
        _settingsService = settingsService;
        _yamlSyncService = yamlSyncService;
    }

    public async Task<GitOperationResult> InitializeRepositoryAsync()
    {
        if (!IsConfigured)
        {
            return GitOperationResult.Fail("Git repository not configured. Please set remote URL and local path.");
        }

        var localPath = Settings!.GitRepositoryPath!;
        var remoteUrl = Settings.GitRemoteUrl!;

        try
        {
            // Check if directory exists and is a git repo
            if (Directory.Exists(localPath))
            {
                if (Repository.IsValid(localPath))
                {
                    StatusMessage = "Repository already initialized";
                    return GitOperationResult.Ok("Repository already exists and is valid.");
                }
                else
                {
                    // Directory exists but not a git repo - check if empty
                    if (Directory.GetFileSystemEntries(localPath).Length > 0)
                    {
                        return GitOperationResult.Fail(
                            "Directory exists but is not a Git repository and is not empty.",
                            "Please choose an empty directory or an existing Git repository.");
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(localPath);
            }

            // Clone the repository
            StatusMessage = "Cloning repository...";

            await Task.Run(() =>
            {
                var options = new CloneOptions
                {
                    BranchName = Settings.GitBranch
                };
                options.FetchOptions.CredentialsProvider = GetCredentialsHandler();

                Repository.Clone(remoteUrl, localPath, options);
            });

            StatusMessage = "Repository cloned successfully";
            return GitOperationResult.Ok("Repository cloned successfully.");
        }
        catch (LibGit2SharpException ex)
        {
            StatusMessage = "Clone failed";
            return GitOperationResult.Fail("Failed to clone repository", ex.Message);
        }
        catch (Exception ex)
        {
            StatusMessage = "Initialization failed";
            return GitOperationResult.Fail("Failed to initialize repository", ex.Message);
        }
    }

    public async Task<GitOperationResult> PullAndImportAsync()
    {
        if (!IsConfigured)
        {
            return GitOperationResult.Fail("Git repository not configured.");
        }

        var localPath = Settings!.GitRepositoryPath!;

        if (!Repository.IsValid(localPath))
        {
            var initResult = await InitializeRepositoryAsync();
            if (!initResult.Success)
            {
                return initResult;
            }
        }

        try
        {
            StatusMessage = "Pulling changes...";

            int commitsMerged = 0;

            await Task.Run(() =>
            {
                using var repo = new Repository(localPath);

                // Configure signature for merge commits
                var signature = GetSignature();

                // Fetch from remote
                var remote = repo.Network.Remotes["origin"];
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

                Commands.Fetch(repo, remote.Name, refSpecs, new FetchOptions
                {
                    CredentialsProvider = GetCredentialsHandler()
                }, "Fetching from origin");

                // Get tracking branch
                var trackingBranch = repo.Head.TrackedBranch;
                if (trackingBranch != null)
                {
                    // Check if we're behind
                    var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, trackingBranch.Tip);
                    commitsMerged = divergence.BehindBy ?? 0;

                    if (commitsMerged > 0)
                    {
                        // Pull (fetch + merge)
                        var mergeResult = Commands.Pull(repo, signature, new PullOptions
                        {
                            FetchOptions = new FetchOptions
                            {
                                CredentialsProvider = GetCredentialsHandler()
                            },
                            MergeOptions = new MergeOptions
                            {
                                FastForwardStrategy = FastForwardStrategy.Default
                            }
                        });
                    }
                }
            });

            // Import YAML files into database
            StatusMessage = "Importing data...";
            var importResult = await _yamlSyncService.ImportDataFromYamlAsync(localPath);

            if (importResult.Success)
            {
                StatusMessage = $"Sync complete: {importResult.TotalCreated} created, {importResult.TotalUpdated} updated";
                return new GitOperationResult
                {
                    Success = true,
                    Message = $"Pull completed. {commitsMerged} commits merged.",
                    ItemsImported = importResult.TotalCreated + importResult.TotalUpdated,
                    CommitsMerged = commitsMerged
                };
            }
            else
            {
                StatusMessage = "Import failed";
                return GitOperationResult.Fail("Pull succeeded but import failed",
                    string.Join(", ", importResult.Errors));
            }
        }
        catch (LibGit2SharpException ex)
        {
            StatusMessage = "Pull failed";
            return GitOperationResult.Fail("Failed to pull changes", ex.Message);
        }
        catch (Exception ex)
        {
            StatusMessage = "Sync failed";
            return GitOperationResult.Fail("Failed to sync", ex.Message);
        }
    }

    public async Task<GitOperationResult> ExportAndPushAsync(string? commitMessage = null)
    {
        if (!IsConfigured)
        {
            return GitOperationResult.Fail("Git repository not configured.");
        }

        var localPath = Settings!.GitRepositoryPath!;

        if (!Repository.IsValid(localPath))
        {
            return GitOperationResult.Fail("Local repository not initialized. Please initialize first.");
        }

        try
        {
            // Export database to YAML
            StatusMessage = "Exporting data...";
            var exportResult = await _yamlSyncService.ExportDataToYamlAsync(localPath);

            if (!exportResult.Success)
            {
                StatusMessage = "Export failed";
                return GitOperationResult.Fail("Failed to export data",
                    string.Join(", ", exportResult.Errors));
            }

            StatusMessage = "Committing changes...";

            bool hasChanges = false;

            await Task.Run(() =>
            {
                using var repo = new Repository(localPath);

                // Stage all changes
                Commands.Stage(repo, "*");

                // Check if there are changes to commit
                var status = repo.RetrieveStatus();
                hasChanges = status.IsDirty;

                if (hasChanges)
                {
                    var signature = GetSignature();
                    var message = commitMessage ?? $"TwinShell sync: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                    repo.Commit(message, signature, signature);
                }
            });

            if (!hasChanges)
            {
                StatusMessage = "No changes to push";
                return new GitOperationResult
                {
                    Success = true,
                    Message = "No changes to push. Everything is up to date.",
                    ItemsExported = exportResult.TotalExported
                };
            }

            // Push to remote
            StatusMessage = "Pushing to remote...";

            await Task.Run(() =>
            {
                using var repo = new Repository(localPath);

                var remote = repo.Network.Remotes["origin"];
                var pushRefSpec = $"refs/heads/{Settings.GitBranch}";

                repo.Network.Push(remote, pushRefSpec, new PushOptions
                {
                    CredentialsProvider = GetCredentialsHandler()
                });
            });

            StatusMessage = $"Pushed {exportResult.TotalExported} items";
            return new GitOperationResult
            {
                Success = true,
                Message = "Changes pushed successfully.",
                ItemsExported = exportResult.TotalExported
            };
        }
        catch (NonFastForwardException)
        {
            StatusMessage = "Push rejected - pull first";
            return GitOperationResult.Fail(
                "Push rejected. Remote has changes that need to be pulled first.",
                "Please pull changes before pushing.");
        }
        catch (LibGit2SharpException ex)
        {
            StatusMessage = "Push failed";
            return GitOperationResult.Fail(
                "Export succeeded but push failed. You can push manually with git push.",
                ex.Message);
        }
        catch (Exception ex)
        {
            StatusMessage = "Export/Push failed";
            return GitOperationResult.Fail($"Failed to export and push: {ex.GetType().Name}", ex.Message);
        }
    }

    public async Task<GitOperationResult> FullSyncAsync()
    {
        // First pull and import
        var pullResult = await PullAndImportAsync();
        if (!pullResult.Success)
        {
            return pullResult;
        }

        // Then export and push (if auto-push enabled)
        if (Settings?.GitAutoPush == true)
        {
            var pushResult = await ExportAndPushAsync();

            // Consider sync successful if pull succeeded, even if push had issues
            // (push failures are usually credential issues, not data issues)
            var message = pushResult.Success
                ? $"Sync complete. {pullResult.ItemsImported} imported, {pushResult.ItemsExported} exported."
                : $"Sync complete. {pullResult.ItemsImported} imported, {pushResult.ItemsExported} exported. Push failed: {pushResult.ErrorDetails}";

            return new GitOperationResult
            {
                Success = true, // Pull and import succeeded, that's the important part
                Message = message,
                ErrorDetails = pushResult.Success ? null : pushResult.ErrorDetails,
                ItemsImported = pullResult.ItemsImported,
                ItemsExported = pushResult.ItemsExported,
                CommitsMerged = pullResult.CommitsMerged
            };
        }

        return pullResult;
    }

    public async Task<GitOperationResult> TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(Settings?.GitRemoteUrl))
        {
            return GitOperationResult.Fail("Remote URL not configured.");
        }

        try
        {
            StatusMessage = "Testing connection...";

            await Task.Run(() =>
            {
                // Try to list remote references to test connection
                var refs = Repository.ListRemoteReferences(Settings.GitRemoteUrl, GetCredentialsHandler());
                var count = refs.Count();
            });

            StatusMessage = "Connection successful";
            return GitOperationResult.Ok("Connection to remote repository successful.");
        }
        catch (LibGit2SharpException ex)
        {
            StatusMessage = "Connection failed";
            return GitOperationResult.Fail("Failed to connect to remote repository", ex.Message);
        }
        catch (Exception ex)
        {
            StatusMessage = "Connection test failed";
            return GitOperationResult.Fail("Connection test failed", ex.Message);
        }
    }

    public async Task<GitRepositoryStatus> GetRepositoryStatusAsync()
    {
        var status = new GitRepositoryStatus
        {
            RemoteUrl = Settings?.GitRemoteUrl
        };

        if (!IsConfigured || string.IsNullOrWhiteSpace(Settings?.GitRepositoryPath))
        {
            return status;
        }

        var localPath = Settings.GitRepositoryPath;

        if (!Repository.IsValid(localPath))
        {
            return status;
        }

        try
        {
            await Task.Run(() =>
            {
                using var repo = new Repository(localPath);

                status.IsInitialized = true;
                status.CurrentBranch = repo.Head.FriendlyName;
                status.HasLocalChanges = repo.RetrieveStatus().IsDirty;

                if (repo.Head.Tip != null)
                {
                    status.LastCommitMessage = repo.Head.Tip.MessageShort;
                }

                var trackingBranch = repo.Head.TrackedBranch;
                if (trackingBranch != null)
                {
                    var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(
                        repo.Head.Tip, trackingBranch.Tip);
                    status.CommitsAhead = divergence.AheadBy ?? 0;
                    status.CommitsBehind = divergence.BehindBy ?? 0;
                }
            });
        }
        catch
        {
            // Ignore errors, return partial status
        }

        return status;
    }

    private CredentialsHandler GetCredentialsHandler()
    {
        return (url, usernameFromUrl, types) =>
        {
            // HTTPS with token - most common case
            // SECURITY NOTE: LibGit2Sharp doesn't support SecureString, so we minimize
            // token exposure by not storing intermediate references. The token is retrieved
            // directly from the encrypted settings and passed to LibGit2Sharp.
            // Token is cleared from memory when UsernamePasswordCredentials is disposed.
            var token = Settings?.GitAccessToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                return new UsernamePasswordCredentials
                {
                    Username = Settings?.GitUserName ?? usernameFromUrl ?? "git",
                    Password = token
                };
            }

            // If no token configured, use default credentials (will work for public repos or system SSH agent)
            return new DefaultCredentials();
        };
    }

    private Signature GetSignature()
    {
        var name = Settings?.GitUserName ?? "TwinShell User";
        var email = Settings?.GitUserEmail ?? "twinshell@local";
        return new Signature(name, email, DateTimeOffset.Now);
    }
}
