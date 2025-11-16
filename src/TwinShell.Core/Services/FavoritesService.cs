using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing user favorites
/// </summary>
public class FavoritesService : IFavoritesService
{
    private readonly IFavoritesRepository _repository;
    private const int MaxFavorites = 50;

    public FavoritesService(IFavoritesRepository repository)
    {
        _repository = repository;
    }

    public async Task<(bool Success, string? ErrorMessage)> AddFavoriteAsync(string actionId, string? userId = null)
    {
        // Check if already favorited
        if (await _repository.IsFavoriteAsync(actionId, userId))
        {
            return (false, "This action is already in your favorites.");
        }

        // Check limit
        var currentCount = await _repository.GetCountAsync(userId);
        if (currentCount >= MaxFavorites)
        {
            return (false, $"You have reached the maximum limit of {MaxFavorites} favorites. Please remove some favorites before adding new ones.");
        }

        // Add favorite
        var favorite = new UserFavorite
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ActionId = actionId,
            CreatedAt = DateTime.UtcNow,
            DisplayOrder = currentCount // Add at the end
        };

        await _repository.AddAsync(favorite);
        return (true, null);
    }

    public async Task RemoveFavoriteAsync(string actionId, string? userId = null)
    {
        await _repository.RemoveByActionIdAsync(actionId, userId);
    }

    public async Task<bool> ToggleFavoriteAsync(string actionId, string? userId = null)
    {
        var isFavorite = await _repository.IsFavoriteAsync(actionId, userId);

        if (isFavorite)
        {
            await RemoveFavoriteAsync(actionId, userId);
            return false;
        }
        else
        {
            var result = await AddFavoriteAsync(actionId, userId);
            return result.Success;
        }
    }

    public async Task<bool> IsFavoriteAsync(string actionId, string? userId = null)
    {
        return await _repository.IsFavoriteAsync(actionId, userId);
    }

    public async Task<IEnumerable<UserFavorite>> GetAllFavoritesAsync(string? userId = null)
    {
        return await _repository.GetAllAsync(userId);
    }

    public async Task<int> GetFavoriteCountAsync(string? userId = null)
    {
        return await _repository.GetCountAsync(userId);
    }

    public async Task ReorderFavoriteAsync(string favoriteId, int newOrder)
    {
        await _repository.UpdateDisplayOrderAsync(favoriteId, newOrder);
    }

    public async Task ClearAllFavoritesAsync(string? userId = null)
    {
        await _repository.ClearAllAsync(userId);
    }
}
