using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Policies;
using Xunit;

namespace FileTransferWeb.Transfer.Domain.Tests.Transfer.Models;

public class TransferBatchTests
{
    [Fact(DisplayName = "예상 파일 수가 0 이하이면 배치를 생성할 수 없다")]
    public void Create_Throws_WhenExpectedFileCountIsInvalid()
    {
        Assert.Throws<TransferDomainException>(
            () => TransferBatch.Create("images", 0, DateTime.UtcNow));
    }

    [Fact(DisplayName = "중복된 uploadId는 배치에 등록할 수 없다")]
    public void RegisterCompletedUpload_Throws_WhenUploadIdDuplicated()
    {
        var batch = TransferBatch.Create("images", 3, DateTime.UtcNow);
        var upload = TransferBatchUpload.Create("u-1", "photo.jpg", 10, "image/jpeg", DateTime.UtcNow);
        batch.RegisterCompletedUpload(upload);

        Assert.Throws<TransferDomainException>(() => batch.RegisterCompletedUpload(upload));
    }

    [Fact(DisplayName = "배치 계획 생성 시 기존 파일명을 기준으로 최소 빈 번호를 선택한다")]
    public void BuildFinalizePlan_AssignsSmallestMissingNumber()
    {
        var batch = TransferBatch.Create("images", 3, DateTime.UtcNow);
        batch.RegisterCompletedUpload(
            TransferBatchUpload.Create("u-1", "report.txt", 10, "text/plain", DateTime.UtcNow));
        batch.RegisterCompletedUpload(
            TransferBatchUpload.Create("u-2", "report.txt", 20, "text/plain", DateTime.UtcNow));


        var plan = batch.BuildFinalizePlan(new UploadFileNamePolicy(["report.txt", "report (1).txt", "report (3).txt"]));

        Assert.Equal(2, plan.Count);
        Assert.Equal("report (2).txt", plan[0].StoredFileName);
        Assert.Equal("report (4).txt", plan[1].StoredFileName);
        Assert.Equal("images/report (2).txt", plan[0].RelativePath);
    }

    [Fact(DisplayName = "모든 파일이 오지 않아도 배치를 마무리할 수 있고 상태는 부분 완료가 된다")]
    public void MarkFinalized_AllowsFinalizeBeforeExpectedCount_AndSetsPartiallyCompleted()
    {
        var batch = TransferBatch.Create("images", 3, DateTime.UtcNow);
        batch.RegisterCompletedUpload(
            TransferBatchUpload.Create("u-1", "photo.jpg", 100, "image/jpeg", DateTime.UtcNow));

        batch.MarkFinalized(
        [
            UploadFileResult.Succeeded("photo.jpg", "photo.jpg", "images/photo.jpg", 100)
        ], DateTime.UtcNow);

        Assert.Equal(TransferBatchStatus.PartiallyCompleted, batch.Status);
        Assert.NotNull(batch.FinalizedAtUtc);
    }

    [Fact(DisplayName = "성공 결과가 하나도 없으면 실패 상태가 된다")]
    public void MarkFinalized_SetsFailed_WhenAllFailed()
    {
        var batch = TransferBatch.Create("images", 1, DateTime.UtcNow);
        batch.RegisterCompletedUpload(
            TransferBatchUpload.Create("u-1", "photo.jpg", 100, "image/jpeg", DateTime.UtcNow));

        batch.MarkFinalized(
        [
            UploadFileResult.Failed("photo.jpg", "저장 실패", 100)
        ], DateTime.UtcNow);

        Assert.Equal(TransferBatchStatus.Failed, batch.Status);
    }
}
