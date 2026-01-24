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
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Infrastructure.Services;

namespace TwinShell.Infrastructure.Tests.Services;

/// <summary>
/// Tests for CommandExecutionService
/// </summary>
public class CommandExecutionServiceTests
{
    private readonly CommandExecutionService _service;

    public CommandExecutionServiceTests()
    {
        _service = new CommandExecutionService(NullLogger<CommandExecutionService>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WithSimplePowerShellCommand_ReturnsSuccess()
    {
        // Arrange - Use a simple command that works on Windows
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        var command = "Write-Output 'Hello World'";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 10);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("Hello World");
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingCommand_ReturnsFailure()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        var command = "exit 1";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 10);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_SetsCancelledFlag()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        // A command that takes time
        var command = "Start-Sleep -Seconds 30";
        var cts = new CancellationTokenSource();

        // Cancel after 100ms
        cts.CancelAfter(100);

        // Act
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 30);

        // Assert
        result.Should().NotBeNull();
        result.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_SetsTimedOutFlag()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        // A command that takes time
        var command = "Start-Sleep -Seconds 30";
        var cts = new CancellationTokenSource();

        // Act - Set a very short timeout
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 1);

        // Assert
        result.Should().NotBeNull();
        result.TimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_CapturesStandardOutput()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        var command = "Write-Output 'Line1'; Write-Output 'Line2'";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var outputLines = new List<OutputLine>();

        // Act
        var result = await _service.ExecuteAsync(
            command,
            Platform.Windows,
            cts.Token,
            timeoutSeconds: 10,
            onOutputReceived: line => outputLines.Add(line));

        // Assert
        result.Success.Should().BeTrue();
        result.Stdout.Should().Contain("Line1");
        result.Stdout.Should().Contain("Line2");
        outputLines.Should().Contain(l => l.Text.Contains("Line1"));
    }

    [Fact]
    public async Task ExecuteAsync_CapturesStandardError()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        var command = "Write-Error 'Error message' 2>&1";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 10);

        // Assert
        result.Should().NotBeNull();
        // Write-Error goes to stderr but with 2>&1 redirect it may appear in stdout
        (result.Stdout + result.Stderr).Should().Contain("Error");
    }

    [Fact]
    public async Task ExecuteAsync_SetsExecutionDuration()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        var command = "Write-Output 'Quick'";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 10);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.StartedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows
        }

        // Test that special characters are handled via base64 encoding
        var command = "Write-Output 'Test with $special & characters'";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var result = await _service.ExecuteAsync(command, Platform.Windows, cts.Token, timeoutSeconds: 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Stdout.Should().Contain("Test with $special & characters");
    }
}
