using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Notifications;

public sealed class TusUploadCompletedNotificationHandler(
    ITransferTargetFileNameReader targetFileNameReader,
    ITransferCompletedFileMover completedFileMover)
    : INotificationHandler<TusUploadCompletedNotification>
{
    private readonly ITransferTargetFileNameReader _targetFileNameReader = targetFileNameReader;
    private readonly ITransferCompletedFileMover _completedFileMover = completedFileMover;

    public async Task Handle(TusUploadCompletedNotification notification, CancellationToken cancellationToken)
    {
        var uploadJob = UploadJob.Create(notification.TargetPath, [notification.OriginalFileName]);
        var existingFileNames = await _targetFileNameReader.GetExistingFileNamesAsync(
            uploadJob.TargetPath,
            cancellationToken);

        var fileNamePolicy = new UploadFileNamePolicy(existingFileNames);
        var storedFileName = fileNamePolicy.ReserveStoredFileName(notification.OriginalFileName);

        string relativePath;
        try
        {
            relativePath = await _completedFileMover.MoveCompletedFileAsync(
                notification.UploadId,
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
                    notification.OriginalFileName,
                    "파일 저장에 실패했습니다.",
                    notification.FileSizeBytes)
            ]);

            throw;
        }

        uploadJob.Complete(
        [
            UploadFileResult.Succeeded(
                notification.OriginalFileName,
                storedFileName,
                relativePath,
                notification.FileSizeBytes)
        ]);
    }
}
