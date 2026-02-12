using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.CompleteTusUpload;

public sealed class CompleteTusUploadCommandHandler(
    ITransferCompletedUploadReader completedUploadReader,
    ITransferTargetFileNameReader targetFileNameReader,
    ITransferCompletedFileMover completedFileMover)
    : IRequestHandler<CompleteTusUploadCommand>
{
    private readonly ITransferCompletedUploadReader _completedUploadReader = completedUploadReader;
    private readonly ITransferTargetFileNameReader _targetFileNameReader = targetFileNameReader;
    private readonly ITransferCompletedFileMover _completedFileMover = completedFileMover;

    public async Task Handle(CompleteTusUploadCommand request, CancellationToken cancellationToken)
    {
        var completedUpload = await _completedUploadReader.ReadAsync(request.UploadId, cancellationToken);
        var uploadJob = UploadJob.Create(completedUpload.TargetPath, [completedUpload.OriginalFileName]);
        var existingFileNames = await _targetFileNameReader.GetExistingFileNamesAsync(
            uploadJob.TargetPath,
            cancellationToken);

        var fileNamePolicy = new UploadFileNamePolicy(existingFileNames);
        var storedFileName = fileNamePolicy.ReserveStoredFileName(completedUpload.OriginalFileName);

        string relativePath;
        try
        {
            relativePath = await _completedFileMover.MoveCompletedFileAsync(
                completedUpload.UploadId,
                uploadJob.TargetPath,
                storedFileName,
                cancellationToken);
        }
        catch (Exception ex) when (ex is IOException
                                   or UnauthorizedAccessException
                                   or ArgumentException
                                   or NotSupportedException
                                   or PathTooLongException
                                   or InvalidOperationException)
        {
            uploadJob.Complete(
            [
                UploadFileResult.Failed(
                    completedUpload.OriginalFileName,
                    "파일 저장에 실패했습니다.",
                    completedUpload.FileSizeBytes)
            ]);

            throw;
        }

        uploadJob.Complete(
        [
            UploadFileResult.Succeeded(
                completedUpload.OriginalFileName,
                storedFileName,
                relativePath,
                completedUpload.FileSizeBytes)
        ]);
    }
}
