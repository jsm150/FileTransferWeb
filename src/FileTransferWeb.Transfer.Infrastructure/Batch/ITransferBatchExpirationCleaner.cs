namespace FileTransferWeb.Transfer.Infrastructure.Batch;

public interface ITransferBatchExpirationCleaner
{
    ValueTask RemoveExpiredAsync(CancellationToken cancellationToken);
}
