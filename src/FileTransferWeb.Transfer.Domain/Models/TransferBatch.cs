using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Policies;

namespace FileTransferWeb.Transfer.Domain.Models;

public sealed class TransferBatch
{
    private readonly List<TransferBatchUpload> _completedUploads = [];
    private readonly List<UploadFileResult> _finalizeResults = [];

    private TransferBatch(Guid id, string targetPath, int expectedFileCount, DateTime createdAtUtc)
    {
        Id = id;
        TargetPath = targetPath;
        ExpectedFileCount = expectedFileCount;
        CreatedAtUtc = createdAtUtc;
        Status = TransferBatchStatus.Collecting;
    }

    public Guid Id { get; }

    public string TargetPath { get; }

    public int ExpectedFileCount { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? FinalizedAtUtc { get; private set; }

    public TransferBatchStatus Status { get; private set; }

    public IReadOnlyList<TransferBatchUpload> CompletedUploads => _completedUploads;

    public IReadOnlyList<UploadFileResult> FinalizeResults => _finalizeResults;

    public static TransferBatch Create(
        string targetPath,
        int expectedFileCount,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new TransferDomainException("업로드 대상 경로가 비어 있습니다.");
        }

        if (expectedFileCount <= 0)
        {
            throw new TransferDomainException("예상 파일 수는 1 이상이어야 합니다.");
        }

        return new TransferBatch(Guid.NewGuid(), targetPath.Trim(), expectedFileCount, createdAtUtc);
    }

    public void RegisterCompletedUpload(TransferBatchUpload completedUpload)
    {
        if (completedUpload is null)
        {
            throw new TransferDomainException("완료된 업로드 정보가 비어 있습니다.");
        }

        EnsureCollectingStatus();

        if (_completedUploads.Any(x => string.Equals(x.UploadId, completedUpload.UploadId, StringComparison.Ordinal)))
        {
            throw new TransferDomainException("이미 등록된 업로드 식별자입니다.");
        }

        _completedUploads.Add(completedUpload);
    }

    public IReadOnlyList<TransferBatchFinalizePlanItem> BuildFinalizePlan(UploadFileNamePolicy fileNamePolicy)
    {
        EnsureCollectingStatus();

        if (_completedUploads.Count == 0)
        {
            throw new TransferDomainException("완료된 업로드가 없어 배치를 마무리할 수 없습니다.");
        }

        var planItems = new List<TransferBatchFinalizePlanItem>(_completedUploads.Count);

        foreach (var completedUpload in _completedUploads)
        {
            var storedFileName = fileNamePolicy.ReserveStoredFileName(completedUpload.OriginalFileName);
            var relativePath = BuildRelativePath(TargetPath, storedFileName);

            planItems.Add(
                TransferBatchFinalizePlanItem.Create(
                    completedUpload.UploadId,
                    completedUpload.OriginalFileName,
                    storedFileName,
                    relativePath,
                    completedUpload.SizeBytes,
                    completedUpload.ContentType));
        }

        return planItems;
    }

    public void MarkFinalized(IReadOnlyList<UploadFileResult> fileResults, DateTime finalizedAtUtc)
    {
        EnsureCollectingStatus();

        if (fileResults is null || fileResults.Count == 0)
        {
            throw new TransferDomainException("배치 마무리 결과가 비어 있습니다.");
        }

        if (fileResults.Count > _completedUploads.Count)
        {
            throw new TransferDomainException("배치 마무리 결과 수가 완료된 업로드 수보다 많습니다.");
        }

        _finalizeResults.Clear();
        _finalizeResults.AddRange(fileResults);

        var successCount = _finalizeResults.Count(x => x.IsSuccess);
        if (successCount == 0)
        {
            Status = TransferBatchStatus.Failed;
        }
        else if (successCount == _finalizeResults.Count && _finalizeResults.Count == ExpectedFileCount)
        {
            Status = TransferBatchStatus.Completed;
        }
        else
        {
            Status = TransferBatchStatus.PartiallyCompleted;
        }

        FinalizedAtUtc = finalizedAtUtc;
    }

    private void EnsureCollectingStatus()
    {
        if (Status != TransferBatchStatus.Collecting)
        {
            throw new TransferDomainException("수집 중인 배치에서만 업로드 등록 또는 마무리가 가능합니다.");
        }
    }

    private static string BuildRelativePath(string targetPath, string storedFileName)
    {
        var normalizedPath = targetPath.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return storedFileName;
        }

        return $"{normalizedPath}/{storedFileName}";
    }
}
