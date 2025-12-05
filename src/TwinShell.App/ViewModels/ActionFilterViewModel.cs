using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwinShell.App.Collections;
using TwinShell.Core.Constants;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for action filtering and search functionality.
/// Manages search text, category filters, platform filters, and level filters.
/// </summary>
public partial class ActionFilterViewModel : ObservableObject, IDisposable
{
    private readonly ISearchService _searchService;
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<ActionFilterViewModel> _logger;
    private readonly SemaphoreSlim _filterSemaphore = new(1, 1);
    private bool _disposed;

    private List<ActionModel> _allActions = new();
    private HashSet<string> _favoriteActionIds = new();

    /// <summary>
    /// Event raised when filters change and results are updated
    /// </summary>
    public event EventHandler<IReadOnlyList<ActionModel>>? FiltersChanged;

    [ObservableProperty]
    private ObservableCollection<string> _categories = new();

    [ObservableProperty]
    private ObservableRangeCollection<ActionModel> _filteredActions = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _selectedCategory;

    [ObservableProperty]
    private bool _filterWindows = true;

    [ObservableProperty]
    private bool _filterLinux = true;

    [ObservableProperty]
    private bool _filterBoth = true;

    [ObservableProperty]
    private bool _filterInfo = true;

    [ObservableProperty]
    private bool _filterRun = true;

    [ObservableProperty]
    private bool _filterDangerous = true;

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private int _searchResultCount;

    [ObservableProperty]
    private string _searchTime = string.Empty;

    [ObservableProperty]
    private bool _showSearchMetrics;

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

