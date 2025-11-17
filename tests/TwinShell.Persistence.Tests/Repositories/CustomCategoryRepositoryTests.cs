using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TwinShell.Core.Models;
using TwinShell.Persistence;
using TwinShell.Persistence.Repositories;

namespace TwinShell.Persistence.Tests.Repositories;

public class CustomCategoryRepositoryTests : IDisposable
{
    private readonly TwinShellDbContext _context;
    private readonly CustomCategoryRepository _repository;

    public CustomCategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TwinShellDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TwinShellDbContext(options);
        _repository = new CustomCategoryRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_CreatesCategory()
    {
        // Arrange
        var category = CreateTestCategory("cat-1", "Test Category");

        // Act
        var result = await _repository.CreateAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("cat-1");
        result.Name.Should().Be("Test Category");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));
        await _repository.CreateAsync(CreateTestCategory("cat-2", "Category 2"));
        await _repository.CreateAsync(CreateTestCategory("cat-3", "Category 3"));

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDisplayOrder()
    {
        // Arrange
        var cat1 = CreateTestCategory("cat-1", "First");
        cat1.DisplayOrder = 3;

        var cat2 = CreateTestCategory("cat-2", "Second");
        cat2.DisplayOrder = 1;

        var cat3 = CreateTestCategory("cat-3", "Third");
        cat3.DisplayOrder = 2;

        await _repository.CreateAsync(cat1);
        await _repository.CreateAsync(cat2);
        await _repository.CreateAsync(cat3);

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result[0].Name.Should().Be("Second");
        result[1].Name.Should().Be("Third");
        result[2].Name.Should().Be("First");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectCategory()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));
        await _repository.CreateAsync(CreateTestCategory("cat-2", "Category 2"));

        // Act
        var result = await _repository.GetByIdAsync("cat-2");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Category 2");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistent()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCategory()
    {
        // Arrange
        var category = CreateTestCategory("cat-1", "Original Name");
        await _repository.CreateAsync(category);

        category.Name = "Updated Name";
        category.IconKey = "updated-icon";

        // Act
        await _repository.UpdateAsync(category);

        // Assert
        var result = await _repository.GetByIdAsync("cat-1");
        result!.Name.Should().Be("Updated Name");
        result.IconKey.Should().Be("updated-icon");
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));
        await _repository.CreateAsync(CreateTestCategory("cat-2", "Category 2"));

        // Act
        await _repository.DeleteAsync("cat-1");

        // Assert
        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("cat-2");
    }

    [Fact]
    public async Task GetVisibleCategoriesAsync_ReturnsOnlyVisibleCategories()
    {
        // Arrange
        var cat1 = CreateTestCategory("cat-1", "Visible 1");
        cat1.IsHidden = false;

        var cat2 = CreateTestCategory("cat-2", "Hidden");
        cat2.IsHidden = true;

        var cat3 = CreateTestCategory("cat-3", "Visible 2");
        cat3.IsHidden = false;

        await _repository.CreateAsync(cat1);
        await _repository.CreateAsync(cat2);
        await _repository.CreateAsync(cat3);

        // Act
        var result = (await _repository.GetVisibleCategoriesAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Visible 1");
        result.Should().Contain(c => c.Name == "Visible 2");
        result.Should().NotContain(c => c.Name == "Hidden");
    }

    [Fact]
    public async Task AddActionToCategoryAsync_AddsMapping()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));

        // Act
        await _repository.AddActionToCategoryAsync("action-1", "cat-1");

        // Assert
        var actionIds = (await _repository.GetActionIdsForCategoryAsync("cat-1")).ToList();
        actionIds.Should().Contain("action-1");
    }

    [Fact]
    public async Task AddActionToCategoryAsync_DoesNotDuplicateMapping()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));
        await _repository.AddActionToCategoryAsync("action-1", "cat-1");

        // Act
        await _repository.AddActionToCategoryAsync("action-1", "cat-1");

        // Assert
        var actionIds = (await _repository.GetActionIdsForCategoryAsync("cat-1")).ToList();
        actionIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveActionFromCategoryAsync_RemovesMapping()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));
        await _repository.AddActionToCategoryAsync("action-1", "cat-1");
        await _repository.AddActionToCategoryAsync("action-2", "cat-1");

        // Act
        await _repository.RemoveActionFromCategoryAsync("action-1", "cat-1");

        // Assert
        var actionIds = (await _repository.GetActionIdsForCategoryAsync("cat-1")).ToList();
        actionIds.Should().HaveCount(1);
        actionIds.Should().Contain("action-2");
    }

    [Fact]
    public async Task GetActionIdsForCategoryAsync_ReturnsCorrectIds()
    {
        // Arrange
        await _repository.CreateAsync(CreateTestCategory("cat-1", "Category 1"));
        await _repository.AddActionToCategoryAsync("action-1", "cat-1");
        await _repository.AddActionToCategoryAsync("action-2", "cat-1");
        await _repository.AddActionToCategoryAsync("action-3", "cat-1");

        // Act
        var result = (await _repository.GetActionIdsForCategoryAsync("cat-1")).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("action-1");
        result.Should().Contain("action-2");
        result.Should().Contain("action-3");
    }

    [Fact]
    public async Task IsCategorySystemAsync_ReturnsTrueForSystemCategory()
    {
        // Arrange
        var category = CreateTestCategory("cat-1", "System Category");
        category.IsSystemCategory = true;
        await _repository.CreateAsync(category);

        // Act
        var result = await _repository.IsCategorySystemAsync("cat-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCategorySystemAsync_ReturnsFalseForUserCategory()
    {
        // Arrange
        var category = CreateTestCategory("cat-1", "User Category");
        category.IsSystemCategory = false;
        await _repository.CreateAsync(category);

        // Act
        var result = await _repository.IsCategorySystemAsync("cat-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetNextDisplayOrderAsync_ReturnsNextOrder()
    {
        // Arrange
        var cat1 = CreateTestCategory("cat-1", "Category 1");
        cat1.DisplayOrder = 1;

        var cat2 = CreateTestCategory("cat-2", "Category 2");
        cat2.DisplayOrder = 5;

        await _repository.CreateAsync(cat1);
        await _repository.CreateAsync(cat2);

        // Act
        var result = await _repository.GetNextDisplayOrderAsync();

        // Assert
        result.Should().Be(6);
    }

    [Fact]
    public async Task GetNextDisplayOrderAsync_ReturnsOneWhenEmpty()
    {
        // Act
        var result = await _repository.GetNextDisplayOrderAsync();

        // Assert
        result.Should().Be(1);
    }

    private CustomCategory CreateTestCategory(string id, string name)
    {
        return new CustomCategory
        {
            Id = id,
            Name = name,
            Description = "Test description",
            IconKey = "test-icon",
            ColorHex = "#FF0000",
            IsSystemCategory = false,
            IsHidden = false,
            DisplayOrder = 0,
            ActionIds = new List<string>()
        };
    }
}
