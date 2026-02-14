namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.RegisterCompletedTusUpload;

public sealed record RegisterCompletedTusUploadResult(
    Guid BatchId,
    int ExpectedFileCount,
    int CompletedUploadCount);
