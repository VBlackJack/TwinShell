/*
 * Copyright 2025 Julien Bombled
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Infrastructure.Services;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for GitSyncService
/// </summary>
public class GitSyncServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ISyncService> _syncServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly ILogger<GitSyncService> _logger;
    private readonly GitSyncService _service;

    public GitSyncServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _syncServiceMock = new Mock<ISyncService>();
        _localizationMock = new Mock<ILocalizationService>();
        _logger = NullLogger<GitSyncService>.Instance;

        // Setup localization mock to return the key as the message (for testing)
        _localizationMock.Setup(l => l.GetString(It.IsAny<string>()))
            .Returns((string key) => key);
        _localizationMock.Setup(l => l.GetFormattedString(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => string.Format(key, args));

        _service = new GitSyncService(
            _settingsServiceMock.Object,
            _syncServiceMock.Object,
            _logger,
            _localizationMock.Object);
    }

    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithNoSettings_ReturnsFalse()
    {
        // Arrange - intentionally test null settings scenario
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        // Act
        var result = _service.IsConfigured;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyRemoteUrl_ReturnsFalse()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "",
            GitRepositoryPath = "C:\\some\\path"
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = _service.IsConfigured;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyRepositoryPath_ReturnsFalse()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "https://github.com/user/repo.git",
            GitRepositoryPath = ""
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = _service.IsConfigured;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithValidSettings_ReturnsTrue()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "https://github.com/user/repo.git",
            GitRepositoryPath = "C:\\repos\\twinshell"
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = _service.IsConfigured;

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region InitializeRepositoryAsync Tests

    [Fact]
    public async Task InitializeRepositoryAsync_WhenNotConfigured_ReturnsFailure()
    {
        // Arrange - intentionally test null settings scenario
#pragma warning disable CS8625
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        // Act
        var result = await _service.InitializeRepositoryAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.InvalidConfiguration);
        // Message contains the localization key (mock returns key as-is)
        result.Message.Should().Contain("GitSync.NotConfigured");
    }

    [Fact]
    public async Task InitializeRepositoryAsync_WithEmptyRemoteUrl_ReturnsInvalidConfiguration()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "",
            GitRepositoryPath = "C:\\repos\\test"
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = await _service.InitializeRepositoryAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.InvalidConfiguration);
    }

    #endregion

    #region PullAndImportAsync Tests

    [Fact]
    public async Task PullAndImportAsync_WhenNotConfigured_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CS8625
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        // Act
        var result = await _service.PullAndImportAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.InvalidConfiguration);
    }

    #endregion

    #region ExportAndPushAsync Tests

    [Fact]
    public async Task ExportAndPushAsync_WhenNotConfigured_ReturnsFailure()
    {
        // Arrange
#pragma warning disable CS8625
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        // Act
        var result = await _service.ExportAndPushAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.InvalidConfiguration);
    }

    [Fact]
    public async Task ExportAndPushAsync_WhenRepositoryNotInitialized_ReturnsFailure()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var settings = new UserSettings
        {
            GitRemoteUrl = "https://github.com/user/repo.git",
            GitRepositoryPath = tempPath // Non-existent path
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = await _service.ExportAndPushAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.RepositoryNotInitialized);
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_WhenRemoteUrlNotConfigured_ReturnsFailure()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "",
            GitRepositoryPath = "C:\\repos\\test"
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = await _service.TestConnectionAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.InvalidConfiguration);
    }

    #endregion

    #region GetRepositoryStatusAsync Tests

    [Fact]
    public async Task GetRepositoryStatusAsync_WhenNotConfigured_ReturnsEmptyStatus()
    {
        // Arrange
#pragma warning disable CS8625
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        // Act
        var result = await _service.GetRepositoryStatusAsync();

        // Assert
        result.IsInitialized.Should().BeFalse();
        result.CurrentBranch.Should().BeNull();
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_WhenRepositoryPathNotSet_ReturnsEmptyStatus()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "https://github.com/user/repo.git",
            GitRepositoryPath = ""
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = await _service.GetRepositoryStatusAsync();

        // Assert
        result.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_WhenRepositoryPathInvalid_ReturnsEmptyStatus()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "https://github.com/user/repo.git",
            GitRepositoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = await _service.GetRepositoryStatusAsync();

        // Assert
        result.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_IncludesRemoteUrl()
    {
        // Arrange
        var remoteUrl = "https://github.com/user/repo.git";
        var settings = new UserSettings
        {
            GitRemoteUrl = remoteUrl,
            GitRepositoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var result = await _service.GetRepositoryStatusAsync();

        // Assert
        result.RemoteUrl.Should().Be(remoteUrl);
    }

    #endregion

    #region FullSyncAsync Tests

    [Fact]
    public async Task FullSyncAsync_WhenPullFails_ReturnsFailureImmediately()
    {
        // Arrange
#pragma warning disable CS8625
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        // Act
        var result = await _service.FullSyncAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.InvalidConfiguration);
    }

    #endregion

    #region StatusChanged Event Tests

    [Fact]
    public async Task InitializeRepositoryAsync_RaisesStatusChangedEvent()
    {
        // Arrange
#pragma warning disable CS8625
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns((UserSettings?)null);
#pragma warning restore CS8625

        var eventCount = 0;
        _service.StatusChanged += (_, _) => eventCount++;

        // Act
        await _service.InitializeRepositoryAsync();

        // Assert - Event should not be raised for config error (no operation started)
        // This tests the basic event mechanism
        eventCount.Should().Be(0);
    }

    [Fact]
    public async Task TestConnectionAsync_RaisesStatusChangedEvent_WhenConfigured()
    {
        // Arrange
        var settings = new UserSettings
        {
            GitRemoteUrl = "https://invalid-url-for-testing.example.com/repo.git",
            GitRepositoryPath = "C:\\repos\\test"
        };
        _settingsServiceMock.Setup(s => s.CurrentSettings).Returns(settings);

        var statusMessages = new List<string>();
        _service.StatusChanged += (sender, args) =>
        {
            statusMessages.Add(args.Status);
        };

        // Act
        await _service.TestConnectionAsync();

        // Assert - Message contains the localization key (mock returns key as-is)
        statusMessages.Should().Contain(s => s.Contains("GitSync.TestingConnection"));
    }

    #endregion

    #region GitOperationResult Factory Tests

    [Fact]
    public void GitOperationResult_Ok_CreatesSuccessResult()
    {
        // Act
        var result = GitOperationResult.Ok("Test message");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Test message");
        result.ErrorCode.Should().Be(GitSyncErrorCode.None);
    }

    [Fact]
    public void GitOperationResult_Fail_CreatesFailureResult()
    {
        // Act
        var result = GitOperationResult.Fail("Error message", GitSyncErrorCode.NetworkError, "Details");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Error message");
        result.ErrorCode.Should().Be(GitSyncErrorCode.NetworkError);
        result.ErrorDetails.Should().Be("Details");
    }

    [Fact]
    public void GitOperationResult_WithConflicts_CreatesConflictResult()
    {
        // Arrange
        var conflicts = new List<SyncConflict>
        {
            new SyncConflict
            {
                EntityType = "Action",
                EntityId = Guid.NewGuid(),
                EntityName = "Test Action",
                LocalModifiedAt = DateTime.UtcNow.AddHours(-1),
                RemoteModifiedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = GitOperationResult.WithConflicts("Conflicts detected", conflicts);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(GitSyncErrorCode.DataConflict);
        result.ConflictsDetected.Should().Be(1);
        result.Conflicts.Should().HaveCount(1);
    }

    [Fact]
    public void GitOperationResult_WithMergeConflicts_CreatesMergeConflictResult()
    {
        // Arrange
        var conflictedFiles = new List<string>
        {
            "actions/network/ping.yaml",
            "templates/ssh.yaml",
            "categories/network.yaml"
        };

        // Act
        var result = GitOperationResult.WithMergeConflicts("Merge conflicts detected", conflictedFiles);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Merge conflicts detected");
        result.ErrorCode.Should().Be(GitSyncErrorCode.MergeConflict);
        result.ConflictsDetected.Should().Be(3);
        result.ConflictedFiles.Should().HaveCount(3);
        result.ConflictedFiles.Should().Contain("actions/network/ping.yaml");
    }

    #endregion

    #region SyncConflict Tests

    [Fact]
    public void SyncConflict_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var entityId = Guid.NewGuid();
        var localTime = DateTime.UtcNow.AddHours(-1);
        var remoteTime = DateTime.UtcNow;

        var conflict = new SyncConflict
        {
            EntityType = "Action",
            EntityId = entityId,
            EntityName = "Test Action",
            LocalModifiedAt = localTime,
            RemoteModifiedAt = remoteTime,
            Resolution = ConflictResolution.KeepLocal
        };

        // Assert
        conflict.EntityType.Should().Be("Action");
        conflict.EntityId.Should().Be(entityId);
        conflict.EntityName.Should().Be("Test Action");
        conflict.LocalModifiedAt.Should().Be(localTime);
        conflict.RemoteModifiedAt.Should().Be(remoteTime);
        conflict.Resolution.Should().Be(ConflictResolution.KeepLocal);
    }

    #endregion

    #region GitRepositoryStatus Tests

    [Fact]
    public void GitRepositoryStatus_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var status = new GitRepositoryStatus();

        // Assert
        status.IsInitialized.Should().BeFalse();
        status.CurrentBranch.Should().BeNull();
        status.CommitsAhead.Should().Be(0);
        status.CommitsBehind.Should().Be(0);
        status.HasLocalChanges.Should().BeFalse();
        status.LastSyncTime.Should().BeNull();
        status.LastCommitMessage.Should().BeNull();
        status.RemoteUrl.Should().BeNull();
    }

    #endregion

    #region GitSyncStatusEventArgs Tests

    [Fact]
    public void GitSyncStatusEventArgs_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var args = new GitSyncStatusEventArgs();

        // Assert
        args.Status.Should().BeEmpty();
        args.IsOperationInProgress.Should().BeFalse();
        args.Progress.Should().BeNull();
        args.Phase.Should().Be(SyncPhase.Idle);
        args.CurrentFile.Should().BeNull();
        args.TotalFiles.Should().Be(0);
        args.ProcessedFiles.Should().Be(0);
        args.CurrentEntityType.Should().BeNull();
    }

    [Fact]
    public void GitSyncStatusEventArgs_CanSetAllProperties()
    {
        // Arrange & Act
        var args = new GitSyncStatusEventArgs
        {
            Status = "Importing actions...",
            IsOperationInProgress = true,
            Progress = 50.0,
            Phase = SyncPhase.Importing,
            CurrentFile = "actions/network/ping.yaml",
            TotalFiles = 10,
            ProcessedFiles = 5,
            CurrentEntityType = "Action"
        };

        // Assert
        args.Status.Should().Be("Importing actions...");
        args.IsOperationInProgress.Should().BeTrue();
        args.Progress.Should().Be(50.0);
        args.Phase.Should().Be(SyncPhase.Importing);
        args.CurrentFile.Should().Be("actions/network/ping.yaml");
        args.TotalFiles.Should().Be(10);
        args.ProcessedFiles.Should().Be(5);
        args.CurrentEntityType.Should().Be("Action");
    }

    #endregion

    #region SyncPhase Enum Tests

    [Theory]
    [InlineData(SyncPhase.Idle)]
    [InlineData(SyncPhase.Initializing)]
    [InlineData(SyncPhase.Fetching)]
    [InlineData(SyncPhase.Merging)]
    [InlineData(SyncPhase.Importing)]
    [InlineData(SyncPhase.Exporting)]
    [InlineData(SyncPhase.Staging)]
    [InlineData(SyncPhase.Committing)]
    [InlineData(SyncPhase.Pushing)]
    [InlineData(SyncPhase.Validating)]
    [InlineData(SyncPhase.DetectingConflicts)]
    [InlineData(SyncPhase.Completed)]
    [InlineData(SyncPhase.Failed)]
    public void SyncPhase_AllValues_AreDefined(SyncPhase phase)
    {
        // This test ensures all enum values are defined and accessible
        Enum.IsDefined(typeof(SyncPhase), phase).Should().BeTrue();
    }

    #endregion

    #region GitSyncErrorCode Enum Tests

    [Theory]
    [InlineData(GitSyncErrorCode.None, 0)]
    [InlineData(GitSyncErrorCode.NetworkError, 1)]
    [InlineData(GitSyncErrorCode.AuthenticationFailed, 2)]
    [InlineData(GitSyncErrorCode.RepositoryNotFound, 3)]
    [InlineData(GitSyncErrorCode.InvalidConfiguration, 4)]
    [InlineData(GitSyncErrorCode.PushRejected, 5)]
    [InlineData(GitSyncErrorCode.MergeConflict, 6)]
    [InlineData(GitSyncErrorCode.RepositoryNotInitialized, 7)]
    [InlineData(GitSyncErrorCode.FileSystemError, 8)]
    [InlineData(GitSyncErrorCode.ValidationError, 9)]
    [InlineData(GitSyncErrorCode.Cancelled, 10)]
    [InlineData(GitSyncErrorCode.Timeout, 11)]
    [InlineData(GitSyncErrorCode.DataConflict, 12)]
    [InlineData(GitSyncErrorCode.Unknown, 99)]
    public void GitSyncErrorCode_HasCorrectValue(GitSyncErrorCode errorCode, int expectedValue)
    {
        // Assert
        ((int)errorCode).Should().Be(expectedValue);
    }

    #endregion

    #region ConflictResolution Enum Tests

    [Fact]
    public void ConflictResolution_HasAllExpectedValues()
    {
        // Assert
        Enum.GetValues<ConflictResolution>().Should().HaveCount(3);
        Enum.IsDefined(typeof(ConflictResolution), ConflictResolution.KeepLocal).Should().BeTrue();
        Enum.IsDefined(typeof(ConflictResolution), ConflictResolution.UseRemote).Should().BeTrue();
        Enum.IsDefined(typeof(ConflictResolution), ConflictResolution.Skip).Should().BeTrue();
    }

    #endregion
}
