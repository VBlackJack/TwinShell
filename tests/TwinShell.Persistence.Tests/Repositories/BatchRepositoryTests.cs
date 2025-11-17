using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Enums;
using TwinShell.Core.Models;
using TwinShell.Persistence;
using TwinShell.Persistence.Repositories;

namespace TwinShell.Persistence.Tests.Repositories;

public class BatchRepositoryTests : IDisposable
{
    private readonly TwinShellDbContext _context;
    private readonly BatchRepository _repository;

    public BatchRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TwinShellDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TwinShellDbContext(options);
        _repository = new BatchRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsBatchSuccessfully()
    {
        // Arrange
        var batch = CreateTestBatch("batch-1", "Test Batch");

        // Act
        await _repository.AddAsync(batch);

        // Assert
        var result = await _repository.GetByIdAsync("batch-1");
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Batch");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBatches()
    {
        // Arrange
        await _repository.AddAsync(CreateTestBatch("batch-1", "Batch 1"));
        await _repository.AddAsync(CreateTestBatch("batch-2", "Batch 2"));
        await _repository.AddAsync(CreateTestBatch("batch-3", "Batch 3"));

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByUpdatedAtDescending()
    {
        // Arrange
        var batch1 = CreateTestBatch("batch-1", "Old Batch");
        batch1.UpdatedAt = DateTime.UtcNow.AddDays(-2);

        var batch2 = CreateTestBatch("batch-2", "Recent Batch");
        batch2.UpdatedAt = DateTime.UtcNow;

        await _repository.AddAsync(batch1);
        await _repository.AddAsync(batch2);

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.First().Name.Should().Be("Recent Batch");
        result.Last().Name.Should().Be("Old Batch");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectBatch()
    {
        // Arrange
        await _repository.AddAsync(CreateTestBatch("batch-1", "Batch 1"));
        await _repository.AddAsync(CreateTestBatch("batch-2", "Batch 2"));

        // Act
        var result = await _repository.GetByIdAsync("batch-2");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Batch 2");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistentBatch()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesBatchSuccessfully()
    {
        // Arrange
        var batch = CreateTestBatch("batch-1", "Original Name");
        await _repository.AddAsync(batch);

        batch.Name = "Updated Name";
        batch.Description = "Updated Description";

        // Act
        await _repository.UpdateAsync(batch);

        // Assert
        var result = await _repository.GetByIdAsync("batch-1");
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task DeleteAsync_RemovesBatch()
    {
        // Arrange
        await _repository.AddAsync(CreateTestBatch("batch-1", "Batch 1"));
        await _repository.AddAsync(CreateTestBatch("batch-2", "Batch 2"));

        // Act
        await _repository.DeleteAsync("batch-1");

        // Assert
        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("batch-2");
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrowForNonExistentBatch()
    {
        // Act & Assert
        await _repository.DeleteAsync("non-existent");
        // Should not throw
    }

    [Fact]
    public async Task SearchAsync_FindsBatchesByName()
    {
        // Arrange
        await _repository.AddAsync(CreateTestBatch("batch-1", "System Cleanup"));
        await _repository.AddAsync(CreateTestBatch("batch-2", "Database Backup"));
        await _repository.AddAsync(CreateTestBatch("batch-3", "System Update"));

        // Act
        var result = (await _repository.SearchAsync("System")).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Name == "System Cleanup");
        result.Should().Contain(b => b.Name == "System Update");
    }

    [Fact]
    public async Task SearchAsync_FindsBatchesByDescription()
    {
        // Arrange
        var batch1 = CreateTestBatch("batch-1", "Batch 1");
        batch1.Description = "Contains security updates";

        var batch2 = CreateTestBatch("batch-2", "Batch 2");
        batch2.Description = "Regular maintenance";

        await _repository.AddAsync(batch1);
        await _repository.AddAsync(batch2);

        // Act
        var result = (await _repository.SearchAsync("security")).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("batch-1");
    }

    [Fact]
    public async Task SearchAsync_ReturnEmptyForNoMatches()
    {
        // Arrange
        await _repository.AddAsync(CreateTestBatch("batch-1", "Test Batch"));

        // Act
        var result = (await _repository.SearchAsync("nonexistent")).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    private CommandBatch CreateTestBatch(string id, string name)
    {
        return new CommandBatch
        {
            Id = id,
            Name = name,
            Description = "Test description",
            ExecutionMode = ExecutionMode.Sequential,
            Actions = new List<BatchAction>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
