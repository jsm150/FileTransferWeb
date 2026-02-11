using FileTransferWeb.Transfer.Domain.Policies;
using Xunit;

namespace FileTransferWeb.Transfer.Domain.Tests.Transfer.Policies;

public class UploadFileNamePolicyTests
{
    [Fact(DisplayName = "기존 이름 목록을 단일 스캔해 비어있는 최소 번호를 선택한다")]
    public void Reserve_SelectsSmallestMissingNumber_FromExistingNames()
    {
        var policy = new UploadFileNamePolicy(
            ["report.txt", "report (1).txt", "report (3).txt"]);

        var storedFileName = policy.ReserveStoredFileName("report.txt");

        Assert.Equal("report (2).txt", storedFileName);
    }

    [Fact(DisplayName = "같은 요청 내 중복 파일 이름은 연속 번호를 부여한다")]
    public void Reserve_AssignsSequentialNumbers_ForSameRequestDuplicates()
    {
        var policy = new UploadFileNamePolicy(["report.txt"]);

        var first = policy.ReserveStoredFileName("report.txt");
        var second = policy.ReserveStoredFileName("report.txt");
        var third = policy.ReserveStoredFileName("report.txt");

        Assert.Equal("report (1).txt", first);
        Assert.Equal("report (2).txt", second);
        Assert.Equal("report (3).txt", third);
    }

    [Fact(DisplayName = "경로가 포함된 파일 이름은 파일명만 추출해 정규화한다")]
    public void Reserve_NormalizesPathIncludedFileName()
    {
        var policy = new UploadFileNamePolicy([]);

        var storedFileName = policy.ReserveStoredFileName("../folder/photo.jpg");

        Assert.Equal("photo.jpg", storedFileName);
    }

    [Fact(DisplayName = "확장자가 없는 이름도 번호 규칙을 적용한다")]
    public void Reserve_HandlesFileNameWithoutExtension()
    {
        var policy = new UploadFileNamePolicy(["report", "report (1)"]);

        var storedFileName = policy.ReserveStoredFileName("report");

        Assert.Equal("report (2)", storedFileName);
    }

    [Fact(DisplayName = "빈 파일 이름은 기본 이름으로 대체한 뒤 번호를 부여한다")]
    public void Reserve_UsesDefaultName_WhenFileNameIsBlank()
    {
        var policy = new UploadFileNamePolicy(["이름없는파일", "이름없는파일 (1)"]);

        var storedFileName = policy.ReserveStoredFileName("   ");
        Assert.Equal("이름없는파일 (2)", storedFileName);
    }
}
