using FileTransferWeb.Transfer.Domain.Models;

namespace FileTransferWeb.Transfer.Domain.Ports;

public interface ITransferBatchRepository
{
    ValueTask<TransferBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken);

    ValueTask SaveAsync(TransferBatch batch, CancellationToken cancellationToken);
}
