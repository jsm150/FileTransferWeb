using FileTransferWeb.Domain.Shared;
using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Application.Features.ListDirectories;
using Xunit;

namespace FileTransferWeb.Storage.Application.Tests.Storage.Features.ListDirectories;

public class ListDirectoriesQueryHandlerTests
{
    [Fact(DisplayName = "핸들러는 정책의 현재 절대 경로로 디렉터리 조회를 수행한다")]
    public async Task Handle_RequestsDirectories_WithPolicyResolvedPath()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var rootPathProvider = new FakeStorageRootPathProvider(rootPath);
        var directoryReader = new FakeStorageDirectoryReader(["trip"]);
        var handler = new ListDirectoriesQueryHandler(rootPathProvider, directoryReader);

        var result = await handler.Handle(new ListDirectoriesQuery("images/2026"), CancellationToken.None);

        Assert.Equal("images/2026", result.CurrentPath);
        Assert.Equal("images", result.ParentPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(rootPath, "images/2026")), directoryReader.LastRequestedPath);
    }

    [Fact(DisplayName = "핸들러는 도메인 결과를 DTO로 매핑해 반환한다")]
    public async Task Handle_ReturnsMappedDto_FromDomainModel()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var rootPathProvider = new FakeStorageRootPathProvider(rootPath);
        var directoryReader = new FakeStorageDirectoryReader(["beta", "alpha"]);
        var handler = new ListDirectoriesQueryHandler(rootPathProvider, directoryReader);

        var result = await handler.Handle(new ListDirectoriesQuery(""), CancellationToken.None);

        Assert.Equal(string.Empty, result.CurrentPath);
        Assert.Null(result.ParentPath);
        Assert.Equal(2, result.Directories.Count);
        Assert.Equal("alpha", result.Directories[0].Name);
        Assert.Equal("alpha", result.Directories[0].RelativePath);
    }

    [Fact(DisplayName = "루트 밖 경로 요청 시 도메인 예외를 발생시킨다")]
    public async Task Handle_Throws_WhenPathTraversalIsRequested()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        var rootPathProvider = new FakeStorageRootPathProvider(rootPath);
        var directoryReader = new FakeStorageDirectoryReader(["ignored"]);
        var handler = new ListDirectoriesQueryHandler(rootPathProvider, directoryReader);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => handler.Handle(new ListDirectoriesQuery("../outside"), CancellationToken.None));
    }

    private sealed class FakeStorageRootPathProvider(string rootPath) : IStorageRootPathProvider
    {
        public string GetUploadRootPath() => rootPath;
    }

    private sealed class FakeStorageDirectoryReader(IReadOnlyList<string> directoryNames) : IStorageDirectoryReader
    {
        public string LastRequestedPath { get; private set; } = string.Empty;

        public ValueTask<IReadOnlyList<string>> GetDirectoryNamesAsync(string absolutePath, CancellationToken cancellationToken)
        {
            LastRequestedPath = absolutePath;
            return ValueTask.FromResult(directoryNames);
        }
    }
}
