using FluentAssertions;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

public class AuditLogServiceTests
{
    private readonly AuditLogService _service;
    private readonly FakeAuditLogRepository _repository;

    public AuditLogServiceTests()
    {
        _repository = new FakeAuditLogRepository();
        _service = new AuditLogService(_repository);
    }

    [Fact]
    public async Task AddLogAsync_AddsLogSuccessfully()
    {
        // Arrange
        var log = new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            ActionTitle = "Test Action",
            Category = "Test",
            Command = "test-command",
            Platform = Platform.Windows,
            Timestamp = DateTime.UtcNow,
            ExitCode = 0,
            Success = true,
            Duration = TimeSpan.FromSeconds(1),
            WasDangerous = false
        };

        // Act
        await _service.AddLogAsync(log);

        // Assert
        _repository.Logs.Should().HaveCount(1);
        _repository.Logs.First().Should().BeEquivalentTo(log);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 150; i++)
        {
            await _service.AddLogAsync(CreateTestLog($"action-{i}"));
        }

        // Act
        var logs = (await _service.GetRecentAsync(100)).ToList();

        // Assert
        logs.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetByDateRangeAsync_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _service.AddLogAsync(CreateTestLog("old", now.AddDays(-10)));
        await _service.AddLogAsync(CreateTestLog("recent1", now.AddDays(-2)));
        await _service.AddLogAsync(CreateTestLog("recent2", now.AddDays(-1)));

        // Act
        var logs = (await _service.GetByDateRangeAsync(now.AddDays(-3), now)).ToList();

        // Assert
        logs.Should().HaveCount(2);
        logs.Should().AllSatisfy(l => l.Timestamp.Should().BeAfter(now.AddDays(-3)));
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _service.AddLogAsync(CreateTestLog($"action-{i}"));
        }

        // Act
        var count = await _service.GetCountAsync();

        // Assert
        count.Should().Be(25);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_DeletesOldLogs()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _service.AddLogAsync(CreateTestLog("old1", now.AddDays(-400)));
        await _service.AddLogAsync(CreateTestLog("old2", now.AddDays(-380)));
        await _service.AddLogAsync(CreateTestLog("recent", now.AddDays(-10)));

        // Act
        await _service.CleanupOldLogsAsync(365);

        // Assert
        var remainingLogs = await _service.GetRecentAsync(100);
        remainingLogs.Should().HaveCount(1);
        remainingLogs.First().ActionTitle.Should().Be("recent");
    }

    [Fact]
    public async Task ExportToCsvAsync_ThrowsOnInvalidPath()
    {
        // Arrange
        var invalidPath = "../../../etc/passwd";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ExportToCsvAsync(invalidPath));
    }

    private AuditLog CreateTestLog(string title, DateTime? timestamp = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            ActionTitle = title,
            Category = "Test",
            Command = "test-command",
            Platform = Platform.Windows,
            Timestamp = timestamp ?? DateTime.UtcNow,
            ExitCode = 0,
            Success = true,
            Duration = TimeSpan.FromSeconds(1),
            WasDangerous = false
        };
    }
}

/// <summary>
/// Fake repository for testing AuditLogService
/// </summary>
public class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Logs { get; } = new();

    public Task AddAsync(AuditLog log)
    {
        Logs.Add(log);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100)
    {
        return Task.FromResult<IEnumerable<AuditLog>>(
            Logs.OrderByDescending(l => l.Timestamp).Take(count).ToList());
    }

    public Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return Task.FromResult<IEnumerable<AuditLog>>(
            Logs.Where(l => l.Timestamp >= from && l.Timestamp <= to).ToList());
    }

    public Task<int> GetCountAsync()
    {
        return Task.FromResult(Logs.Count);
    }

    public Task DeleteOlderThanAsync(DateTime cutoffDate)
    {
        Logs.RemoveAll(l => l.Timestamp < cutoffDate);
        return Task.CompletedTask;
    }
}
