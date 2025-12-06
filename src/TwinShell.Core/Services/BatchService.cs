using System.Text.Json;
using TwinShell.Core.Interfaces;
using TwinShell.Core.Models;

namespace TwinShell.Core.Services;

/// <summary>
/// Service for managing command batches
/// </summary>
public class BatchService : IBatchService
{
    private readonly IBatchRepository _repository;

    // PERFORMANCE: Static JsonSerializerOptions to avoid recreation on each call
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ImportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BatchService(IBatchRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<CommandBatch>> GetAllBatchesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<CommandBatch?> GetBatchByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<CommandBatch> CreateBatchAsync(CommandBatch batch)
    {
        batch.Id = Guid.NewGuid().ToString();
        batch.CreatedAt = DateTime.UtcNow;
        batch.UpdatedAt = DateTime.UtcNow;
        batch.IsUserCreated = true;

        // Ensure all commands have IDs and proper batch reference
        for (int i = 0; i < batch.Commands.Count; i++)
        {
            var command = batch.Commands[i];
            if (string.IsNullOrEmpty(command.Id))
            {
                command.Id = Guid.NewGuid().ToString();
            }
            command.BatchId = batch.Id;
            command.Order = i;
        }

        await _repository.AddAsync(batch);
        return batch;
    }

    public async Task UpdateBatchAsync(CommandBatch batch)
    {
        batch.UpdatedAt = DateTime.UtcNow;

        // Ensure order is correct
        for (int i = 0; i < batch.Commands.Count; i++)
        {
            batch.Commands[i].Order = i;
        }

        await _repository.UpdateAsync(batch);
    }

    public async Task DeleteBatchAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<CommandBatch>> SearchBatchesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllBatchesAsync();
        }

        return await _repository.SearchAsync(query);
    }

    public string ExportBatchToJson(CommandBatch batch)
    {
        return JsonSerializer.Serialize(batch, ExportJsonOptions);
    }

    public CommandBatch ImportBatchFromJson(string json)
    {
        var batch = JsonSerializer.Deserialize<CommandBatch>(json, ImportJsonOptions);

        if (batch == null)
        {
            throw new InvalidOperationException("Failed to deserialize batch from JSON");
        }

        // Generate new IDs to avoid conflicts
        batch.Id = Guid.NewGuid().ToString();
        batch.CreatedAt = DateTime.UtcNow;
        batch.UpdatedAt = DateTime.UtcNow;
        batch.LastExecutedAt = null;

        // Reset execution status and generate new IDs for commands
        for (int i = 0; i < batch.Commands.Count; i++)
        {
            var command = batch.Commands[i];
            command.Id = Guid.NewGuid().ToString();
            command.BatchId = batch.Id;
            command.Order = i;
            command.IsExecuted = false;
            command.ExecutionResult = null;
        }

        return batch;
    }
}
