using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.GetTransferBatchStatus;

public sealed class GetTransferBatchStatusQueryHandler(
    ITransferBatchRepository transferBatchRepository)
    : IRequestHandler<GetTransferBatchStatusQuery, GetTransferBatchStatusResult>
{
    private readonly ITransferBatchRepository _transferBatchRepository = transferBatchRepository;

    public async Task<GetTransferBatchStatusResult> Handle(
        GetTransferBatchStatusQuery request,
        CancellationToken cancellationToken)
    {
        var batch = await _transferBatchRepository.GetAsync(request.BatchId, cancellationToken)
                    ?? throw new TransferDomainException("업로드 배치를 찾을 수 없습니다.");

        return new GetTransferBatchStatusResult(
            batch.Id,
            batch.TargetPath,
            batch.ExpectedFileCount,
            batch.CompletedUploads.Count,
            batch.Status,
            batch.CreatedAtUtc,
            batch.FinalizedAtUtc,
            batch.FinalizeResults.Select(
                x => new GetTransferBatchStatusFileResult(
                    x.OriginalFileName,
                    x.StoredFileName,
                    x.RelativePath,
                    x.SizeBytes,
                    x.FailureReason)).ToArray());
    }
}
