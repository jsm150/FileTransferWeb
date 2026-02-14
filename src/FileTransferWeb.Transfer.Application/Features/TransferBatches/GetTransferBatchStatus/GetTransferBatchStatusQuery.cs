using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.GetTransferBatchStatus;

public sealed record GetTransferBatchStatusQuery(Guid BatchId) : IRequest<GetTransferBatchStatusResult>;
