using FileTransferWeb.Transfer.Domain.Exceptions;

namespace FileTransferWeb.Transfer.Domain.Models;

public sealed class UploadFilePlanItem
{
    public UploadFilePlanItem(
        string originalFileName,
        string storedFileName,
        long sizeBytes,
        Func<Stream> openReadStream)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new TransferDomainException("원본 파일 이름이 비어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            throw new TransferDomainException("저장 파일 이름이 비어 있습니다.");
        }

        if (sizeBytes < 0)
        {
            throw new TransferDomainException("파일 크기는 0 이상이어야 합니다.");
        }

        OriginalFileName = originalFileName.Trim();
        StoredFileName = storedFileName.Trim();
        SizeBytes = sizeBytes;
        OpenReadStream = openReadStream ?? throw new TransferDomainException("파일 읽기 함수가 비어 있습니다.");
    }

    public string OriginalFileName { get; }

    public string StoredFileName { get; }

    public long SizeBytes { get; }

    public Func<Stream> OpenReadStream { get; }
}
