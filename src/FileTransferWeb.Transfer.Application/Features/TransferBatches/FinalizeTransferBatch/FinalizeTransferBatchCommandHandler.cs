using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.FinalizeTransferBatch;

public sealed class FinalizeTransferBatchCommandHandler(
    ITransferBatchRepository transferBatchRepository,
    ITransferTargetFileNameReader targetFileNameReader,
    ITransferCompletedFileMover completedFileMover)
    : IRequestHandler<FinalizeTransferBatchCommand, FinalizeTransferBatchResult>
{
    private readonly ITransferBatchRepository _transferBatchRepository = transferBatchRepository;
    private readonly ITransferTargetFileNameReader _targetFileNameReader = targetFileNameReader;
    private readonly ITransferCompletedFileMover _completedFileMover = completedFileMover;

    public async Task<FinalizeTransferBatchResult> Handle(
        FinalizeTransferBatchCommand request,
        CancellationToken cancellationToken)
    {
        var batch = await _transferBatchRepository.GetAsync(request.BatchId, cancellationToken)
                    ?? throw new TransferDomainException("업로드 배치를 찾을 수 없습니다.");

        var existingFileNames = await _targetFileNameReader.GetExistingFileNamesAsync(
            batch.TargetPath,
            cancellationToken);
        var plan = batch.BuildFinalizePlan(existingFileNames);

        var fileResults = new List<UploadFileResult>(plan.Count);
        foreach (var item in plan)
        {
            try
            {
                var relativePath = await _completedFileMover.MoveCompletedFileAsync(
                    item.UploadId,
                    batch.TargetPath,
                    item.StoredFileName,
                    cancellationToken);

                fileResults.Add(
                    UploadFileResult.Succeeded(
                        item.OriginalFileName,
                        item.StoredFileName,
                        relativePath,
                        item.SizeBytes));
            }
            catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or NotSupportedException
                                       or PathTooLongException
                                       or InvalidOperationException)
            {
                fileResults.Add(
                    UploadFileResult.Failed(
                        item.OriginalFileName,
                        "파일 저장에 실패했습니다.",
                        item.SizeBytes));
            }
        }

        batch.MarkFinalized(fileResults, DateTime.UtcNow);
        await _transferBatchRepository.SaveAsync(batch, cancellationToken);

        return new FinalizeTransferBatchResult(
            batch.Id,
            batch.TargetPath,
            batch.ExpectedFileCount,
            batch.CompletedUploads.Count,
            batch.Status,
            batch.FinalizeResults.Select(
                x => new FinalizeTransferBatchFileResult(
                    x.OriginalFileName,
                    x.StoredFileName,
                    x.RelativePath,
                    x.SizeBytes,
                    x.FailureReason)).ToArray());
    }
}
