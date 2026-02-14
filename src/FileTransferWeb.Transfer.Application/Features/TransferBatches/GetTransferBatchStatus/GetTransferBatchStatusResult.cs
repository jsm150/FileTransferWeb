using FileTransferWeb.Transfer.Domain.Models;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.GetTransferBatchStatus;

public sealed record GetTransferBatchStatusResult(
    Guid BatchId,
    string TargetPath,
    int ExpectedFileCount,
    int CompletedUploadCount,
    TransferBatchStatus Status,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc);
