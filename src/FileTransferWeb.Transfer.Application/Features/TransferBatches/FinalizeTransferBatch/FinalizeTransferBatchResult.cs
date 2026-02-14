using FileTransferWeb.Transfer.Domain.Models;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.FinalizeTransferBatch;

public sealed record FinalizeTransferBatchResult(
    Guid BatchId,
    string TargetPath,
    int ExpectedFileCount,
    int CompletedUploadCount,
    TransferBatchStatus Status,
    IReadOnlyList<FinalizeTransferBatchFileResult> Files);

public sealed record FinalizeTransferBatchFileResult(
    string OriginalFileName,
    string? StoredFileName,
    string? RelativePath,
    long SizeBytes,
    string? FailureReason);
