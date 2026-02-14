using System.Collections.Concurrent;
using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Ports;
using Microsoft.Extensions.Options;

namespace FileTransferWeb.Transfer.Infrastructure.Batch;

public sealed class InMemoryTransferBatchRepository(
    IOptions<TransferBatchMemoryOptions> options,
    TimeProvider timeProvider)
    : ITransferBatchRepository, ITransferBatchExpirationCleaner
{
    private readonly ConcurrentDictionary<Guid, Entry> _batches = new();
    private readonly TransferBatchMemoryOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public ValueTask<TransferBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_batches.TryGetValue(batchId, out var entry))
        {
            return ValueTask.FromResult<TransferBatch?>(null);
        }

        if (IsExpired(entry))
        {
            _batches.TryRemove(batchId, out _);
            return ValueTask.FromResult<TransferBatch?>(null);
        }

        return ValueTask.FromResult<TransferBatch?>(entry.Batch);
    }

    public ValueTask SaveAsync(TransferBatch batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expiresAtUtc = _timeProvider.GetUtcNow().Add(TimeSpan.FromMinutes(_options.SlidingTtlMinutes));
        _batches[batch.Id] = new Entry(batch, expiresAtUtc);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveExpiredAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var (batchId, entry) in _batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsExpired(entry))
            {
                _batches.TryRemove(batchId, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    private bool IsExpired(Entry entry)
    {
        return entry.ExpiresAtUtc <= _timeProvider.GetUtcNow();
    }

    private sealed record Entry(TransferBatch Batch, DateTimeOffset ExpiresAtUtc);
}
