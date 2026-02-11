namespace FileTransferWeb.Transfer.Domain.Ports;

public interface ITransferTargetFileNameReader
{
    ValueTask<IReadOnlyList<string>> GetExistingFileNamesAsync(
        string targetRelativePath,
        CancellationToken cancellationToken);
}
