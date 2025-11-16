using FluentAssertions;
using TwinShell.Core.Enums;
using TwinShell.Infrastructure.Services;
using Xunit;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for CommandExecutionService
/// </summary>
public class CommandExecutionServiceTests
{
    private readonly CommandExecutionService _sut;

    public CommandExecutionServiceTests()
    {
        _sut = new CommandExecutionService();
    }

    [Fact]
    public async Task ExecuteAsync_SimpleEchoCommand_ReturnsSuccessWithOutput()
    {
        // Arrange
        var command = "echo test";
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.ExecuteAsync(command, platform, cts.Token, timeoutSeconds: 30);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("test");
        result.Stderr.Should().BeEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.WasCancelled.Should().BeFalse();
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCommand_ReturnsFailure()
    {
        // Arrange
        var command = "this-command-does-not-exist-12345";
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.ExecuteAsync(command, platform, cts.Token, timeoutSeconds: 30);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledExecution_ReturnsWasCancelledTrue()
    {
        // Arrange
        // Use a long-running command that we can cancel
        var command = OperatingSystem.IsWindows()
            ? "Start-Sleep -Seconds 30"  // PowerShell
            : "sleep 30";                 // Bash
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = _sut.ExecuteAsync(command, platform, cts.Token, timeoutSeconds: 60);

        // Cancel after 500ms
        await Task.Delay(500);
        cts.Cancel();

        var result = await executeTask;

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.WasCancelled.Should().BeTrue();
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(-1);
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutExecution_ReturnsTimedOutTrue()
    {
        // Arrange
        // Use a long-running command that will timeout
        var command = OperatingSystem.IsWindows()
            ? "Start-Sleep -Seconds 30"  // PowerShell
            : "sleep 30";                 // Bash
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.ExecuteAsync(command, platform, cts.Token, timeoutSeconds: 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
        result.WasCancelled.Should().BeFalse();
        result.ExitCode.Should().Be(-1);
        result.ErrorMessage.Should().Contain("timed out");
    }

    [Fact]
    public async Task ExecuteAsync_OutputCallbackReceivesOutput()
    {
        // Arrange
        var command = "echo line1 && echo line2 && echo line3";
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        using var cts = new CancellationTokenSource();
        var outputLines = new List<string>();

        // Act
        var result = await _sut.ExecuteAsync(
            command,
            platform,
            cts.Token,
            timeoutSeconds: 30,
            onOutputReceived: line => outputLines.Add(line.Text));

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        outputLines.Should().NotBeEmpty();
        outputLines.Should().Contain(line => line.Contains("line"));
    }

    [Fact]
    public async Task ExecuteAsync_PlatformBoth_DetectsCurrentPlatform()
    {
        // Arrange
        var command = "echo test";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.ExecuteAsync(command, Platform.Both, cts.Token, timeoutSeconds: 30);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Stdout.Should().Contain("test");
    }

    [Fact]
    public async Task ExecuteAsync_ErrorOutput_CapturesStderr()
    {
        // Arrange
        // Command that writes to stderr
        var command = OperatingSystem.IsWindows()
            ? "Write-Error 'error message'"  // PowerShell
            : "echo 'error message' >&2";    // Bash
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.ExecuteAsync(command, platform, cts.Token, timeoutSeconds: 30);

        // Assert
        result.Should().NotBeNull();
        // PowerShell Write-Error doesn't necessarily cause non-zero exit code
        // but stderr should be captured
        result.Stderr.Should().NotBeEmpty();
    }
}
