using FileTransferWeb.Domain.Shared;
using FileTransferWeb.Storage.Domain.Models;
using FileTransferWeb.Storage.Domain.Policies;
using Xunit;

namespace FileTransferWeb.Storage.Domain.Tests.Storage.Models;

public class DirectoryListingTests
{
    [Fact(DisplayName = "생성자는 정책을 반드시 받아야 한다")]
    public void Constructor_Throws_WhenPolicyIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new DirectoryListing(null!, ["photos"]));

        Assert.Equal("policy", exception.ParamName);
    }

    [Fact(DisplayName = "생성자는 디렉터리 이름을 대소문자 무시 오름차순으로 정렬한다")]
    public void Constructor_SortsDirectories_CaseInsensitiveAscending()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var policy = new StoragePathPolicy(rootPath, "images/2026");
        var listing = new DirectoryListing(policy, ["zeta", "Alpha"]);

        Assert.Equal(["Alpha", "zeta"], listing.Directories.Select(x => x.Name));
        Assert.Equal(["images/2026/Alpha", "images/2026/zeta"], listing.Directories.Select(x => x.RelativePath));
    }

    [Fact(DisplayName = "루트 경로 조회 시 부모 경로는 null이다")]
    public void Constructor_SetsRootPaths_WhenPolicyPointsToRoot()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var policy = new StoragePathPolicy(rootPath, "");
        var listing = new DirectoryListing(policy, ["docs"]);

        Assert.Equal(string.Empty, listing.CurrentPath);
        Assert.Null(listing.ParentPath);
        Assert.Single(listing.Directories);
        Assert.Equal("docs", listing.Directories[0].RelativePath);
    }

    [Fact(DisplayName = "디렉터리 이름이 비어 있으면 도메인 예외를 발생시킨다")]
    public void Constructor_Throws_WhenDirectoryNameIsEmpty()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var policy = new StoragePathPolicy(rootPath, "");

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => new DirectoryListing(policy, ["ok", " "]));

        Assert.Contains("디렉터리 이름이 비어 있습니다.", exception.Message);
    }
}
