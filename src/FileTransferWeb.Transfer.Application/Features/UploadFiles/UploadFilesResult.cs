using FileTransferWeb.Transfer.Domain;

namespace FileTransferWeb.Transfer.Application.Features.UploadFiles;

public sealed record SavedUploadFileItem(
    string OriginalFileName,
    string StoredFileName,
    string RelativePath,
    long SizeBytes);

public sealed record FailedUploadFileItem(
    string OriginalFileName,
    string Reason,
    long SizeBytes);

public sealed record UploadSummary(
    int TotalCount,
    int SuccessCount,
    int FailedCount);

public sealed record UploadFilesResult(
    Guid JobId,
    string TargetPath,
    UploadStatus Status,
    IReadOnlyList<SavedUploadFileItem> SavedFiles,
    IReadOnlyList<FailedUploadFileItem> FailedFiles,
    UploadSummary Summary)
{
    public static UploadFilesResult FromDomain(UploadJob uploadJob)
    {
        var savedFiles = uploadJob.FileResults
            .Where(result => result.IsSuccess)
            .Select(result => new SavedUploadFileItem(
                result.OriginalFileName,
                result.StoredFileName!,
                result.RelativePath!,
                result.SizeBytes))
            .ToArray();

        var failedFiles = uploadJob.FileResults
            .Where(result => !result.IsSuccess)
            .Select(result => new FailedUploadFileItem(
                result.OriginalFileName,
                result.FailureReason!,
                result.SizeBytes))
            .ToArray();

        var summary = new UploadSummary(
            uploadJob.FileResults.Count,
            savedFiles.Length,
            failedFiles.Length);

        return new UploadFilesResult(
            uploadJob.Id,
            uploadJob.TargetPath,
            uploadJob.Status,
            savedFiles,
            failedFiles,
            summary);
    }
}
