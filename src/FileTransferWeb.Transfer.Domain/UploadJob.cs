using FileTransferWeb.Transfer.Domain.Exceptions;

namespace FileTransferWeb.Transfer.Domain;

public sealed class UploadJob
{
    private readonly List<UploadFileResult> _fileResults = [];

    private UploadJob(Guid id, string targetPath)
    {
        Id = id;
        TargetPath = targetPath;
        Status = UploadStatus.Pending;
    }

    public Guid Id { get; }

    public string TargetPath { get; }

    public UploadStatus Status { get; private set; }

    public IReadOnlyList<UploadFileResult> FileResults => _fileResults;

    public static UploadJob Create(string targetPath, IReadOnlyList<string> fileNames)
    {
        if (fileNames is null)
        {
            throw new TransferDomainException("업로드 파일 목록이 비어 있습니다.");
        }

        if (fileNames.Count == 0)
        {
            throw new TransferDomainException("업로드할 파일이 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new TransferDomainException("업로드 대상 경로가 비어 있습니다.");
        }

        foreach (var fileName in fileNames)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new TransferDomainException("업로드 파일 이름이 비어 있습니다.");
            }
        }

        return new UploadJob(Guid.NewGuid(), targetPath.Trim());
    }

    public void Complete(IReadOnlyList<UploadFileResult> fileResults)
    {
        if (fileResults is null)
        {
            throw new TransferDomainException("파일 처리 결과가 비어 있습니다.");
        }

        if (fileResults.Count == 0)
        {
            throw new TransferDomainException("파일 처리 결과가 비어 있습니다.");
        }

        _fileResults.Clear();
        _fileResults.AddRange(fileResults);

        var successCount = _fileResults.Count(x => x.IsSuccess);
        if (successCount == _fileResults.Count)
        {
            Status = UploadStatus.Completed;
            return;
        }

        if (successCount == 0)
        {
            Status = UploadStatus.Failed;
            return;
        }

        Status = UploadStatus.PartiallyCompleted;
    }
}
