using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<BatchViewModel> _logger;

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
        INotificationService notificationService,
        ILogger<BatchViewModel> logger)
    {
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
        _batchExecutionService = batchExecutionService ?? throw new ArgumentNullException(nameof(batchExecutionService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // BUGFIX: Removed async call from constructor - batches will be loaded on demand
    }

    [RelayCommand]
    private async Task LoadBatchesAsync()
    {
        try
        {
            // BUGFIX: Removed ConfigureAwait(false) before Dispatcher calls
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
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Failed to load batches");
            _notificationService.ShowError("Failed to load batches");
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

            // BUGFIX: Removed ConfigureAwait(false) because callbacks use Dispatcher
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
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Failed to execute batch");
            _notificationService.ShowError("Failed to execute batch");
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
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Failed to delete batch");
            _notificationService.ShowError("Failed to delete batch");
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
                // BUGFIX: Removed ConfigureAwait(false) before notification call
                await File.WriteAllTextAsync(dialog.FileName, json);
                _notificationService.ShowSuccess("Batch exported successfully");
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Failed to export batch");
            _notificationService.ShowError("Failed to export batch");
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
                // BUGFIX: Removed ConfigureAwait(false) before notification calls
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var batch = _batchService.ImportBatchFromJson(json);
                await _batchService.CreateBatchAsync(batch);
                _notificationService.ShowSuccess("Batch imported successfully");
                await LoadBatchesAsync();
            }
        }
        catch (Exception ex)
        {
            // SECURITY: Don't expose exception details to users
            _logger.LogError(ex, "Failed to import batch");
            _notificationService.ShowError("Failed to import batch");
        }
    }
}
