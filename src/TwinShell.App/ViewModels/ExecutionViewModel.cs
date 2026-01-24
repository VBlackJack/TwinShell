using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Constants;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for command execution panel
/// </summary>
public partial class ExecutionViewModel : ObservableObject, IDisposable
{
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly ICommandHistoryService _commandHistoryService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    // BUGFIX: Added lock object for thread-safe access to _executionCts and _executionTimer
    private readonly object _lock = new object();
    private CancellationTokenSource? _executionCts;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<OutputLineViewModel> _outputLines = new();

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private double _executionProgress;

    [ObservableProperty]
    private string _executionTime = "00:00";

    private System.Timers.Timer? _executionTimer;
    private DateTime _executionStartTime;
    // BUGFIX: Replace fragile OutputLines.Count == 4 check with explicit flag
    private bool _outputReceivedViaCallbacks;

    public ExecutionViewModel(
        ICommandExecutionService commandExecutionService,
        ICommandHistoryService commandHistoryService,
        ISettingsService settingsService,
        ILocalizationService localizationService)
    {
        _commandExecutionService = commandExecutionService;
        _commandHistoryService = commandHistoryService;
        _settingsService = settingsService;
        _localizationService = localizationService;
        _statusMessage = _localizationService.GetString(MessageKeys.ExecutionReady);
    }

