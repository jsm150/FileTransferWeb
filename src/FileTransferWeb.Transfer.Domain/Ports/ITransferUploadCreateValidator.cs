namespace FileTransferWeb.Transfer.Domain.Ports;

public interface ITransferUploadCreateValidator
{
    ValueTask ValidateAsync(
        string targetPath,
        string fileName,
        CancellationToken cancellationToken);
}
