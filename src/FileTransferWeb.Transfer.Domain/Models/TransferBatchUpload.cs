using FileTransferWeb.Transfer.Domain.Exceptions;

namespace FileTransferWeb.Transfer.Domain.Models;

public sealed class TransferBatchUpload
{
    private TransferBatchUpload(
        string uploadId,
        string originalFileName,
        long sizeBytes,
        string? contentType,
        DateTime completedAtUtc)
    {
        UploadId = uploadId;
        OriginalFileName = originalFileName;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        CompletedAtUtc = completedAtUtc;
    }

    public string UploadId { get; }

    public string OriginalFileName { get; }

    public long SizeBytes { get; }

    public string? ContentType { get; }

    public DateTime CompletedAtUtc { get; }

    public static TransferBatchUpload Create(
        string uploadId,
        string originalFileName,
        long sizeBytes,
        string? contentType,
        DateTime completedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new TransferDomainException("업로드 식별자가 비어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new TransferDomainException("원본 파일 이름이 비어 있습니다.");
        }

        if (sizeBytes < 0)
        {
            throw new TransferDomainException("파일 크기는 0 이상이어야 합니다.");
        }

        return new TransferBatchUpload(
            uploadId.Trim(),
            originalFileName.Trim(),
            sizeBytes,
            string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim(),
            completedAtUtc);
    }
}
