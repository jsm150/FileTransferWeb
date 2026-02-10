using FileTransferWeb.Domain.Shared;

namespace FileTransferWeb.Transfer.Domain;

public sealed class UploadJob
{
    private UploadJob(Guid id, string fileName, string targetPath)
    {
        Id = id;
        FileName = fileName;
        TargetPath = targetPath;
        Status = UploadStatus.Pending;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string TargetPath { get; }

    public UploadStatus Status { get; private set; }

    public static UploadJob Create(string fileName, string targetPath)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainRuleViolationException("업로드 파일 이름이 비어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new DomainRuleViolationException("업로드 대상 경로가 비어 있습니다.");
        }

        return new UploadJob(Guid.NewGuid(), fileName, targetPath);
    }

    public void MarkCompleted() => Status = UploadStatus.Completed;

    public void MarkFailed() => Status = UploadStatus.Failed;
}
