using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinShell.Core.Enums;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.App.ViewModels;

/// <summary>
/// ViewModel for managing and executing command batches
/// </summary>
public partial class BatchViewModel : ObservableObject
{
    private readonly IBatchService _batchService;
    private readonly IBatchExecutionService _batchExecutionService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<CommandBatch> _batches = new();

    [ObservableProperty]
    private CommandBatch? _selectedBatch;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private ObservableCollection<OutputLineViewModel> _outputLines = new();

    public BatchViewModel(
        IBatchService batchService,
        IBatchExecutionService batchExecutionService,
        INotificationService notificationService)
    {
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
        _batchExecutionService = batchExecutionService ?? throw new ArgumentNullException(nameof(batchExecutionService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        LoadBatchesAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task LoadBatchesAsync()
    {
        try
        {
            var batches = await _batchService.GetAllBatchesAsync();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Batches.Clear();
                foreach (var batch in batches)
                {
                    Batches.Add(batch);
                }
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to load batches: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExecuteBatchAsync()
    {
        if (SelectedBatch == null)
        {
            _notificationService.ShowWarning("Please select a batch to execute");
            return;
        }

        if (IsExecuting)
        {
            _notificationService.ShowWarning("A batch is already executing");
            return;
        }

        try
        {
            IsExecuting = true;
            ProgressPercentage = 0;
            OutputLines.Clear();

            var result = await _batchExecutionService.ExecuteBatchAsync(
                SelectedBatch,
                CancellationToken.None,
                timeoutSeconds: 60,
                onProgressChanged: progress =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressPercentage = progress.ProgressPercentage;
                        ProgressMessage = $"Executing command {progress.CurrentCommandIndex + 1} of {progress.TotalCommands}";

                        if (progress.CurrentCommand != null)
                        {
                            OutputLines.Add(new OutputLineViewModel
                            {
                                Text = $"==> {progress.CurrentCommand.ActionTitle}",
                                IsError = false,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    });
                },
                onOutputReceived: output =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLines.Add(new OutputLineViewModel
                        {
                            Text = output.Text,
                            IsError = output.IsError,
                            Timestamp = output.Timestamp
                        });
                    });
                });

            if (result.Success)
            {
                _notificationService.ShowSuccess($"Batch completed successfully. {result.SuccessCount}/{result.ExecutedCount} commands succeeded.");
            }
            else
            {
                _notificationService.ShowWarning($"Batch completed with errors. {result.SuccessCount}/{result.ExecutedCount} commands succeeded, {result.FailureCount} failed.");
            }

            await LoadBatchesAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to execute batch: {ex.Message}");
        }
        finally
        {
            IsExecuting = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task DeleteBatchAsync()
    {
        if (SelectedBatch == null)
        {
            _notificationService.ShowWarning("Please select a batch to delete");
            return;
        }

        try
        {
            await _batchService.DeleteBatchAsync(SelectedBatch.Id);
            _notificationService.ShowSuccess("Batch deleted successfully");
            await LoadBatchesAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to delete batch: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportBatchAsync()
    {
        if (SelectedBatch == null)
        {
            _notificationService.ShowWarning("Please select a batch to export");
            return;
        }

        try
        {
            var json = _batchService.ExportBatchToJson(SelectedBatch);
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"{SelectedBatch.Name}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllTextAsync(dialog.FileName, json);
                _notificationService.ShowSuccess("Batch exported successfully");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to export batch: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportBatchAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var batch = _batchService.ImportBatchFromJson(json);
                await _batchService.CreateBatchAsync(batch);
                _notificationService.ShowSuccess("Batch imported successfully");
                await LoadBatchesAsync();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to import batch: {ex.Message}");
        }
    }
}

/// <summary>
/// ViewModel for output lines
/// </summary>
public class OutputLineViewModel
{
    public string Text { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public DateTime Timestamp { get; set; }
}
