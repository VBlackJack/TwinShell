using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

public partial class RecentCommandsViewModel : ObservableObject
{
    private readonly ICommandHistoryService _historyService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private ObservableCollection<RecentCommandItemViewModel> _recentCommands = new();

    public RecentCommandsViewModel(
        ICommandHistoryService historyService,
        IClipboardService clipboardService)
    {
        _historyService = historyService;
        _clipboardService = clipboardService;
    }

    public async Task InitializeAsync()
    {
        await LoadRecentCommandsAsync();
    }

    public async Task LoadRecentCommandsAsync()
    {
        try
        {
            var recent = await _historyService.GetRecentAsync(5);

            RecentCommands.Clear();
            foreach (var command in recent)
            {
                RecentCommands.Add(new RecentCommandItemViewModel(command, _clipboardService));
            }
        }
        catch (Exception ex)
        {
            // BUGFIX: Log exception instead of silently failing
            // Widget is optional but we should track failures for debugging
            System.Diagnostics.Debug.WriteLine($"Failed to load recent commands: {ex.Message}");
            // Could also set a flag to display 'Recent commands unavailable' to user
        }
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        // This will be handled by the parent window
        // We'll use a messenger or event for this
    }
}

public partial class RecentCommandItemViewModel : ObservableObject
{
    private readonly CommandHistory _history;
    private readonly IClipboardService _clipboardService;

    public string GeneratedCommand => _history.GeneratedCommand;
    public string ActionTitle => _history.ActionTitle;
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

            return CreatedAt.ToLocalTime().ToString("MMM dd, HH:mm");
        }
    }

    public RecentCommandItemViewModel(CommandHistory history, IClipboardService clipboardService)
    {
        _history = history;
        _clipboardService = clipboardService;
    }

    [RelayCommand]
    private void Copy()
    {
        _clipboardService.SetText(GeneratedCommand);
    }
}
