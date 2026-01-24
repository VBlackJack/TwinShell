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
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

public class CustomCategoryServiceTests
{
    private readonly CustomCategoryService _service;
    private readonly FakeCustomCategoryRepository _repository;

    public CustomCategoryServiceTests()
    {
        _repository = new FakeCustomCategoryRepository();
        _service = new CustomCategoryService(_repository);
    }

    #region GetAllCategoriesAsync Tests

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsAllCategories()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "Cat1"));
        _repository.Categories.Add(CreateCategory("2", "Cat2"));

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetVisibleCategoriesAsync Tests

    [Fact]
    public async Task GetVisibleCategoriesAsync_ReturnsOnlyVisibleCategories()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "Visible1", isHidden: false));
        _repository.Categories.Add(CreateCategory("2", "Hidden", isHidden: true));
        _repository.Categories.Add(CreateCategory("3", "Visible2", isHidden: false));

        // Act
        var result = await _service.GetVisibleCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(c => !c.IsHidden).Should().BeTrue();
    }

    #endregion

    #region GetCategoryByIdAsync Tests

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetCategoryByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsCategory_WhenFound()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("test-id", "Test Category"));

        // Act
        var result = await _service.GetCategoryByIdAsync("test-id");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Category");
    }

    #endregion

    #region CreateCategoryAsync Tests

    [Fact]
    public async Task CreateCategoryAsync_CreatesCategory()
    {
        // Act
        var result = await _service.CreateCategoryAsync("New Category");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("New Category");
        result.IsSystemCategory.Should().BeFalse();
        _repository.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateCategoryAsync_ThrowsException_WhenNameEmpty()
    {
        // Act & Assert
        var act = () => _service.CreateCategoryAsync("");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Category name cannot be empty*");
    }

    [Fact]
    public async Task CreateCategoryAsync_ThrowsException_WhenNameTooLong()
    {
        // Act & Assert
        var act = () => _service.CreateCategoryAsync(new string('a', 101));
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Category name cannot exceed 100 characters*");
    }

    [Fact]
    public async Task CreateCategoryAsync_ThrowsException_WhenNameExists()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "Existing"));

        // Act & Assert
        var act = () => _service.CreateCategoryAsync("Existing");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateCategoryAsync_ThrowsException_WhenMaxReached()
    {
        // Arrange - Add 50 categories (max limit)
        for (int i = 0; i < 50; i++)
        {
            _repository.Categories.Add(CreateCategory($"id-{i}", $"Category {i}"));
        }

        // Act & Assert
        var act = () => _service.CreateCategoryAsync("One More");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Maximum number of categories*");
    }

    [Fact]
    public async Task CreateCategoryAsync_SetsDefaultValues()
    {
        // Act
        var result = await _service.CreateCategoryAsync("Test", "custom-icon", "#FF0000", "Description");

        // Assert
        result.IconKey.Should().Be("custom-icon");
        result.ColorHex.Should().Be("#FF0000");
        result.Description.Should().Be("Description");
        result.IsHidden.Should().BeFalse();
        result.DisplayOrder.Should().Be(0);
    }

    #endregion

    #region UpdateCategoryAsync Tests

    [Fact]
    public async Task UpdateCategoryAsync_UpdatesCategory()
    {
        // Arrange
        var category = CreateCategory("test-id", "Original");
        _repository.Categories.Add(category);

        category.Name = "Updated";

        // Act
        var result = await _service.UpdateCategoryAsync(category);

        // Assert
        result.Should().BeTrue();
        _repository.Categories.First().Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var category = CreateCategory("non-existent", "Test");

        // Act
        var result = await _service.UpdateCategoryAsync(category);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCategoryAsync_ThrowsException_WhenSystemCategory()
    {
        // Arrange
        var category = CreateCategory("system-id", "System", isSystem: true);
        _repository.Categories.Add(category);

        // Act & Assert
        var act = () => _service.UpdateCategoryAsync(category);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*System categories cannot be modified*");
    }

    [Fact]
    public async Task UpdateCategoryAsync_ThrowsException_WhenDuplicateName()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "Category A"));
        _repository.Categories.Add(CreateCategory("2", "Category B"));

        var categoryToUpdate = _repository.Categories.First(c => c.Id == "2");
        categoryToUpdate.Name = "Category A";

        // Act & Assert
        var act = () => _service.UpdateCategoryAsync(categoryToUpdate);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    #endregion

    #region DeleteCategoryAsync Tests

    [Fact]
    public async Task DeleteCategoryAsync_DeletesCategory()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("to-delete", "Delete Me"));

        // Act
        var result = await _service.DeleteCategoryAsync("to-delete");

        // Assert
        result.Should().BeTrue();
        _repository.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _service.DeleteCategoryAsync("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ThrowsException_WhenSystemCategory()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("system-id", "System", isSystem: true));

        // Act & Assert
        var act = () => _service.DeleteCategoryAsync("system-id");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*System categories cannot be deleted*");
    }

    #endregion

    #region ToggleCategoryVisibilityAsync Tests

    [Fact]
    public async Task ToggleCategoryVisibilityAsync_TogglesVisibility()
    {
        // Arrange
        var category = CreateCategory("test-id", "Test", isHidden: false);
        _repository.Categories.Add(category);

        // Act
        var result = await _service.ToggleCategoryVisibilityAsync("test-id");

        // Assert
        result.Should().BeTrue();
        _repository.Categories.First().IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleCategoryVisibilityAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _service.ToggleCategoryVisibilityAsync("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ReorderCategoriesAsync Tests

    [Fact]
    public async Task ReorderCategoriesAsync_ReordersCategories()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "Cat1", displayOrder: 0));
        _repository.Categories.Add(CreateCategory("2", "Cat2", displayOrder: 1));
        _repository.Categories.Add(CreateCategory("3", "Cat3", displayOrder: 2));

        // Act
        await _service.ReorderCategoriesAsync(new[] { "3", "1", "2" });

        // Assert
        _repository.Categories.First(c => c.Id == "3").DisplayOrder.Should().Be(0);
        _repository.Categories.First(c => c.Id == "1").DisplayOrder.Should().Be(1);
        _repository.Categories.First(c => c.Id == "2").DisplayOrder.Should().Be(2);
    }

    #endregion

    #region AddActionToCategoryAsync Tests

    [Fact]
    public async Task AddActionToCategoryAsync_AddsAction()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("cat-1", "Category"));

        // Act
        await _service.AddActionToCategoryAsync("action-1", "cat-1");

        // Assert
        _repository.CategoryActions.Should().ContainKey("cat-1");
        _repository.CategoryActions["cat-1"].Should().Contain("action-1");
    }

    [Fact]
    public async Task AddActionToCategoryAsync_ThrowsException_WhenIdsEmpty()
    {
        // Act & Assert
        var act = () => _service.AddActionToCategoryAsync("", "cat-1");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetActionsInCategoryAsync Tests

    [Fact]
    public async Task GetActionsInCategoryAsync_ReturnsActionIds()
    {
        // Arrange
        _repository.CategoryActions["cat-1"] = new List<string> { "action-1", "action-2" };

        // Act
        var result = await _service.GetActionsInCategoryAsync("cat-1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("action-1");
        result.Should().Contain("action-2");
    }

    #endregion

    #region ValidateCategoryNameAsync Tests

    [Fact]
    public async Task ValidateCategoryNameAsync_ReturnsFalse_WhenNameEmpty()
    {
        // Act
        var result = await _service.ValidateCategoryNameAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCategoryNameAsync_ReturnsFalse_WhenNameExists()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "Existing"));

        // Act
        var result = await _service.ValidateCategoryNameAsync("Existing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCategoryNameAsync_ReturnsTrue_WhenExcludingSameId()
    {
        // Arrange
        _repository.Categories.Add(CreateCategory("1", "MyCategory"));

        // Act - Validating name for the same category (editing)
        var result = await _service.ValidateCategoryNameAsync("MyCategory", "1");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private static CustomCategory CreateCategory(
        string id,
        string name,
        bool isHidden = false,
        bool isSystem = false,
        int displayOrder = 0)
    {
        return new CustomCategory
        {
            Id = id,
            Name = name,
            IsHidden = isHidden,
            IsSystemCategory = isSystem,
            DisplayOrder = displayOrder,
            IconKey = "folder",
            ColorHex = "#2196F3"
        };
    }

    #endregion
}

/// <summary>
/// Fake in-memory implementation of ICustomCategoryRepository for testing
/// </summary>
internal class FakeCustomCategoryRepository : ICustomCategoryRepository
{
    public List<CustomCategory> Categories { get; } = new();
    public Dictionary<string, List<string>> CategoryActions { get; } = new();

    public Task<IEnumerable<CustomCategory>> GetAllAsync()
    {
        return Task.FromResult(Categories.AsEnumerable());
    }

    public Task<CustomCategory?> GetByIdAsync(string id)
    {
        return Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));
    }

    public Task<CustomCategory> CreateAsync(CustomCategory category)
    {
        Categories.Add(category);
        return Task.FromResult(category);
    }

    public Task UpdateAsync(CustomCategory category)
    {
        var index = Categories.FindIndex(c => c.Id == category.Id);
        if (index >= 0)
        {
            Categories[index] = category;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var category = Categories.FirstOrDefault(c => c.Id == id);
        if (category != null)
        {
            Categories.Remove(category);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<CustomCategory>> GetVisibleCategoriesAsync()
    {
        return Task.FromResult(Categories.Where(c => !c.IsHidden).AsEnumerable());
    }

    public Task<IEnumerable<string>> GetActionIdsForCategoryAsync(string categoryId)
    {
        if (CategoryActions.TryGetValue(categoryId, out var actions))
        {
            return Task.FromResult(actions.AsEnumerable());
        }
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task AddActionToCategoryAsync(string actionId, string categoryId)
    {
        if (!CategoryActions.ContainsKey(categoryId))
        {
            CategoryActions[categoryId] = new List<string>();
        }
        CategoryActions[categoryId].Add(actionId);
        return Task.CompletedTask;
    }

    public Task RemoveActionFromCategoryAsync(string actionId, string categoryId)
    {
        if (CategoryActions.TryGetValue(categoryId, out var actions))
        {
            actions.Remove(actionId);
        }
        return Task.CompletedTask;
    }

    public Task<bool> IsCategorySystemAsync(string categoryId)
    {
        var category = Categories.FirstOrDefault(c => c.Id == categoryId);
        return Task.FromResult(category?.IsSystemCategory ?? false);
    }

    public Task<int> GetNextDisplayOrderAsync()
    {
        if (!Categories.Any())
            return Task.FromResult(0);
        return Task.FromResult(Categories.Max(c => c.DisplayOrder) + 1);
    }

    public Task<int> GetCountAsync()
    {
        return Task.FromResult(Categories.Count);
    }

    public Task<bool> ExistsByNameAsync(string name, string? excludeId = null)
    {
        var exists = Categories.Any(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (excludeId == null || c.Id != excludeId));
        return Task.FromResult(exists);
    }

    public Task UpdateBatchAsync(IEnumerable<CustomCategory> categories)
    {
        foreach (var category in categories)
        {
            var index = Categories.FindIndex(c => c.Id == category.Id);
            if (index >= 0)
            {
                Categories[index] = category;
            }
        }
        return Task.CompletedTask;
    }
}
