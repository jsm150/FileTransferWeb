using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Models;

namespace FileTransferWeb.Transfer.Domain.Ports;

public interface ITransferFileStore
{
    ValueTask<IReadOnlyList<UploadFileResult>> SaveFilesAsync(
        string targetRelativePath,
        IReadOnlyList<UploadFilePlanItem> plannedFiles,
        CancellationToken cancellationToken);
}