    /// <summary>
    /// Execute a command
    /// </summary>
    [RelayCommand]
    private async Task ExecuteCommandAsync(ExecuteCommandParameter parameter)
    {
        if (!ValidateCommand(parameter.Command))
        {
            return;
        }

        if (!await ConfirmDangerousCommandIfNeededAsync(parameter))
        {
            return;
        }

        InitializeExecution();

        try
        {
            // Get timeout from settings
            var settings = await _settingsService.LoadSettingsAsync();
            var timeout = Math.Clamp(settings.ExecutionTimeoutSeconds,
                TimeoutConstants.MinCommandTimeoutSeconds,
                TimeoutConstants.MaxCommandTimeoutSeconds);

            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Executing command: {parameter.Command}", false);
            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Platform: {parameter.Platform}", false);
            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Timeout: {timeout} seconds", false);
            AddOutputLine("", false);

            // Execute the command
            CancellationToken token;
            lock (_lock)
            {
                token = _executionCts!.Token;
            }

            var result = await _commandExecutionService.ExecuteAsync(
                parameter.Command,
                parameter.Platform,
                token,
                timeout,
                onOutputReceived: (outputLine) =>
                {
                    _outputReceivedViaCallbacks = true;
                    // UI-003: Add output line to UI on UI thread with null check
                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher != null)
                    {
                        dispatcher.InvokeAsync(() =>
                        {
                            AddOutputLine(outputLine.Text, outputLine.IsError);
                        });
                    }
                });

            StopExecutionTimer();
            DisplayExecutionOutput(result);
            UpdateExecutionStatus(result);

            StatusMessage = result.Success
                ? _localizationService.GetString(MessageKeys.ExecutionSuccessStatus)
                : _localizationService.GetString(MessageKeys.ExecutionFailedStatus);
            ExecutionProgress = 100;

            // Update command history with execution results
            if (parameter.ActionId != null)
            {
                await UpdateCommandHistoryWithExecution(
                    parameter.ActionId,
                    parameter.Command,
                    parameter.Parameters,
                    parameter.Platform,
                    parameter.ActionTitle,
                    parameter.Category,
                    result);
            }
        }
        catch (Exception)
        {
            lock (_lock)
            {
                if (_executionTimer != null)
                {
                    _executionTimer.Stop();
                }
            }
            AddOutputLine("", false);
            // SECURITY: Don't expose exception details to users
            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ✗ ERROR: Command execution failed", true);
            StatusMessage = _localizationService.GetString(MessageKeys.ExecutionErrorOccurred);
        }
        finally
        {
            IsExecuting = false;
            lock (_lock)
            {
                _executionCts?.Dispose();
                _executionCts = null;

                // Properly dispose timer and detach event handler
                if (_executionTimer != null)
                {
                    _executionTimer.Stop();
                    _executionTimer.Elapsed -= OnTimerElapsed;
                    _executionTimer.Dispose();
                    _executionTimer = null;
                }
            }
        }
    }

    /// <summary>
    /// Timer elapsed event handler
    /// </summary>
    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var elapsed = DateTime.Now - _executionStartTime;
        // UI-003: Null check for Application.Current before Dispatcher access
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null)
        {
            dispatcher.InvokeAsync(() =>
            {
                ExecutionTime = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            });
        }
    }

    /// <summary>
    /// Stop the currently executing command
    /// </summary>
    [RelayCommand]
    private void StopExecution()
    {
        lock (_lock)
        {
            if (_executionCts != null && !_executionCts.IsCancellationRequested)
            {
                AddOutputLine($"[{DateTime.Now:HH:mm:ss}] {_localizationService.GetString(MessageKeys.ExecutionCancellingMessage)}", true);
                _executionCts.Cancel();
            }
        }
    }

    /// <summary>
    /// Clear the output panel
    /// </summary>
    [RelayCommand]
    private void ClearOutput()
    {
        OutputLines.Clear();
        StatusMessage = _localizationService.GetString(MessageKeys.ExecutionReady);
        ExecutionTime = "00:00";
        ExecutionProgress = 0;
    }

    /// <summary>
    /// Add an output line to the collection
    /// </summary>
    private void AddOutputLine(string text, bool isError)
    {
        OutputLines.Add(new OutputLineViewModel
        {
            Text = text,
            IsError = isError,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// Validates that a command is not empty
    /// </summary>
    private bool ValidateCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            MessageBox.Show(
                _localizationService.GetString(MessageKeys.ExecutionNoCommand),
                _localizationService.GetString(MessageKeys.ExecutionNoCommandTitle),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Confirms dangerous command execution if needed
    /// </summary>
    private async Task<bool> ConfirmDangerousCommandIfNeededAsync(ExecuteCommandParameter parameter)
    {
        if (!parameter.IsDangerous || !parameter.RequireConfirmation)
        {
            return true;
        }

        var confirmResult = MessageBox.Show(
            _localizationService.GetFormattedString(MessageKeys.ExecutionDangerousWarning, parameter.Command),
            _localizationService.GetString(MessageKeys.ExecutionDangerousTitle),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmResult != MessageBoxResult.Yes)
        {
            AddOutputLine(_localizationService.GetString(MessageKeys.ExecutionCancelledByUser), true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Initializes execution state and starts timer
    /// </summary>
    private void InitializeExecution()
    {
        OutputLines.Clear();
        IsExecuting = true;
        StatusMessage = _localizationService.GetString(MessageKeys.ExecutionExecuting);
        ExecutionProgress = 0;
        _outputReceivedViaCallbacks = false;

        // Create cancellation token source
        lock (_lock)
        {
            _executionCts = new CancellationTokenSource();
        }

        // Start execution timer
        _executionStartTime = DateTime.Now;
        lock (_lock)
        {
            // PERFORMANCE: Reduced from 100ms to 250ms (60% CPU reduction, no UX impact)
            _executionTimer = new System.Timers.Timer(250); // Update every 250ms
            _executionTimer.Elapsed += OnTimerElapsed;
            _executionTimer.Start();
        }
    }

    /// <summary>
    /// Stops the execution timer
    /// </summary>
    private void StopExecutionTimer()
    {
        lock (_lock)
        {
            if (_executionTimer != null)
            {
                _executionTimer.Stop();
            }
        }
    }

    /// <summary>
    /// Displays execution output and summary
    /// </summary>
    private void DisplayExecutionOutput(ExecutionResult result)
    {
        // BUGFIX: Use explicit flag instead of fragile OutputLines.Count == 4 check
        // Add final output if not already added via callbacks
        if (!string.IsNullOrEmpty(result.Stdout) && !_outputReceivedViaCallbacks)
        {
            foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                AddOutputLine(line, false);
            }
        }

        if (!string.IsNullOrEmpty(result.Stderr) && !result.Stderr.Contains("OperationCanceledException"))
        {
            foreach (var line in result.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                AddOutputLine(line, true);
            }
        }
    }

    /// <summary>
    /// Updates execution status summary in output
    /// </summary>
    private void UpdateExecutionStatus(ExecutionResult result)
    {
        // Add execution summary
        AddOutputLine("", false);
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ─────────────────────────────────────", false);
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Exit Code: {result.ExitCode}", !result.Success);
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Duration: {result.Duration.TotalSeconds:F2}s", false);
        AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Status: {(result.Success ? "✓ SUCCESS" : "✗ FAILED")}", !result.Success);

        if (result.WasCancelled)
        {
            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ⚠ Execution was cancelled", true);
        }
        else if (result.TimedOut)
        {
            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] ⚠ Execution timed out", true);
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            AddOutputLine($"[{DateTime.Now:HH:mm:ss}] Error: {result.ErrorMessage}", true);
        }
    }

    /// <summary>
    /// Update command history with execution results
    /// </summary>
    private async Task UpdateCommandHistoryWithExecution(
        string actionId,
        string command,
        Dictionary<string, string> parameters,
        Platform platform,
        string actionTitle,
        string category,
        ExecutionResult result)
    {
        // Add the command to history first
        var historyId = await _commandHistoryService.AddCommandAsync(
            actionId,
            command,
            parameters,
            platform,
            actionTitle,
            category);

        // Update with execution results
        await _commandHistoryService.UpdateWithExecutionResultsAsync(
            historyId,
            result.ExitCode,
            result.Duration,
            result.Success);
    }

    /// <summary>
    /// Dispose resources to prevent memory leaks
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            // Stop and dispose timer
            if (_executionTimer != null)
            {
                _executionTimer.Stop();
                _executionTimer.Elapsed -= OnTimerElapsed;
                _executionTimer.Dispose();
                _executionTimer = null;
            }

            // Cancel and dispose cancellation token source
            if (_executionCts != null)
            {
                _executionCts.Cancel();
                _executionCts.Dispose();
                _executionCts = null;
            }
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// ViewModel for a single output line
/// </summary>
public partial class OutputLineViewModel : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private DateTime _timestamp;
}

/// <summary>
/// Parameter for ExecuteCommand
/// </summary>
public class ExecuteCommandParameter
{
    public string Command { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public bool IsDangerous { get; set; }
    public bool RequireConfirmation { get; set; } = true;
    public string? ActionId { get; set; }
    public string ActionTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
