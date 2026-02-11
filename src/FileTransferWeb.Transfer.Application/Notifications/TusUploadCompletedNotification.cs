using MediatR;

namespace FileTransferWeb.Transfer.Application.Notifications;

public sealed record TusUploadCompletedNotification(
    string UploadId,
    string TargetPath,
    string OriginalFileName,
    string TempFilePath,
    long FileSizeBytes,
    string? ContentType) : INotification;
