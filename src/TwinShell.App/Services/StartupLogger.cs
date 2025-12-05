using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TwinShell.App.Services;

/// <summary>
/// High-performance startup logger that buffers log entries and writes them asynchronously.
/// Prevents blocking the UI thread during application startup.
/// </summary>
public sealed class StartupLogger : IDisposable
{
    private static readonly Lazy<StartupLogger> _instance = new(() => new StartupLogger());
    public static StartupLogger Instance => _instance.Value;

    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly string _infoLogPath;
    private readonly string _errorLogPath;
    private readonly Timer _flushTimer;
    private readonly object _flushLock = new();
    private bool _disposed;

    private StartupLogger()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _infoLogPath = Path.Combine(baseDir, "startup.log");
        _errorLogPath = Path.Combine(baseDir, "startup-error.log");

        // Flush logs every 500ms or on dispose
        _flushTimer = new Timer(_ => FlushLogs(), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Logs an informational message (non-blocking).
    /// </summary>
    public void LogInfo(string message)
    {
        if (_disposed) return;
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        _logQueue.Enqueue($"INFO|{logMessage}");
    }

    /// <summary>
    /// Logs an error message with optional exception details (non-blocking).
    /// </summary>
    public void LogError(string message, Exception? ex = null)
    {
        if (_disposed) return;

        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}");

        if (ex != null)
        {
            // SECURITY: Sanitize exception logging to avoid exposing sensitive paths or data
            sb.AppendLine($"Type: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Source: {ex.Source}");
        }

        _logQueue.Enqueue($"ERROR|{sb}");
    }

    /// <summary>
    /// Flushes all pending log entries to disk.
    /// Called automatically by timer and on dispose.
    /// </summary>
    public void FlushLogs()
    {
        if (_disposed) return;

        lock (_flushLock)
        {
            var infoEntries = new StringBuilder();
            var errorEntries = new StringBuilder();

            while (_logQueue.TryDequeue(out var entry))
            {
                if (entry.StartsWith("ERROR|"))
                {
                    errorEntries.AppendLine(entry[6..]);
                }
                else if (entry.StartsWith("INFO|"))
                {
                    infoEntries.AppendLine(entry[5..]);
                }
            }

            try
            {
                if (infoEntries.Length > 0)
                {
                    File.AppendAllText(_infoLogPath, infoEntries.ToString());
                }

                if (errorEntries.Length > 0)
                {
                    File.AppendAllText(_errorLogPath, errorEntries.ToString());
                }
            }
            catch
            {
                // Can't log logging errors - fail silently but don't crash the app
            }
        }
    }

    /// <summary>
    /// Synchronously logs an error for critical failures (use sparingly).
    /// Only use when the application is about to crash and async logging won't complete.
    /// </summary>
    public void LogErrorSync(string message, Exception? ex = null)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}");

            if (ex != null)
            {
                sb.AppendLine($"Type: {ex.GetType().Name}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Source: {ex.Source}");
            }

            File.AppendAllText(_errorLogPath, sb.ToString());
        }
        catch
        {
            // Can't log logging errors
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Dispose();
        FlushLogs(); // Final flush
    }
}
