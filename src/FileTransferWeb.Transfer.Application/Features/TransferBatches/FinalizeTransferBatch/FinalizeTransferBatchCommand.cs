using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.FinalizeTransferBatch;

public sealed record FinalizeTransferBatchCommand(Guid BatchId) : IRequest<FinalizeTransferBatchResult>;
