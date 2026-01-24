/*
 * Copyright 2026 Julien Bombled
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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.ViewModels;

/// <summary>
/// UI-006: ViewModel for search and filter functionality.
/// Extracted from MainViewModel to reduce dependency count and improve SRP compliance.
/// </summary>
public partial class SearchViewModel : ObservableObject, IDisposable
{
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ILogger<SearchViewModel> _logger;
    private readonly SemaphoreSlim _filterSemaphore = new(1, 1);

    // Debouncing
    private CancellationTokenSource? _debounceCts;
    private const int DebounceDelayMs = 150;
    private bool _disposed;

    /// <summary>
    /// Event raised when filters change and should be applied.
    /// MainViewModel subscribes to this to trigger filtering.
    /// </summary>
    public event Func<Task>? FiltersChanged;

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
    private ObservableCollection<string> _searchSuggestions = new();

    public SearchViewModel(
        ISearchHistoryService searchHistoryService,
        ILogger<SearchViewModel> logger)
    {
        _searchHistoryService = searchHistoryService;
        _logger = logger;
    }

    partial void OnSearchTextChanged(string value)
    {
        DebouncedNotifyFiltersChanged();
        _ = UpdateSearchSuggestionsAsync(value);
    }

    partial void OnSelectedCategoryChanged(string? value) => NotifyFiltersChanged();
    partial void OnFilterWindowsChanged(bool value) => NotifyFiltersChanged();
    partial void OnFilterLinuxChanged(bool value) => NotifyFiltersChanged();
    partial void OnFilterBothChanged(bool value) => NotifyFiltersChanged();
    partial void OnFilterInfoChanged(bool value) => NotifyFiltersChanged();
    partial void OnFilterRunChanged(bool value) => NotifyFiltersChanged();
    partial void OnFilterDangerousChanged(bool value) => NotifyFiltersChanged();
    partial void OnShowFavoritesOnlyChanged(bool value) => NotifyFiltersChanged();

    /// <summary>
    /// Clears all search filters and resets to defaults.
    /// </summary>
    [RelayCommand]
    public void ClearFilters()
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

    /// <summary>
    /// Records the current search text in search history.
    /// </summary>
    /// <param name="resultCount">Number of results found for this search.</param>
    public async Task RecordSearchAsync(int resultCount)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            await _searchHistoryService.AddSearchAsync(SearchText, resultCount);
        }
    }

    /// <summary>
    /// Acquires the filter semaphore with timeout.
    /// Returns true if acquired, false if timed out.
    /// </summary>
    public async Task<bool> AcquireFilterLockAsync(TimeSpan timeout)
    {
        return await _filterSemaphore.WaitAsync(timeout);
    }

    /// <summary>
    /// Releases the filter semaphore.
    /// </summary>
    public void ReleaseFilterLock()
    {
        _filterSemaphore.Release();
    }

    private void NotifyFiltersChanged()
    {
        FiltersChanged?.Invoke();
    }

    private async void DebouncedNotifyFiltersChanged()
    {
        try
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            await Task.Delay(DebounceDelayMs, token);

            if (!token.IsCancellationRequested)
            {
                FiltersChanged?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debounced filter notification");
        }
    }

    private async Task UpdateSearchSuggestionsAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            SearchSuggestions.Clear();
            return;
        }

        try
        {
            var suggestions = await _searchHistoryService.GetSearchSuggestionsAsync(searchText, 5);

            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating search suggestions");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _filterSemaphore.Dispose();
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
