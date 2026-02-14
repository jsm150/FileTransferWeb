namespace FileTransferWeb.Transfer.Domain.Ports;

public sealed record TransferCompletedUploadInfo(
    string UploadId,
    Guid BatchId,
    string TargetPath,
    string OriginalFileName,
    long FileSizeBytes,
    string? ContentType);
