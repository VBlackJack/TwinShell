using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ICommandHistoryService _historyService;
    private readonly IClipboardService _clipboardService;

    private List<CommandHistory> _allHistory = new();

    [ObservableProperty]
    private ObservableCollection<CommandHistoryViewModel> _historyItems = new();

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

    private async Task LoadHistoryAsync()
    {
        try
        {
            _allHistory = (await _historyService.GetRecentAsync(1000)).ToList();

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
        catch (Exception ex)
        {
            StatusMessage = $"Error loading history: {ex.Message}";
        }
    }

    private async Task ApplyFiltersAsync()
    {
        await Task.Run(() =>
        {
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
            TotalCount = filteredList.Count;

            // Update UI on main thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                HistoryItems.Clear();
                foreach (var history in filteredList)
                {
                    var viewModel = new CommandHistoryViewModel(history, _clipboardService, this);
                    HistoryItems.Add(viewModel);
                }
            });
        });
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
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing history: {ex.Message}";
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
    }

    internal async Task DeleteHistoryItemAsync(string id)
    {
        try
        {
            await _historyService.DeleteAsync(id);
            await LoadHistoryAsync();
            StatusMessage = "History entry deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting entry: {ex.Message}";
        }
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
