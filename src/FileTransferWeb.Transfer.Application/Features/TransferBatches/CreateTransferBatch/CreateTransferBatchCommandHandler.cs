using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.CreateTransferBatch;

public sealed class CreateTransferBatchCommandHandler(
    ITransferBatchRepository transferBatchRepository)
    : IRequestHandler<CreateTransferBatchCommand, CreateTransferBatchResult>
{
    private readonly ITransferBatchRepository _transferBatchRepository = transferBatchRepository;

    public async Task<CreateTransferBatchResult> Handle(
        CreateTransferBatchCommand request,
        CancellationToken cancellationToken)
    {
        var batch = TransferBatch.Create(
            request.TargetPath,
            request.ExpectedFileCount,
            DateTime.UtcNow);

        await _transferBatchRepository.SaveAsync(batch, cancellationToken);

        return new CreateTransferBatchResult(
            batch.Id,
            batch.TargetPath,
            batch.ExpectedFileCount);
    }
}
