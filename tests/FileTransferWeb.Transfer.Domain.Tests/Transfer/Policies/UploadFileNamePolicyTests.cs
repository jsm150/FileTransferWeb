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

        var plan = policy.Reserve("report.txt", 10, CreateStreamFactory("a"));

        Assert.Equal("report (2).txt", plan.StoredFileName);
    }

    [Fact(DisplayName = "같은 요청 내 중복 파일 이름은 연속 번호를 부여한다")]
    public void Reserve_AssignsSequentialNumbers_ForSameRequestDuplicates()
    {
        var policy = new UploadFileNamePolicy(["report.txt"]);

        var first = policy.Reserve("report.txt", 10, CreateStreamFactory("a"));
        var second = policy.Reserve("report.txt", 10, CreateStreamFactory("b"));
        var third = policy.Reserve("report.txt", 10, CreateStreamFactory("c"));

        Assert.Equal("report (1).txt", first.StoredFileName);
        Assert.Equal("report (2).txt", second.StoredFileName);
        Assert.Equal("report (3).txt", third.StoredFileName);
    }

    [Fact(DisplayName = "경로가 포함된 파일 이름은 파일명만 추출해 정규화한다")]
    public void Reserve_NormalizesPathIncludedFileName()
    {
        var policy = new UploadFileNamePolicy([]);

        var plan = policy.Reserve("../folder/photo.jpg", 10, CreateStreamFactory("a"));

        Assert.Equal("photo.jpg", plan.OriginalFileName);
        Assert.Equal("photo.jpg", plan.StoredFileName);
    }

    [Fact(DisplayName = "확장자가 없는 이름도 번호 규칙을 적용한다")]
    public void Reserve_HandlesFileNameWithoutExtension()
    {
        var policy = new UploadFileNamePolicy(["report", "report (1)"]);

        var plan = policy.Reserve("report", 10, CreateStreamFactory("a"));

        Assert.Equal("report (2)", plan.StoredFileName);
    }

    [Fact(DisplayName = "빈 파일 이름은 기본 이름으로 대체한 뒤 번호를 부여한다")]
    public void Reserve_UsesDefaultName_WhenFileNameIsBlank()
    {
        var policy = new UploadFileNamePolicy(["이름없는파일", "이름없는파일 (1)"]);

        var plan = policy.Reserve("   ", 10, CreateStreamFactory("a"));

        Assert.Equal("이름없는파일", plan.OriginalFileName);
        Assert.Equal("이름없는파일 (2)", plan.StoredFileName);
    }

    private static Func<Stream> CreateStreamFactory(string content)
    {
        return () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    }
}
