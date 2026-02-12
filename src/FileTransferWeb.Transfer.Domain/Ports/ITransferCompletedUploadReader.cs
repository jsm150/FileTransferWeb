namespace FileTransferWeb.Transfer.Domain.Ports;

public interface ITransferCompletedUploadReader
{
    ValueTask<TransferCompletedUploadInfo> ReadAsync(
        string uploadId,
        CancellationToken cancellationToken);
}
