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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Infrastructure.Services;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Tests for BatchExecutionService
/// </summary>
public class BatchExecutionServiceTests
{
    private readonly Mock<ICommandExecutionService> _mockCommandExecutionService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly BatchExecutionService _service;

    public BatchExecutionServiceTests()
    {
        _mockCommandExecutionService = new Mock<ICommandExecutionService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _service = new BatchExecutionService(
            _mockCommandExecutionService.Object,
            _mockAuditLogService.Object,
            NullLogger<BatchExecutionService>.Instance);
    }

    [Fact]
    public void ValidateBatch_WithNullBatch_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateBatch(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBatch_WithEmptyCommands_ReturnsFalse()
    {
        // Arrange
        var batch = new CommandBatch
        {
            Id = "batch-1",
            Name = "Test Batch",
            Commands = new List<BatchCommandItem>()
        };

        // Act
        var result = _service.ValidateBatch(batch);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBatch_WithEmptyCommandString_ReturnsFalse()
    {
        // Arrange
        var batch = new CommandBatch
        {
            Id = "batch-1",
            Name = "Test Batch",
            Commands = new List<BatchCommandItem>
            {
                new BatchCommandItem { Command = "", Platform = Platform.Windows }
            }
        };

        // Act
        var result = _service.ValidateBatch(batch);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBatch_WithValidBatch_ReturnsTrue()
    {
        // Arrange
        var batch = new CommandBatch
        {
            Id = "batch-1",
            Name = "Test Batch",
            Commands = new List<BatchCommandItem>
            {
                new BatchCommandItem { Command = "echo test", Platform = Platform.Windows }
            }
        };

        // Act
        var result = _service.ValidateBatch(batch);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithInvalidBatch_ReturnsFailure()
    {
        // Arrange
        var batch = new CommandBatch
        {
            Id = "batch-1",
            Name = "Empty Batch",
            Commands = new List<BatchCommandItem>()
        };

        // Act
        var result = await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid batch");
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithSuccessfulCommands_ReturnsSuccess()
    {
        // Arrange
        var batch = CreateValidBatch(2);
        var successResult = CreateSuccessResult();

        _mockCommandExecutionService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ExecutedCount.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithFailedCommand_ContinuesExecution_InContinueOnErrorMode()
    {
        // Arrange
        var batch = CreateValidBatch(3);
        batch.ExecutionMode = BatchExecutionMode.ContinueOnError;

        var successResult = CreateSuccessResult();
        var failureResult = CreateFailureResult();

        // First command fails, others succeed
        _mockCommandExecutionService
            .SetupSequence(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(failureResult)
            .ReturnsAsync(successResult)
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ExecutedCount.Should().Be(3);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithFailedCommand_StopsExecution_InStopOnErrorMode()
    {
        // Arrange
        var batch = CreateValidBatch(3);
        batch.ExecutionMode = BatchExecutionMode.StopOnError;

        var successResult = CreateSuccessResult();
        var failureResult = CreateFailureResult();

        // Second command fails
        _mockCommandExecutionService
            .SetupSequence(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(successResult)
            .ReturnsAsync(failureResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ExecutedCount.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithCancellation_SetsCancelledFlag()
    {
        // Arrange
        var batch = CreateValidBatch(3);
        var cts = new CancellationTokenSource();

        var successResult = CreateSuccessResult();

        // Cancel after first command
        _mockCommandExecutionService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .Callback(() => cts.Cancel())
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteBatchAsync(batch, cts.Token);

        // Assert
        result.WasCancelled.Should().BeTrue();
        result.SkippedCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteBatchAsync_CallsAuditLogService_ForEachCommand()
    {
        // Arrange
        var batch = CreateValidBatch(2);
        var successResult = CreateSuccessResult();

        _mockCommandExecutionService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        _mockAuditLogService.Verify(
            s => s.AddLogAsync(It.IsAny<AuditLog>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteBatchAsync_ReportsProgress()
    {
        // Arrange
        var batch = CreateValidBatch(2);
        var successResult = CreateSuccessResult();
        var progressReports = new List<BatchExecutionProgress>();

        _mockCommandExecutionService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteBatchAsync(
            batch,
            CancellationToken.None,
            onProgressChanged: p => progressReports.Add(p));

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.Should().Contain(p => p.TotalCommands == 2);
    }

    [Fact]
    public async Task ExecuteBatchAsync_UpdatesLastExecutedAt()
    {
        // Arrange
        var batch = CreateValidBatch(1);
        var beforeExecution = DateTime.UtcNow;
        var successResult = CreateSuccessResult();

        _mockCommandExecutionService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        batch.LastExecutedAt.Should().NotBeNull();
        batch.LastExecutedAt!.Value.Should().BeOnOrAfter(beforeExecution);
    }

    [Fact]
    public async Task ExecuteBatchAsync_SetsDurationAndTimestamps()
    {
        // Arrange
        var batch = CreateValidBatch(1);
        var successResult = CreateSuccessResult();

        _mockCommandExecutionService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<Action<OutputLine>>()))
            .ReturnsAsync(successResult);

        _mockAuditLogService
            .Setup(s => s.AddLogAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteBatchAsync(batch, CancellationToken.None);

        // Assert
        result.TotalDuration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        result.CompletedAt.Should().NotBeNull();
        result.StartedAt.Should().BeOnOrBefore(result.CompletedAt!.Value);
    }

    #region Helper Methods

    private static CommandBatch CreateValidBatch(int commandCount)
    {
        var commands = new List<BatchCommandItem>();
        for (int i = 0; i < commandCount; i++)
        {
            commands.Add(new BatchCommandItem
            {
                Command = $"echo test{i}",
                Platform = Platform.Windows,
                ActionId = $"action-{i}",
                ActionTitle = $"Test Command {i}"
            });
        }

        return new CommandBatch
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Batch",
            Commands = commands,
            ExecutionMode = BatchExecutionMode.ContinueOnError
        };
    }

    private static ExecutionResult CreateSuccessResult()
    {
        return new ExecutionResult
        {
            Success = true,
            ExitCode = 0,
            Stdout = "Success output",
            Stderr = "",
            StartedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100)
        };
    }

    private static ExecutionResult CreateFailureResult()
    {
        return new ExecutionResult
        {
            Success = false,
            ExitCode = 1,
            Stdout = "",
            Stderr = "Error output",
            StartedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(50)
        };
    }

    #endregion
}
