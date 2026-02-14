using System.Collections.Concurrent;
using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Ports;

namespace FileTransferWeb.Transfer.Infrastructure.Batch;

public sealed class InMemoryTransferBatchRepository : ITransferBatchRepository
{
    private readonly ConcurrentDictionary<Guid, TransferBatch> _batches = new();

    public ValueTask<TransferBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            _batches.TryGetValue(batchId, out var batch)
                ? batch
                : null);
    }

    public ValueTask SaveAsync(TransferBatch batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _batches[batch.Id] = batch;
        return ValueTask.CompletedTask;
    }
}
