namespace FileTransferWeb.Transfer.Domain.Ports;

public interface ITransferCompletedFileMover
{
    ValueTask<string> MoveCompletedFileAsync(
        string uploadId,
        string targetRelativePath,
        string storedFileName,
        CancellationToken cancellationToken);
}
