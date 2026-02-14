using FileTransferWeb.Transfer.Domain.Exceptions;

namespace FileTransferWeb.Transfer.Domain.Models;

public sealed class TransferBatchFinalizePlanItem
{
    private TransferBatchFinalizePlanItem(
        string uploadId,
        string originalFileName,
        string storedFileName,
        string relativePath,
        long sizeBytes,
        string? contentType)
    {
        UploadId = uploadId;
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        RelativePath = relativePath;
        SizeBytes = sizeBytes;
        ContentType = contentType;
    }

    public string UploadId { get; }

    public string OriginalFileName { get; }

    public string StoredFileName { get; }

    public string RelativePath { get; }

    public long SizeBytes { get; }

    public string? ContentType { get; }

    public static TransferBatchFinalizePlanItem Create(
        string uploadId,
        string originalFileName,
        string storedFileName,
        string relativePath,
        long sizeBytes,
        string? contentType)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new TransferDomainException("업로드 식별자가 비어 있습니다.");
        }

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

        return new TransferBatchFinalizePlanItem(
            uploadId.Trim(),
            originalFileName.Trim(),
            storedFileName.Trim(),
            relativePath.Trim(),
            sizeBytes,
            string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim());
    }
}
