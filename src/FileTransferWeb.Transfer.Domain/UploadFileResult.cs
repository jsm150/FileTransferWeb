using FileTransferWeb.Transfer.Domain.Exceptions;

namespace FileTransferWeb.Transfer.Domain;

public sealed class UploadFileResult
{
    private UploadFileResult(
        string originalFileName,
        string? storedFileName,
        string? relativePath,
        long sizeBytes,
        string? failureReason)
    {
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        RelativePath = relativePath;
        SizeBytes = sizeBytes;
        FailureReason = failureReason;
    }

    public string OriginalFileName { get; }

    public string? StoredFileName { get; }

    public string? RelativePath { get; }

    public long SizeBytes { get; }

    public string? FailureReason { get; }

    public bool IsSuccess => string.IsNullOrEmpty(FailureReason);

    public static UploadFileResult Succeeded(
        string originalFileName,
        string storedFileName,
        string relativePath,
        long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new TransferDomainException("원본 파일 이름이 비어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            throw new TransferDomainException("저장 파일 이름이 비어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new TransferDomainException("저장 상대 경로가 비어 있습니다.");
        }

        if (sizeBytes < 0)
        {
            throw new TransferDomainException("파일 크기는 0 이상이어야 합니다.");
        }

        return new UploadFileResult(
            originalFileName.Trim(),
            storedFileName.Trim(),
            relativePath.Trim(),
            sizeBytes,
            null);
    }

    public static UploadFileResult Failed(
        string originalFileName,
        string failureReason,
        long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new TransferDomainException("원본 파일 이름이 비어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new TransferDomainException("실패 사유가 비어 있습니다.");
        }

        if (sizeBytes < 0)
        {
            throw new TransferDomainException("파일 크기는 0 이상이어야 합니다.");
        }

        return new UploadFileResult(
            originalFileName.Trim(),
            null,
            null,
            sizeBytes,
            failureReason.Trim());
    }
}
