using FileTransferWeb.Transfer.Domain.Models;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.GetTransferBatchStatus;

public sealed record GetTransferBatchStatusResult(
    Guid BatchId,
    string TargetPath,
    int ExpectedFileCount,
    int CompletedUploadCount,
    TransferBatchStatus Status,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc,
    IReadOnlyList<GetTransferBatchStatusFileResult> Files);

public sealed record GetTransferBatchStatusFileResult(
    string OriginalFileName,
    string? StoredFileName,
    string? RelativePath,
    long SizeBytes,
    string? FailureReason);