    public ActionFilterViewModel(
        ISearchService searchService,
        ISearchHistoryService searchHistoryService,
        IFavoritesService favoritesService,
        ILocalizationService localizationService,
        ILogger<ActionFilterViewModel> logger)
    {
        _searchService = searchService;
        _searchHistoryService = searchHistoryService;
        _favoritesService = favoritesService;
        _localizationService = localizationService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize with actions and categories
    /// </summary>
    public async Task InitializeAsync(IEnumerable<ActionModel> actions, IEnumerable<string> categories)
    {
        _allActions = actions.ToList();

        // Load favorites
        var favorites = await _favoritesService.GetAllFavoritesAsync();
        _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();

        // Build category list with special categories
        var categoryList = categories.ToList();
        categoryList.Insert(0, UIConstants.FavoritesCategoryDisplay);
        categoryList.Insert(0, UIConstants.AllCategoryDisplay);
        Categories = new ObservableCollection<string>(categoryList);

        await ApplyFiltersAsync();
    }

    /// <summary>
    /// Reload actions (e.g., after add/edit/delete)
    /// </summary>
    public async Task ReloadActionsAsync(IEnumerable<ActionModel> actions)
    {
        _allActions = actions.ToList();

        // Reload favorites
        var favorites = await _favoritesService.GetAllFavoritesAsync();
        _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();

        await ApplyFiltersAsync();
    }

    /// <summary>
    /// Update favorites cache after toggle
    /// </summary>
    public async Task RefreshFavoritesAsync()
    {
        var favorites = await _favoritesService.GetAllFavoritesAsync();
        _favoriteActionIds = favorites.Select(f => f.ActionId).ToHashSet();
        await ApplyFiltersAsync();
    }

    /// <summary>
    /// Check if an action is a favorite
    /// </summary>
    public bool IsFavorite(string actionId) => _favoriteActionIds.Contains(actionId);

    // Property change handlers
    partial void OnSearchTextChanged(string value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnSelectedCategoryChanged(string? value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterWindowsChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterLinuxChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterBothChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterInfoChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterRunChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnFilterDangerousChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);
    partial void OnShowFavoritesOnlyChanged(bool value) => SafeExecuteAsync(ApplyFiltersAsync);

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
        FilterWindows = true;
        FilterLinux = true;
        FilterBoth = true;
        FilterInfo = true;
        FilterRun = true;
        FilterDangerous = true;
        ShowFavoritesOnly = false;
    }

    private async Task ApplyFiltersAsync()
    {
        await _filterSemaphore.WaitAsync();
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var filtered = _allActions.AsEnumerable();

            var hasActiveSearch = !string.IsNullOrWhiteSpace(SearchText);

            // Favorites filter (special category)
            if (!hasActiveSearch && SelectedCategory == UIConstants.FavoritesCategoryDisplay)
            {
                filtered = filtered.Where(a => _favoriteActionIds.Contains(a.Id));
            }
            // All category - no filtering
            else if (!hasActiveSearch && SelectedCategory == UIConstants.AllCategoryDisplay)
            {
                // No filtering
            }
            // Category filter (only apply if no active search)
            else if (!hasActiveSearch && !string.IsNullOrEmpty(SelectedCategory))
            {
                filtered = filtered.Where(a => a.Category == SelectedCategory);
            }

            // Show favorites only filter
            if (ShowFavoritesOnly)
            {
                filtered = filtered.Where(a => _favoriteActionIds.Contains(a.Id));
            }

            // Search filter
            if (hasActiveSearch)
            {
                filtered = await _searchService.SearchAsync(filtered, SearchText);
            }

            // Platform filter
            var platformFilters = new List<Platform>();
            if (FilterWindows) platformFilters.Add(Platform.Windows);
            if (FilterLinux) platformFilters.Add(Platform.Linux);
            if (FilterBoth) platformFilters.Add(Platform.Both);

            if (platformFilters.Count > 0 && platformFilters.Count < 3)
            {
                filtered = filtered.Where(a => platformFilters.Contains(a.Platform));
            }

            // Level filter
            var levelFilters = new List<CriticalityLevel>();
            if (FilterInfo) levelFilters.Add(CriticalityLevel.Info);
            if (FilterRun) levelFilters.Add(CriticalityLevel.Run);
            if (FilterDangerous) levelFilters.Add(CriticalityLevel.Dangerous);

            if (levelFilters.Count > 0 && levelFilters.Count < 3)
            {
                filtered = filtered.Where(a => levelFilters.Contains(a.Level));
            }

            // Materialize results
            var results = filtered.ToList();
            sw.Stop();

            // Update search metrics
            if (hasActiveSearch)
            {
                SearchResultCount = results.Count;
                SearchTime = sw.ElapsedMilliseconds < 1000
                    ? $"{sw.ElapsedMilliseconds}ms"
                    : $"{sw.Elapsed.TotalSeconds:F2}s";
                ShowSearchMetrics = true;

                // Save to search history (fire and forget)
                var searchTextCopy = SearchText;
                var resultCount = results.Count;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _searchHistoryService.AddSearchAsync(searchTextCopy, resultCount);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Search history save failed: {ex.Message}");
                    }
                });

                await UpdateSearchSuggestionsAsync();
            }
            else
            {
                ShowSearchMetrics = false;
                SearchSuggestions.Clear();
            }

            // Update filtered actions
            FilteredActions.ReplaceRange(results);

            // Notify listeners
            FiltersChanged?.Invoke(this, results);
        }
        finally
        {
            _filterSemaphore.Release();
        }
    }

    private async Task UpdateSearchSuggestionsAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
            {
                SearchSuggestions.Clear();
                return;
            }

            var suggestions = await _searchHistoryService.GetSearchSuggestionsAsync(SearchText, limit: 5);
            var filteredSuggestions = suggestions
                .Where(s => !s.Equals(SearchText, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            SearchSuggestions.Clear();
            foreach (var suggestion in filteredSuggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        }
        catch
        {
            SearchSuggestions.Clear();
        }
    }

    private async void SafeExecuteAsync(Func<Task> asyncMethod)
    {
        try
        {
            await asyncMethod();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SafeExecuteAsync");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _filterSemaphore?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
