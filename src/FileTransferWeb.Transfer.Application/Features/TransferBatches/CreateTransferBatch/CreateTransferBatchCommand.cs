using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.CreateTransferBatch;

public sealed record CreateTransferBatchCommand(
    string TargetPath,
    int ExpectedFileCount) : IRequest<CreateTransferBatchResult>;
