using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.App.Collections;
using TwinShell.Core.Constants;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

// BUGFIX: Implement IDisposable to properly dispose SemaphoreSlim
public partial class HistoryViewModel : ObservableObject, IDisposable
{
    private readonly ICommandHistoryService _historyService;
    private readonly IClipboardService _clipboardService;
    private readonly SemaphoreSlim _historyLock = new SemaphoreSlim(1, 1);
    private bool _disposed;

    private List<CommandHistory> _allHistory = new();

    [ObservableProperty]
    private ObservableRangeCollection<CommandHistoryViewModel> _historyItems = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedDateFilter = "All";

    [ObservableProperty]
    private string? _selectedCategory;

    [ObservableProperty]
    private Platform? _selectedPlatform;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // PERFORMANCE: Pagination properties to avoid loading 1000 entries at once
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = ValidationConstants.DefaultHistoryPageSize;

    [ObservableProperty]
    private int _totalFilteredCount;

    public ObservableCollection<string> DateFilterOptions { get; } = new()
    {
        "All",
        "Today",
        "Last 7 days",
        "Last 30 days",
        "Custom"
    };

    public ObservableCollection<string> Categories { get; } = new();

    public HistoryViewModel(
        ICommandHistoryService historyService,
        IClipboardService clipboardService)
    {
        _historyService = historyService;
        _clipboardService = clipboardService;
    }

    public async Task InitializeAsync()
    {
        await LoadHistoryAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedDateFilterChanged(string value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedPlatformChanged(Platform? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnCurrentPageChanged(int value)
    {
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
        _ = ApplyFiltersAsync();
    }

    partial void OnTotalPagesChanged(int value)
    {
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1; // Reset to first page when page size changes
        _ = ApplyFiltersAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            _allHistory = (await _historyService.GetRecentAsync(ValidationConstants.DefaultHistoryLoadCount)).ToList();

            // Extract unique categories
            var categories = _allHistory
                .Select(h => h.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            await ApplyFiltersAsync();
            StatusMessage = $"Loaded {_allHistory.Count} history entries";
        }
        catch (Exception)
        {
            // SECURITY: Don't expose exception details to users
            StatusMessage = "Error loading history";
        }
    }

    private async Task ApplyFiltersAsync()
    {
        // Use semaphore to prevent concurrent filter operations
        await _historyLock.WaitAsync();
        try
        {
            // PERFORMANCE: Filtering is fast and synchronous - no need for Task.Run()
            var filtered = _allHistory.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(h =>
                    h.GeneratedCommand.ToLower().Contains(search) ||
                    h.ActionTitle.ToLower().Contains(search));
            }

            // Date filter
            var now = DateTime.UtcNow;
            switch (SelectedDateFilter)
            {
                case "Today":
                    var todayStart = now.Date;
                    filtered = filtered.Where(h => h.CreatedAt >= todayStart);
                    break;
                case "Last 7 days":
                    var weekAgo = now.AddDays(-7);
                    filtered = filtered.Where(h => h.CreatedAt >= weekAgo);
                    break;
                case "Last 30 days":
                    var monthAgo = now.AddDays(-30);
                    filtered = filtered.Where(h => h.CreatedAt >= monthAgo);
                    break;
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(SelectedCategory))
            {
                filtered = filtered.Where(h => h.Category == SelectedCategory);
            }

            // Platform filter
            if (SelectedPlatform.HasValue)
            {
                filtered = filtered.Where(h => h.Platform == SelectedPlatform.Value);
            }

            var filteredList = filtered.ToList();
            TotalFilteredCount = filteredList.Count;
            TotalCount = _allHistory.Count;

            // PERFORMANCE: Calculate pagination
            TotalPages = (int)Math.Ceiling((double)TotalFilteredCount / PageSize);
            if (TotalPages < 1) TotalPages = 1;

            // Ensure current page is within bounds
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            // PERFORMANCE: Apply pagination - only load current page items
            var pagedList = filteredList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

            // PERFORMANCE: Use ReplaceRange for batch update - single UI notification
            var viewModels = pagedList.Select(h =>
                new CommandHistoryViewModel(h, _clipboardService, this));
            HistoryItems.ReplaceRange(viewModels);
        }
        finally
        {
            _historyLock.Release();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to clear all command history?",
            "Clear All History",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                await _historyService.ClearAllAsync();
                await LoadHistoryAsync();
                StatusMessage = "All history cleared successfully";
            }
            catch (Exception)
            {
                // SECURITY: Don't expose exception details to users
                StatusMessage = "Error clearing history";
            }
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedDateFilter = "All";
        SelectedCategory = null;
        SelectedPlatform = null;
        CurrentPage = 1;
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void GoToPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    private bool CanGoToPreviousPage() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    private bool CanGoToNextPage() => CurrentPage < TotalPages;

    [RelayCommand]
    private void GoToFirstPage()
    {
        CurrentPage = 1;
    }

    [RelayCommand]
    private void GoToLastPage()
    {
        CurrentPage = TotalPages;
    }

    internal async Task DeleteHistoryItemAsync(string id)
    {
        try
        {
            await _historyService.DeleteAsync(id);
            await LoadHistoryAsync();
            StatusMessage = "History entry deleted";
        }
        catch (Exception)
        {
            // SECURITY: Don't expose exception details to users
            StatusMessage = "Error deleting entry";
        }
    }

    /// <summary>
    /// Dispose resources to prevent memory leaks
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _historyLock?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public partial class CommandHistoryViewModel : ObservableObject
{
    private readonly CommandHistory _history;
    private readonly IClipboardService _clipboardService;
    private readonly HistoryViewModel _parentViewModel;

    public string Id => _history.Id;
    public string GeneratedCommand => _history.GeneratedCommand;
    public string ActionTitle => _history.ActionTitle;
    public string Category => _history.Category;
    public Platform Platform => _history.Platform;
    public DateTime CreatedAt => _history.CreatedAt;

    public string RelativeTime
    {
        get
        {
            var timeSpan = DateTime.UtcNow - CreatedAt;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";

            return CreatedAt.ToLocalTime().ToString("MMM dd, yyyy");
        }
    }

    public string PlatformDisplay => Platform switch
    {
        Platform.Windows => "Windows",
        Platform.Linux => "Linux",
        _ => "Both"
    };

    public CommandHistoryViewModel(
        CommandHistory history,
        IClipboardService clipboardService,
        HistoryViewModel parentViewModel)
    {
        _history = history;
        _clipboardService = clipboardService;
        _parentViewModel = parentViewModel;
    }

    [RelayCommand]
    private void Copy()
    {
        _clipboardService.SetText(GeneratedCommand);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to delete this history entry?",
            "Delete History",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _parentViewModel.DeleteHistoryItemAsync(Id);
        }
    }
}
