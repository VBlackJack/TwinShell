using System.Diagnostics;
using System.Text;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Infrastructure.Services;

/// <summary>
/// Service for executing PowerShell and Bash commands using System.Diagnostics.Process
/// </summary>
public class CommandExecutionService : ICommandExecutionService
{
    /// <summary>
    /// Executes a command on the specified platform
    /// </summary>
    public async Task<ExecutionResult> ExecuteAsync(
        string command,
        Platform platform,
        CancellationToken cancellationToken,
        int timeoutSeconds = 30,
        Action<OutputLine>? onOutputReceived = null)
    {
        var result = new ExecutionResult
        {
            StartedAt = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        try
        {
            // Determine executable and arguments based on platform
            var (executable, arguments) = GetExecutableAndArguments(command, platform);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = processStartInfo };

            // Event handlers for real-time output capture
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stdoutBuilder.AppendLine(e.Data);
                    onOutputReceived?.Invoke(new OutputLine
                    {
                        Text = e.Data,
                        IsError = false,
                        Timestamp = DateTime.UtcNow
                    });
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stderrBuilder.AppendLine(e.Data);
                    onOutputReceived?.Invoke(new OutputLine
                    {
                        Text = e.Data,
                        IsError = true,
                        Timestamp = DateTime.UtcNow
                    });
                }
            };

            // Start the process
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Create timeout cancellation token source
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Wait for process to exit or cancellation
            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Kill the process if cancelled or timed out
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore errors during kill
                }

                result.WasCancelled = cancellationToken.IsCancellationRequested;
                result.TimedOut = timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
                result.Success = false;
                result.ErrorMessage = result.TimedOut
                    ? $"Execution timed out after {timeoutSeconds} seconds"
                    : "Execution was cancelled by user";

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Stdout = stdoutBuilder.ToString();
                result.Stderr = stderrBuilder.ToString();
                result.ExitCode = -1;

                return result;
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.ExitCode = process.ExitCode;
            result.Stdout = stdoutBuilder.ToString();
            result.Stderr = stderrBuilder.ToString();
            result.Success = process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.ErrorMessage = $"Failed to execute command: {ex.Message}";
            result.ExitCode = -1;
            result.Stderr = ex.ToString();
        }

        return result;
    }

    /// <summary>
    /// Gets the executable and arguments based on platform
    /// </summary>
    private (string executable, string arguments) GetExecutableAndArguments(string command, Platform platform)
    {
        // Detect current OS if platform is "Both"
        var actualPlatform = platform;
        if (platform == Platform.Both)
        {
            actualPlatform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        }

        return actualPlatform switch
        {
            Platform.Windows => ("powershell.exe", $"-NoProfile -NonInteractive -Command \"{EscapeForPowerShell(command)}\""),
            Platform.Linux => ("bash", $"-c \"{EscapeForBash(command)}\""),
            _ => throw new NotSupportedException($"Platform {platform} is not supported for command execution")
        };
    }

    /// <summary>
    /// Escapes command for PowerShell execution
    /// </summary>
    private string EscapeForPowerShell(string command)
    {
        // Escape double quotes by doubling them
        return command.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Escapes command for Bash execution
    /// </summary>
    private string EscapeForBash(string command)
    {
        // Escape double quotes with backslash
        return command.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");
    }
}
