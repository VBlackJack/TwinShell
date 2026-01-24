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
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Services;
using ActionModel = TwinShell.Core.Models.Action;

namespace TwinShell.Core.Tests.Services;

public class ActionServiceTests
{
    private readonly ActionService _service;
    private readonly FakeActionRepository _repository;

    public ActionServiceTests()
    {
        _repository = new FakeActionRepository();
        _service = new ActionService(_repository);
    }

    #region GetAllActionsAsync Tests

    [Fact]
    public async Task GetAllActionsAsync_ReturnsEmptyList_WhenNoActions()
    {
        // Act
        var result = await _service.GetAllActionsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActionsAsync_ReturnsAllActions()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "Category1"));
        _repository.Actions.Add(CreateAction("2", "Action 2", "Category2"));

        // Act
        var result = await _service.GetAllActionsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetActionByIdAsync Tests

    [Fact]
    public async Task GetActionByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetActionByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActionByIdAsync_ReturnsAction_WhenFound()
    {
        // Arrange
        var action = CreateAction("test-id", "Test Action", "TestCategory");
        _repository.Actions.Add(action);

        // Act
        var result = await _service.GetActionByIdAsync("test-id");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-id");
        result.Title.Should().Be("Test Action");
    }

    #endregion

    #region GetActionsByCategoryAsync Tests

    [Fact]
    public async Task GetActionsByCategoryAsync_ReturnsEmptyList_WhenNoMatches()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "Category1"));

        // Act
        var result = await _service.GetActionsByCategoryAsync("NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActionsByCategoryAsync_ReturnsMatchingActions()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "Network"));
        _repository.Actions.Add(CreateAction("2", "Action 2", "Network"));
        _repository.Actions.Add(CreateAction("3", "Action 3", "Security"));

        // Act
        var result = await _service.GetActionsByCategoryAsync("Network");

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.Category == "Network").Should().BeTrue();
    }

    #endregion

    #region GetAllCategoriesAsync Tests

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "Network"));
        _repository.Actions.Add(CreateAction("2", "Action 2", "Network"));
        _repository.Actions.Add(CreateAction("3", "Action 3", "Security"));

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Network");
        result.Should().Contain("Security");
    }

    #endregion

    #region FilterActionsAsync Tests

    [Fact]
    public async Task FilterActionsAsync_FiltersByPlatform()
    {
        // Arrange
        var actions = new List<ActionModel>
        {
            CreateAction("1", "Action 1", "Cat", Platform.Windows),
            CreateAction("2", "Action 2", "Cat", Platform.Linux),
            CreateAction("3", "Action 3", "Cat", Platform.Both)
        };

        // Act
        var result = await _service.FilterActionsAsync(actions, platform: Platform.Windows);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.Id == "1");
        result.Should().Contain(a => a.Id == "3"); // Both includes Windows
    }

    [Fact]
    public async Task FilterActionsAsync_FiltersByLevel()
    {
        // Arrange
        var actions = new List<ActionModel>
        {
            CreateAction("1", "Action 1", "Cat", level: CriticalityLevel.Info),
            CreateAction("2", "Action 2", "Cat", level: CriticalityLevel.Run),
            CreateAction("3", "Action 3", "Cat", level: CriticalityLevel.Dangerous)
        };

        // Act
        var result = await _service.FilterActionsAsync(actions, level: CriticalityLevel.Run);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("2");
    }

    [Fact]
    public async Task FilterActionsAsync_FiltersByPlatformAndLevel()
    {
        // Arrange
        var actions = new List<ActionModel>
        {
            CreateAction("1", "Action 1", "Cat", Platform.Windows, CriticalityLevel.Run),
            CreateAction("2", "Action 2", "Cat", Platform.Linux, CriticalityLevel.Run),
            CreateAction("3", "Action 3", "Cat", Platform.Windows, CriticalityLevel.Info)
        };

        // Act
        var result = await _service.FilterActionsAsync(actions, Platform.Windows, CriticalityLevel.Run);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("1");
    }

    #endregion

    #region CreateActionAsync Tests

    [Fact]
    public async Task CreateActionAsync_CreatesActionWithGeneratedId()
    {
        // Arrange
        var action = new ActionModel { Title = "New Action", Category = "TestCat" };

        // Act
        var result = await _service.CreateActionAsync(action);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        result.IsUserCreated.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _repository.Actions.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateActionAsync_ThrowsException_WhenTitleMissing()
    {
        // Arrange
        var action = new ActionModel { Category = "TestCat" };

        // Act & Assert
        var act = () => _service.CreateActionAsync(action);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Title is required*");
    }

    [Fact]
    public async Task CreateActionAsync_ThrowsException_WhenCategoryMissing()
    {
        // Arrange
        var action = new ActionModel { Title = "Test" };

        // Act & Assert
        var act = () => _service.CreateActionAsync(action);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Category is required*");
    }

    [Fact]
    public async Task CreateActionAsync_ThrowsException_WhenTitleTooLong()
    {
        // Arrange
        var action = new ActionModel
        {
            Title = new string('a', 201), // Max is 200
            Category = "TestCat"
        };

        // Act & Assert
        var act = () => _service.CreateActionAsync(action);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Title cannot exceed*");
    }

    #endregion

    #region UpdateActionAsync Tests

    [Fact]
    public async Task UpdateActionAsync_UpdatesAction()
    {
        // Arrange
        var action = CreateAction("test-id", "Original", "Cat");
        _repository.Actions.Add(action);

        action.Title = "Updated Title";

        // Act
        await _service.UpdateActionAsync(action);

        // Assert
        var updated = _repository.Actions.First();
        updated.Title.Should().Be("Updated Title");
        updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateActionAsync_ThrowsException_WhenInvalid()
    {
        // Arrange
        var action = new ActionModel { Id = "test", Title = "" };

        // Act & Assert
        var act = () => _service.UpdateActionAsync(action);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region DeleteActionAsync Tests

    [Fact]
    public async Task DeleteActionAsync_RemovesAction()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("to-delete", "Action", "Cat"));
        _repository.Actions.Add(CreateAction("keep", "Action 2", "Cat"));

        // Act
        await _service.DeleteActionAsync("to-delete");

        // Assert
        _repository.Actions.Should().HaveCount(1);
        _repository.Actions.First().Id.Should().Be("keep");
    }

    #endregion

    #region GetActionCountByCategoryAsync Tests

    [Fact]
    public async Task GetActionCountByCategoryAsync_ReturnsCorrectCount()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "Network"));
        _repository.Actions.Add(CreateAction("2", "Action 2", "Network"));
        _repository.Actions.Add(CreateAction("3", "Action 3", "Security"));

        // Act
        var count = await _service.GetActionCountByCategoryAsync("Network");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetActionCountByCategoryAsync_ReturnsZero_WhenCategoryNotFound()
    {
        // Act
        var count = await _service.GetActionCountByCategoryAsync("NonExistent");

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region RenameCategoryAsync Tests

    [Fact]
    public async Task RenameCategoryAsync_ReturnsFalse_WhenOldNameEmpty()
    {
        // Act
        var result = await _service.RenameCategoryAsync("", "NewName");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RenameCategoryAsync_ReturnsFalse_WhenNewNameEmpty()
    {
        // Act
        var result = await _service.RenameCategoryAsync("OldName", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RenameCategoryAsync_ReturnsTrue_WhenSameName()
    {
        // Act
        var result = await _service.RenameCategoryAsync("Category", "Category");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RenameCategoryAsync_RenamesCategory()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "OldCategory"));

        // Act
        var result = await _service.RenameCategoryAsync("OldCategory", "NewCategory");

        // Assert
        result.Should().BeTrue();
        _repository.Actions.First().Category.Should().Be("NewCategory");
    }

    #endregion

    #region DeleteCategoryAsync Tests

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenNameEmpty()
    {
        // Act
        var result = await _service.DeleteCategoryAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategoryAsync_ClearsCategory()
    {
        // Arrange
        _repository.Actions.Add(CreateAction("1", "Action 1", "ToDelete"));

        // Act
        var result = await _service.DeleteCategoryAsync("ToDelete");

        // Assert
        result.Should().BeTrue();
        _repository.Actions.First().Category.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private static ActionModel CreateAction(
        string id,
        string title,
        string category,
        Platform platform = Platform.Windows,
        CriticalityLevel level = CriticalityLevel.Info)
    {
        return new ActionModel
        {
            Id = id,
            Title = title,
            Category = category,
            Platform = platform,
            Level = level
        };
    }

    #endregion
}

/// <summary>
/// Fake in-memory implementation of IActionRepository for testing
/// </summary>
internal class FakeActionRepository : IActionRepository
{
    public List<ActionModel> Actions { get; } = new();

    public Task<IEnumerable<ActionModel>> GetAllAsync()
    {
        return Task.FromResult(Actions.AsEnumerable());
    }

    public Task<ActionModel?> GetByIdAsync(string id)
    {
        return Task.FromResult(Actions.FirstOrDefault(a => a.Id == id));
    }

    public Task<IEnumerable<ActionModel>> GetByCategoryAsync(string category)
    {
        return Task.FromResult(Actions.Where(a => a.Category == category).AsEnumerable());
    }

    public Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        return Task.FromResult(Actions.Select(a => a.Category).Distinct());
    }

    public Task AddAsync(ActionModel action)
    {
        Actions.Add(action);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ActionModel action)
    {
        var index = Actions.FindIndex(a => a.Id == action.Id);
        if (index >= 0)
        {
            Actions[index] = action;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var action = Actions.FirstOrDefault(a => a.Id == id);
        if (action != null)
        {
            Actions.Remove(action);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(Actions.Any(a => a.Id == id));
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(Actions.Count);
    }

    public Task<int> CountByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Task.FromResult(0);
        return Task.FromResult(Actions.Count(a => a.Category == category));
    }

    public Task<int> UpdateCategoryForActionsAsync(string oldCategory, string? newCategory)
    {
        var count = 0;
        foreach (var action in Actions.Where(a => a.Category == oldCategory))
        {
            action.Category = newCategory ?? string.Empty;
            count++;
        }
        return Task.FromResult(count);
    }
}
