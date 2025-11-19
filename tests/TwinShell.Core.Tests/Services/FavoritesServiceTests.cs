using FluentAssertions;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;
using TwinShell.Core.Services;

namespace TwinShell.Core.Tests.Services;

public class FavoritesServiceTests
{
    private readonly FavoritesService _service;
    private readonly FakeFavoritesRepository _repository;

    public FavoritesServiceTests()
    {
        _repository = new FakeFavoritesRepository();
        _service = new FavoritesService(_repository);
    }

    [Fact]
    public async Task AddFavoriteAsync_Success_WhenUnderLimit()
    {
        // Arrange
        var actionId = "test-action-1";

        // Act
        var result = await _service.AddFavoriteAsync(actionId);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        _repository.Favorites.Should().HaveCount(1);
        _repository.Favorites.First().ActionId.Should().Be(actionId);
    }

    [Fact]
    public async Task AddFavoriteAsync_Fails_WhenAlreadyFavorited()
    {
        // Arrange
        var actionId = "test-action-1";
        await _service.AddFavoriteAsync(actionId);

        // Act
        var result = await _service.AddFavoriteAsync(actionId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already in your favorites");
        _repository.Favorites.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddFavoriteAsync_Fails_WhenLimitReached()
    {
        // Arrange - Add 50 favorites
        for (int i = 0; i < 50; i++)
        {
            await _service.AddFavoriteAsync($"action-{i}");
        }

        // Act
        var result = await _service.AddFavoriteAsync("action-51");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("maximum limit of 50");
        _repository.Favorites.Should().HaveCount(50);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_RemovesCorrectFavorite()
    {
        // Arrange
        await _service.AddFavoriteAsync("action-1");
        await _service.AddFavoriteAsync("action-2");

        // Act
        await _service.RemoveFavoriteAsync("action-1");

        // Assert
        _repository.Favorites.Should().HaveCount(1);
        _repository.Favorites.First().ActionId.Should().Be("action-2");
    }

    [Fact]
    public async Task ToggleFavoriteAsync_AddsWhenNotFavorited()
    {
        // Arrange
        var actionId = "test-action";

        // Act
        var result = await _service.ToggleFavoriteAsync(actionId);

        // Assert
        result.Should().BeTrue();
        _repository.Favorites.Should().HaveCount(1);
        _repository.Favorites.First().ActionId.Should().Be(actionId);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_RemovesWhenAlreadyFavorited()
    {
        // Arrange
        var actionId = "test-action";
        await _service.AddFavoriteAsync(actionId);

        // Act
        var result = await _service.ToggleFavoriteAsync(actionId);

        // Assert
        result.Should().BeFalse();
        _repository.Favorites.Should().BeEmpty();
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsTrueWhenFavorited()
    {
        // Arrange
        var actionId = "test-action";
        await _service.AddFavoriteAsync(actionId);

        // Act
        var result = await _service.IsFavoriteAsync(actionId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsFalseWhenNotFavorited()
    {
        // Act
        var result = await _service.IsFavoriteAsync("test-action");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllFavoritesAsync_ReturnsAllFavorites()
    {
        // Arrange
        await _service.AddFavoriteAsync("action-1");
        await _service.AddFavoriteAsync("action-2");
        await _service.AddFavoriteAsync("action-3");

        // Act
        var favorites = await _service.GetAllFavoritesAsync();

        // Assert
        favorites.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFavoriteCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _service.AddFavoriteAsync("action-1");
        await _service.AddFavoriteAsync("action-2");

        // Act
        var count = await _service.GetFavoriteCountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task ClearAllFavoritesAsync_RemovesAllFavorites()
    {
        // Arrange
        await _service.AddFavoriteAsync("action-1");
        await _service.AddFavoriteAsync("action-2");
        await _service.AddFavoriteAsync("action-3");

        // Act
        await _service.ClearAllFavoritesAsync();

        // Assert
        _repository.Favorites.Should().BeEmpty();
    }

    [Fact]
    public async Task AddFavoriteAsync_SetsDisplayOrderCorrectly()
    {
        // Arrange & Act
        await _service.AddFavoriteAsync("action-1");
        await _service.AddFavoriteAsync("action-2");
        await _service.AddFavoriteAsync("action-3");

        // Assert
        _repository.Favorites[0].DisplayOrder.Should().Be(0);
        _repository.Favorites[1].DisplayOrder.Should().Be(1);
        _repository.Favorites[2].DisplayOrder.Should().Be(2);
    }
}

/// <summary>
/// Fake in-memory implementation of IFavoritesRepository for testing
/// </summary>
internal class FakeFavoritesRepository : IFavoritesRepository
{
    public List<UserFavorite> Favorites { get; } = new();

    public Task AddAsync(UserFavorite favorite)
    {
        Favorites.Add(favorite);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<UserFavorite>> GetAllAsync(string? userId = null)
    {
        var query = Favorites.AsEnumerable();
        if (userId != null)
        {
            query = query.Where(f => f.UserId == userId);
        }
        else
        {
            query = query.Where(f => f.UserId == null);
        }

        return Task.FromResult(query.OrderBy(f => f.DisplayOrder));
    }

    public Task<UserFavorite?> GetByActionIdAsync(string actionId, string? userId = null)
    {
        var favorite = Favorites.FirstOrDefault(f =>
            f.ActionId == actionId &&
            (userId == null ? f.UserId == null : f.UserId == userId));

        return Task.FromResult(favorite);
    }

    public Task<bool> IsFavoriteAsync(string actionId, string? userId = null)
    {
        var exists = Favorites.Any(f =>
            f.ActionId == actionId &&
            (userId == null ? f.UserId == null : f.UserId == userId));

        return Task.FromResult(exists);
    }

    public Task RemoveAsync(string favoriteId)
    {
        var favorite = Favorites.FirstOrDefault(f => f.Id == favoriteId);
        if (favorite != null)
        {
            Favorites.Remove(favorite);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByActionIdAsync(string actionId, string? userId = null)
    {
        var favorite = Favorites.FirstOrDefault(f =>
            f.ActionId == actionId &&
            (userId == null ? f.UserId == null : f.UserId == userId));

        if (favorite != null)
        {
            Favorites.Remove(favorite);
        }
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync(string? userId = null)
    {
        var count = Favorites.Count(f =>
            userId == null ? f.UserId == null : f.UserId == userId);

        return Task.FromResult(count);
    }

    public Task UpdateDisplayOrderAsync(string favoriteId, int newOrder)
    {
        var favorite = Favorites.FirstOrDefault(f => f.Id == favoriteId);
        if (favorite != null)
        {
            favorite.DisplayOrder = newOrder;
        }
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(string? userId = null)
    {
        var toRemove = Favorites.Where(f =>
            userId == null ? f.UserId == null : f.UserId == userId).ToList();

        foreach (var favorite in toRemove)
        {
            Favorites.Remove(favorite);
        }
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<UserFavorite> favorites)
    {
        // Stub implementation for testing
        foreach (var favorite in favorites)
        {
            Favorites.Add(favorite);
        }
        return Task.CompletedTask;
    }
}
