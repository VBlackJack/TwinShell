using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Persistence;
using TwinShell.Persistence.Repositories;

namespace TwinShell.Persistence.Tests.Repositories;

public class ActionRepositoryTests : IDisposable
{
    private readonly TwinShellDbContext _context;
    private readonly ActionRepository _repository;

    public ActionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TwinShellDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TwinShellDbContext(options);
        _repository = new ActionRepository(_context, NullLogger<ActionRepository>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsActionToDatabase()
    {
        // Arrange
        var action = new Action
        {
            Id = "test-1",
            Title = "Test Action",
            Description = "Test Description",
            Category = "Test",
            Platform = Platform.Windows,
            Level = CriticalityLevel.Info,
            Tags = new List<string> { "test" }
        };

        // Act
        await _repository.AddAsync(action);

        // Assert
        var result = await _repository.GetByIdAsync("test-1");
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Action");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActions()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectAction()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetByIdAsync("action-1");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Action 1");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsActionsInCategory()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetByCategoryAsync("Category A");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.Category.Should().Be("Category A"));
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Category A");
        result.Should().Contain("Category B");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingAction()
    {
        // Arrange
        await SeedTestDataAsync();
        var action = await _repository.GetByIdAsync("action-1");
        action!.Title = "Updated Title";

        // Act
        await _repository.UpdateAsync(action);

        // Assert
        var updated = await _repository.GetByIdAsync("action-1");
        updated!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAction()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        await _repository.DeleteAsync("action-1");

        // Assert
        var result = await _repository.GetByIdAsync("action-1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.ExistsAsync("action-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(3);
    }

    private async Task SeedTestDataAsync()
    {
        var actions = new List<Action>
        {
            new Action
            {
                Id = "action-1",
                Title = "Action 1",
                Description = "Description 1",
                Category = "Category A",
                Platform = Platform.Windows,
                Level = CriticalityLevel.Info,
                Tags = new List<string> { "tag1" }
            },
            new Action
            {
                Id = "action-2",
                Title = "Action 2",
                Description = "Description 2",
                Category = "Category A",
                Platform = Platform.Linux,
                Level = CriticalityLevel.Run,
                Tags = new List<string> { "tag2" }
            },
            new Action
            {
                Id = "action-3",
                Title = "Action 3",
                Description = "Description 3",
                Category = "Category B",
                Platform = Platform.Both,
                Level = CriticalityLevel.Dangerous,
                Tags = new List<string> { "tag3" }
            }
        };

        foreach (var action in actions)
        {
            await _repository.AddAsync(action);
        }
    }
}
