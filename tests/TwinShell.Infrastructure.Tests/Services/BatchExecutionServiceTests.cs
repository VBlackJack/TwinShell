using FluentAssertions;
using Moq;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Infrastructure.Services;
using Xunit;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for BatchExecutionService
/// </summary>
public class BatchExecutionServiceTests
{
    private readonly Mock<ICommandExecutionService> _mockCommandExecutionService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly BatchExecutionService _sut;

    public BatchExecutionServiceTests()
    {
        _mockCommandExecutionService = new Mock<ICommandExecutionService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _sut = new BatchExecutionService(_mockCommandExecutionService.Object, _mockAuditLogService.Object);
    }

    [Fact]
    public async Task ExecuteBatchAsync_EmptyCommands_ReturnsFailure()
    {
        // Arrange
        var commands = new List<BatchCommand>();
        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = true
        };

        // Act
        var result = await _sut.ExecuteBatchAsync(commands, batchConfig, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No commands");
    }

    [Fact]
    public async Task ExecuteBatchAsync_SequentialMode_ExecutesInOrder()
    {
        // Arrange
        var executionOrder = new List<int>();

        var commands = new List<BatchCommand>
        {
            new BatchCommand
            {
                Id = "1",
                Command = "echo test1",
                Platform = Platform.Windows,
                Sequence = 1
            },
            new BatchCommand
            {
                Id = "2",
                Command = "echo test2",
                Platform = Platform.Windows,
                Sequence = 2
            },
            new BatchCommand
            {
                Id = "3",
                Command = "echo test3",
                Platform = Platform.Windows,
                Sequence = 3
            }
        };

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Platform>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), null))
            .ReturnsAsync((string cmd, Platform platform, CancellationToken ct, int timeout, Action<OutputLine>? callback) =>
            {
                var cmdId = cmd.Contains("test1") ? 1 : cmd.Contains("test2") ? 2 : 3;
                executionOrder.Add(cmdId);
                return new CommandResult
                {
                    Success = true,
                    ExitCode = 0,
                    Stdout = $"output {cmdId}",
                    Stderr = string.Empty,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
            });

        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = false
        };

        // Act
        var result = await _sut.ExecuteBatchAsync(commands, batchConfig, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.CommandResults.Should().HaveCount(3);
        executionOrder.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ExecuteBatchAsync_StopOnError_StopsAfterFirstFailure()
    {
        // Arrange
        var commands = new List<BatchCommand>
        {
            new BatchCommand
            {
                Id = "1",
                Command = "echo test1",
                Platform = Platform.Windows,
                Sequence = 1
            },
            new BatchCommand
            {
                Id = "2",
                Command = "failing-command",
                Platform = Platform.Windows,
                Sequence = 2
            },
            new BatchCommand
            {
                Id = "3",
                Command = "echo test3",
                Platform = Platform.Windows,
                Sequence = 3
            }
        };

        var executionCount = 0;
        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Platform>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), null))
            .ReturnsAsync((string cmd, Platform platform, CancellationToken ct, int timeout, Action<OutputLine>? callback) =>
            {
                executionCount++;
                var isFailingCommand = cmd.Contains("failing-command");
                return new CommandResult
                {
                    Success = !isFailingCommand,
                    ExitCode = isFailingCommand ? 1 : 0,
                    Stdout = isFailingCommand ? string.Empty : "output",
                    Stderr = isFailingCommand ? "error" : string.Empty,
                    ErrorMessage = isFailingCommand ? "Command failed" : null,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
            });

        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = true
        };

        // Act
        var result = await _sut.ExecuteBatchAsync(commands, batchConfig, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.CommandResults.Should().HaveCount(2); // Only first two commands executed
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ContinueOnError_ExecutesAllCommands()
    {
        // Arrange
        var commands = new List<BatchCommand>
        {
            new BatchCommand
            {
                Id = "1",
                Command = "echo test1",
                Platform = Platform.Windows,
                Sequence = 1
            },
            new BatchCommand
            {
                Id = "2",
                Command = "failing-command",
                Platform = Platform.Windows,
                Sequence = 2
            },
            new BatchCommand
            {
                Id = "3",
                Command = "echo test3",
                Platform = Platform.Windows,
                Sequence = 3
            }
        };

        var executionCount = 0;
        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Platform>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), null))
            .ReturnsAsync((string cmd, Platform platform, CancellationToken ct, int timeout, Action<OutputLine>? callback) =>
            {
                executionCount++;
                var isFailingCommand = cmd.Contains("failing-command");
                return new CommandResult
                {
                    Success = !isFailingCommand,
                    ExitCode = isFailingCommand ? 1 : 0,
                    Stdout = isFailingCommand ? string.Empty : "output",
                    Stderr = isFailingCommand ? "error" : string.Empty,
                    ErrorMessage = isFailingCommand ? "Command failed" : null,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
            });

        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = false // Continue on error
        };

        // Act
        var result = await _sut.ExecuteBatchAsync(commands, batchConfig, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse(); // Overall failure because one command failed
        result.CommandResults.Should().HaveCount(3); // All three commands executed
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteBatchAsync_Cancellation_StopsExecution()
    {
        // Arrange
        var commands = new List<BatchCommand>
        {
            new BatchCommand
            {
                Id = "1",
                Command = "echo test1",
                Platform = Platform.Windows,
                Sequence = 1
            },
            new BatchCommand
            {
                Id = "2",
                Command = "echo test2",
                Platform = Platform.Windows,
                Sequence = 2
            }
        };

        using var cts = new CancellationTokenSource();
        var executionCount = 0;

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Platform>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), null))
            .ReturnsAsync((string cmd, Platform platform, CancellationToken ct, int timeout, Action<OutputLine>? callback) =>
            {
                executionCount++;
                if (executionCount == 1)
                {
                    // Cancel after first command
                    cts.Cancel();
                }
                return new CommandResult
                {
                    Success = true,
                    ExitCode = 0,
                    Stdout = "output",
                    Stderr = string.Empty,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
            });

        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = false
        };

        // Act
        var result = await _sut.ExecuteBatchAsync(commands, batchConfig, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.WasCancelled.Should().BeTrue();
        executionCount.Should().Be(1); // Only first command executed
    }

    [Fact]
    public async Task ExecuteBatchAsync_ProgressReporting_InvokesCallback()
    {
        // Arrange
        var commands = new List<BatchCommand>
        {
            new BatchCommand
            {
                Id = "1",
                Command = "echo test1",
                Platform = Platform.Windows,
                Sequence = 1
            },
            new BatchCommand
            {
                Id = "2",
                Command = "echo test2",
                Platform = Platform.Windows,
                Sequence = 2
            }
        };

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Platform>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), null))
            .ReturnsAsync(new CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = "output",
                Stderr = string.Empty,
                Duration = TimeSpan.FromMilliseconds(100)
            });

        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = false
        };

        var progressUpdates = new List<BatchProgress>();

        // Act
        var result = await _sut.ExecuteBatchAsync(
            commands,
            batchConfig,
            CancellationToken.None,
            progress => progressUpdates.Add(progress));

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        progressUpdates.Should().NotBeEmpty();
        progressUpdates.Should().Contain(p => p.CurrentCommandIndex == 1);
        progressUpdates.Should().Contain(p => p.CurrentCommandIndex == 2);
    }

    [Fact]
    public async Task ExecuteBatchAsync_CreatesAuditLogEntries()
    {
        // Arrange
        var commands = new List<BatchCommand>
        {
            new BatchCommand
            {
                Id = "1",
                Command = "echo test1",
                Platform = Platform.Windows,
                Sequence = 1
            }
        };

        _mockCommandExecutionService
            .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Platform>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), null))
            .ReturnsAsync(new CommandResult
            {
                Success = true,
                ExitCode = 0,
                Stdout = "output",
                Stderr = string.Empty,
                Duration = TimeSpan.FromMilliseconds(100)
            });

        var batchConfig = new BatchExecutionConfig
        {
            Mode = BatchExecutionMode.Sequential,
            StopOnError = false
        };

        // Act
        var result = await _sut.ExecuteBatchAsync(commands, batchConfig, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockAuditLogService.Verify(
            x => x.LogCommandExecutionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Platform>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.AtLeastOnce);
    }
}
