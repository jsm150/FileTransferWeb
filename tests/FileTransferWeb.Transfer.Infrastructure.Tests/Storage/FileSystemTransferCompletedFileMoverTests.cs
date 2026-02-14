using System.Text;
using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Transfer.Infrastructure.Storage;
using FileTransferWeb.Transfer.Infrastructure.Tus;
using Microsoft.Extensions.Options;
using Xunit;

namespace FileTransferWeb.Transfer.Infrastructure.Tests.Storage;

public sealed class FileSystemTransferCompletedFileMoverTests
{
    [Fact(DisplayName = "Move 가능 환경에서는 임시 파일을 즉시 이동하여 저장한다")]
    public async Task MoveCompletedFileAsync_MovesFile_WhenMoveAvailable()
    {
        using var roots = new TemporaryRoots();
        var storeFactory = CreateStoreFactory(roots.TusTempRoot);
        var mover = CreateMover(roots.UploadRoot, storeFactory);

        var contentBytes = Encoding.UTF8.GetBytes("move-fast-path");
        var uploadId = await CreateTusUploadAsync(storeFactory, contentBytes);

        var relativePath = await mover.MoveCompletedFileAsync(
            uploadId,
            "images",
            "photo.txt",
            CancellationToken.None);

        var destinationPath = Path.Combine(roots.UploadRoot, "images", "photo.txt");
        Assert.Equal("images/photo.txt", relativePath);
        Assert.True(File.Exists(destinationPath));
        Assert.Equal("move-fast-path", await File.ReadAllTextAsync(destinationPath));
        Assert.False(File.Exists(Path.Combine(storeFactory.GetTempRootPath(), uploadId)));
    }

    [Fact(DisplayName = "임시 파일이 없으면 한글 예외를 반환한다")]
    public async Task MoveCompletedFileAsync_Throws_WhenTempFileIsMissing()
    {
        using var roots = new TemporaryRoots();
        var storeFactory = CreateStoreFactory(roots.TusTempRoot);
        var mover = CreateMover(roots.UploadRoot, storeFactory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mover.MoveCompletedFileAsync(
                    Guid.NewGuid().ToString("N"),
                    "images",
                    "photo.txt",
                    CancellationToken.None)
                .AsTask());

        Assert.Equal("업로드 임시 파일을 찾을 수 없습니다.", exception.Message);
    }

    [Fact(DisplayName = "목적지 파일이 이미 있으면 저장 실패를 유지한다")]
    public async Task MoveCompletedFileAsync_Throws_WhenDestinationAlreadyExists()
    {
        using var roots = new TemporaryRoots();
        var storeFactory = CreateStoreFactory(roots.TusTempRoot);
        var mover = CreateMover(roots.UploadRoot, storeFactory);

        var uploadId = await CreateTusUploadAsync(storeFactory, Encoding.UTF8.GetBytes("new-content"));
        var destinationDirectory = Path.Combine(roots.UploadRoot, "images");
        Directory.CreateDirectory(destinationDirectory);
        var destinationPath = Path.Combine(destinationDirectory, "photo.txt");
        await File.WriteAllTextAsync(destinationPath, "existing-content");

        await Assert.ThrowsAnyAsync<IOException>(
            () => mover.MoveCompletedFileAsync(
                    uploadId,
                    "images",
                    "photo.txt",
                    CancellationToken.None)
                .AsTask());

        Assert.Equal("existing-content", await File.ReadAllTextAsync(destinationPath));
    }

    [Fact(DisplayName = "Move 실패 시 fallback 복사 경로로 저장한다")]
    public async Task MoveCompletedFileAsync_CopiesFromStream_WhenMoveFails()
    {
        using var roots = new TemporaryRoots();
        var storeFactory = CreateStoreFactory(roots.TusTempRoot);
        var mover = CreateMover(
            roots.UploadRoot,
            storeFactory,
            (_, _, _) => throw new IOException("강제 이동 실패"));

        var contentBytes = Encoding.UTF8.GetBytes("copy-fallback");
        var uploadId = await CreateTusUploadAsync(storeFactory, contentBytes);

        var relativePath = await mover.MoveCompletedFileAsync(
            uploadId,
            "images",
            "fallback.txt",
            CancellationToken.None);

        var destinationPath = Path.Combine(roots.UploadRoot, "images", "fallback.txt");
        Assert.Equal("images/fallback.txt", relativePath);
        Assert.True(File.Exists(destinationPath));
        Assert.Equal("copy-fallback", await File.ReadAllTextAsync(destinationPath));
        Assert.False(File.Exists(Path.Combine(storeFactory.GetTempRootPath(), uploadId)));
    }

    private static FileSystemTransferCompletedFileMover CreateMover(
        string uploadRoot,
        FileSystemTusStoreFactory storeFactory,
        Action<string, string, bool>? moveFile = null)
    {
        var rootPathProvider = new StubStorageRootPathProvider(uploadRoot);
        return moveFile is null
            ? new FileSystemTransferCompletedFileMover(rootPathProvider, storeFactory)
            : new FileSystemTransferCompletedFileMover(rootPathProvider, storeFactory, moveFile);
    }

    private static FileSystemTusStoreFactory CreateStoreFactory(string tempRoot)
    {
        var options = Options.Create(
            new TransferTusOptions
            {
                TempRoot = tempRoot,
                ExpirationHours = 24,
                MaxSizeBytes = 1024L * 1024L * 1024L
            });
        return new FileSystemTusStoreFactory(options);
    }

    private static async Task<string> CreateTusUploadAsync(
        FileSystemTusStoreFactory storeFactory,
        byte[] contentBytes)
    {
        var store = storeFactory.CreateStore();
        var uploadId = await store.CreateFileAsync(
            contentBytes.LongLength,
            string.Empty,
            CancellationToken.None);
        await using var contentStream = new MemoryStream(contentBytes, writable: false);
        await store.AppendDataAsync(uploadId, contentStream, CancellationToken.None);
        return uploadId;
    }

    private sealed class StubStorageRootPathProvider(string uploadRootPath) : IStorageRootPathProvider
    {
        private readonly string _uploadRootPath = uploadRootPath;

        public string GetUploadRootPath()
        {
            return _uploadRootPath;
        }
    }

    private sealed class TemporaryRoots : IDisposable
    {
        private readonly string _baseRoot;

        public TemporaryRoots()
        {
            _baseRoot = Path.Combine(
                Path.GetTempPath(),
                $"ftw-transfer-mover-tests-{Guid.NewGuid():N}");
            UploadRoot = Path.Combine(_baseRoot, "upload-root");
            TusTempRoot = Path.Combine(_baseRoot, "tus-temp");
            Directory.CreateDirectory(UploadRoot);
            Directory.CreateDirectory(TusTempRoot);
        }

        public string UploadRoot { get; }

        public string TusTempRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(_baseRoot))
            {
                Directory.Delete(_baseRoot, recursive: true);
            }
        }
    }
}
