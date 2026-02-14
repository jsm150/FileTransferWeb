namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.CreateTransferBatch;

public sealed record CreateTransferBatchResult(
    Guid BatchId,
    string TargetPath,
    int ExpectedFileCount);
