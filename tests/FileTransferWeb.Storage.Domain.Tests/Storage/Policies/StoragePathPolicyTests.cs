using FileTransferWeb.Domain.Shared;
using FileTransferWeb.Storage.Domain.Policies;
using Xunit;

namespace FileTransferWeb.Storage.Domain.Tests.Storage.Policies;

public class StoragePathPolicyTests
{
    [Fact(DisplayName = "상대 경로가 비어 있으면 현재 경로는 루트가 된다")]
    public void Constructor_SetsRootState_WhenRelativePathIsEmpty()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");

        var policy = new StoragePathPolicy(rootPath, "");

        Assert.Equal(Path.GetFullPath(rootPath), policy.FullCurrentPath);
        Assert.Equal(string.Empty, policy.CurrentRelativePath);
        Assert.Null(policy.ParentRelativePath);
    }

    [Fact(DisplayName = "정상 상대 경로면 현재/부모 상대 경로를 계산한다")]
    public void Constructor_CalculatesRelativePaths_WhenRelativePathIsValid()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var relativePath = "images/2026";

        var policy = new StoragePathPolicy(rootPath, relativePath);

        var expected = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        Assert.Equal(expected, policy.FullCurrentPath);
        Assert.Equal("images/2026", policy.CurrentRelativePath);
        Assert.Equal("images", policy.ParentRelativePath);
    }

    [Fact(DisplayName = "상대 경로가 루트 밖으로 벗어나면 예외를 발생시킨다")]
    public void Constructor_Throws_WhenPathTraversalIsDetected()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var escapingRelativePath = Path.Combine("..", "outside");

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => new StoragePathPolicy(rootPath, escapingRelativePath));

        Assert.Contains("루트 경로 밖", exception.Message);
    }

    [Fact(DisplayName = "절대 경로 입력은 허용하지 않는다")]
    public void Constructor_Throws_WhenPathIsRooted()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var rootedPath = Path.GetPathRoot(rootPath)!;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => new StoragePathPolicy(rootPath, rootedPath));

        Assert.Contains("상대 경로", exception.Message);
    }
}
