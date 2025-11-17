using System.Globalization;
using System.IO;
using System.Text;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing audit logs
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task AddLogAsync(AuditLog log)
    {
        await _repository.AddAsync(log);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100)
    {
        return await _repository.GetRecentAsync(count);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _repository.GetByDateRangeAsync(from, to);
    }

    public async Task ExportToCsvAsync(string filePath, DateTime? from = null, DateTime? to = null)
    {
        var logs = await _repository.GetByDateRangeAsync(
            from ?? DateTime.UtcNow.AddYears(-1),
            to ?? DateTime.UtcNow);

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("Timestamp,ActionTitle,Category,Command,Platform,ExitCode,Success,Duration,WasDangerous");

        // Data
        foreach (var log in logs)
        {
            csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"\"{EscapeCsv(log.ActionTitle)}\"," +
                          $"\"{EscapeCsv(log.Category)}\"," +
                          $"\"{EscapeCsv(log.Command)}\"," +
                          $"{log.Platform}," +
                          $"{log.ExitCode}," +
                          $"{log.Success}," +
                          $"{log.Duration.TotalSeconds:F2}," +
                          $"{log.WasDangerous}");
        }

        // SECURITY: Validate file path before writing
        if (string.IsNullOrWhiteSpace(filePath) || filePath.Contains(".."))
        {
            throw new ArgumentException("Invalid file path", nameof(filePath));
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());
    }

    public async Task<int> GetCountAsync()
    {
        return await _repository.GetCountAsync();
    }

    public async Task CleanupOldLogsAsync(int retentionDays = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        await _repository.DeleteOlderThanAsync(cutoffDate);
    }

    private string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }
}
