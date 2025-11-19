using FluentAssertions;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

public class CommandHistoryServiceTests
{
    private readonly CommandHistoryService _service;
    private readonly FakeCommandHistoryRepository _repository;

    public CommandHistoryServiceTests()
    {
        _repository = new FakeCommandHistoryRepository();
        _service = new CommandHistoryService(_repository);
    }

    [Fact]
    public async Task AddCommandAsync_CreatesHistoryEntryWithCorrectTimestamp()
    {
        // Arrange
        var actionId = "test-action-1";
        var command = "Get-ADUser -Filter *";
        var parameters = new Dictionary<string, string> { { "Filter", "*" } };
        var platform = Platform.Windows;
        var actionTitle = "List AD Users";
        var category = "Active Directory";

        var beforeAdd = DateTime.UtcNow;

        // Act
        await _service.AddCommandAsync(actionId, command, parameters, platform, actionTitle, category);

        // Assert
        _repository.History.Should().HaveCount(1);
        var history = _repository.History.First();
        history.ActionId.Should().Be(actionId);
        history.GeneratedCommand.Should().Be(command);
        history.Parameters.Should().BeEquivalentTo(parameters);
        history.Platform.Should().Be(platform);
        history.ActionTitle.Should().Be(actionTitle);
        history.Category.Should().Be(category);
        history.CreatedAt.Should().BeOnOrAfter(beforeAdd);
        history.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsCorrectNumberOfEntries()
    {
        // Arrange
        await AddTestHistoryEntries(10);

        // Act
        var result = await _service.GetRecentAsync(5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task SearchAsync_FiltersCorrectlyBySearchText()
    {
        // Arrange
        await _service.AddCommandAsync("1", "Get-ADUser -Filter *", new(), Platform.Windows, "List Users", "AD");
        await _service.AddCommandAsync("2", "Get-Service", new(), Platform.Windows, "List Services", "Services");
        await _service.AddCommandAsync("3", "Get-Process", new(), Platform.Windows, "List Processes", "System");

        // Act
        var result = await _service.SearchAsync(searchText: "Service");

        // Assert
        result.Should().HaveCount(1);
        result.First().GeneratedCommand.Should().Be("Get-Service");
    }

    [Fact]
    public async Task SearchAsync_FiltersCorrectlyByPlatform()
    {
        // Arrange
        await _service.AddCommandAsync("1", "Get-ADUser", new(), Platform.Windows, "List Users", "AD");
        await _service.AddCommandAsync("2", "ls -la", new(), Platform.Linux, "List Files", "Files");
        await _service.AddCommandAsync("3", "Get-Process", new(), Platform.Windows, "List Processes", "System");

        // Act
        var result = await _service.SearchAsync(platform: Platform.Windows);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(h => h.Platform == Platform.Windows);
    }

    [Fact]
    public async Task SearchAsync_FiltersCorrectlyByCategory()
    {
        // Arrange
        await _service.AddCommandAsync("1", "Get-ADUser", new(), Platform.Windows, "List Users", "AD");
        await _service.AddCommandAsync("2", "Get-ADGroup", new(), Platform.Windows, "List Groups", "AD");
        await _service.AddCommandAsync("3", "Get-Service", new(), Platform.Windows, "List Services", "Services");

        // Act
        var result = await _service.SearchAsync(category: "AD");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(h => h.Category == "AD");
    }

    [Fact]
    public async Task SearchAsync_FiltersCorrectlyByDateRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await AddTestHistoryEntries(5);

        // Act
        var result = await _service.SearchAsync(fromDate: now.AddMinutes(-1));

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task DeleteAsync_RemovesHistoryEntry()
    {
        // Arrange
        await AddTestHistoryEntries(3);
        var idToDelete = _repository.History.First().Id;

        // Act
        await _service.DeleteAsync(idToDelete);

        // Assert
        _repository.History.Should().HaveCount(2);
        _repository.History.Should().NotContain(h => h.Id == idToDelete);
    }

    [Fact]
    public async Task CleanupOldEntriesAsync_RemovesEntriesOlderThanSpecifiedDays()
    {
        // Arrange
        var oldHistory = new CommandHistory
        {
            Id = "old-1",
            ActionId = "action-1",
            GeneratedCommand = "Old Command",
            Parameters = new(),
            Platform = Platform.Windows,
            ActionTitle = "Old Action",
            Category = "Test",
            CreatedAt = DateTime.UtcNow.AddDays(-100)
        };
        await _repository.AddAsync(oldHistory);
        await AddTestHistoryEntries(3);

        // Act
        await _service.CleanupOldEntriesAsync(90);

        // Assert
        _repository.History.Should().HaveCount(3);
        _repository.History.Should().NotContain(h => h.Id == "old-1");
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await AddTestHistoryEntries(7);

        // Act
        var count = await _service.GetCountAsync();

        // Assert
        count.Should().Be(7);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllEntries()
    {
        // Arrange
        await AddTestHistoryEntries(10);

        // Act
        await _service.ClearAllAsync();

        // Assert
        _repository.History.Should().BeEmpty();
    }

    private async Task AddTestHistoryEntries(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _service.AddCommandAsync(
                $"action-{i}",
                $"Test Command {i}",
                new Dictionary<string, string> { { "param", $"value{i}" } },
                Platform.Windows,
                $"Test Action {i}",
                "Test Category"
            );
        }
    }
}

/// <summary>
/// Fake in-memory implementation of ICommandHistoryRepository for testing
/// </summary>
internal class FakeCommandHistoryRepository : ICommandHistoryRepository
{
    public List<CommandHistory> History { get; } = new();

    public Task AddAsync(CommandHistory history)
    {
        History.Add(history);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<CommandHistory>> GetRecentAsync(int count = 50)
    {
        var recent = History
            .OrderByDescending(h => h.CreatedAt)
            .Take(count);
        return Task.FromResult<IEnumerable<CommandHistory>>(recent.ToList());
    }

    public Task<IEnumerable<CommandHistory>> SearchAsync(
        string? searchText = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Platform? platform = null,
        string? category = null)
    {
        var query = History.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(h =>
                h.GeneratedCommand.ToLower().Contains(search) ||
                h.ActionTitle.ToLower().Contains(search));
        }

        if (fromDate.HasValue)
            query = query.Where(h => h.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(h => h.CreatedAt <= toDate.Value);

        if (platform.HasValue)
            query = query.Where(h => h.Platform == platform.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(h => h.Category == category);

        return Task.FromResult<IEnumerable<CommandHistory>>(query.ToList());
    }

    public Task<CommandHistory?> GetByIdAsync(string id)
    {
        var history = History.FirstOrDefault(h => h.Id == id);
        return Task.FromResult(history);
    }

    public Task DeleteAsync(string id)
    {
        var history = History.FirstOrDefault(h => h.Id == id);
        if (history != null)
            History.Remove(history);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<string> ids)
    {
        var toRemove = History.Where(h => ids.Contains(h.Id)).ToList();
        foreach (var item in toRemove)
            History.Remove(item);
        return Task.CompletedTask;
    }

    public Task DeleteOlderThanAsync(DateTime date)
    {
        var toRemove = History.Where(h => h.CreatedAt < date).ToList();
        foreach (var item in toRemove)
            History.Remove(item);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(History.Count);
    }

    public Task ClearAllAsync()
    {
        History.Clear();
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<CommandHistory> histories)
    {
        // Stub implementation for testing
        foreach (var history in histories)
        {
            History.Add(history);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CommandHistory history)
    {
        // Stub implementation for testing
        var existing = History.FirstOrDefault(h => h.Id == history.Id);
        if (existing != null)
        {
            History.Remove(existing);
            History.Add(history);
        }
        return Task.CompletedTask;
    }
}
