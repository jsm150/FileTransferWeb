using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.RegisterCompletedTusUpload;

public sealed class RegisterCompletedTusUploadCommandHandler(
    ITransferBatchRepository transferBatchRepository,
    ITransferCompletedUploadReader completedUploadReader)
    : IRequestHandler<RegisterCompletedTusUploadCommand, Unit>
{
    private readonly ITransferBatchRepository _transferBatchRepository = transferBatchRepository;
    private readonly ITransferCompletedUploadReader _completedUploadReader = completedUploadReader;

    public async Task<Unit> Handle(
        RegisterCompletedTusUploadCommand request,
        CancellationToken cancellationToken)
    {
        var completedUpload = await _completedUploadReader.ReadAsync(request.UploadId, cancellationToken);
        var batch = await _transferBatchRepository.GetAsync(completedUpload.BatchId, cancellationToken)
                    ?? throw new TransferDomainException("업로드 배치를 찾을 수 없습니다.");

        var batchUpload = TransferBatchUpload.Create(
            completedUpload.UploadId,
            completedUpload.OriginalFileName,
            completedUpload.FileSizeBytes,
            completedUpload.ContentType,
            DateTime.UtcNow);

        batch.RegisterCompletedUpload(batchUpload);
        await _transferBatchRepository.SaveAsync(batch, cancellationToken);

        return Unit.Value;
    }
}
