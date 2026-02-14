using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Ports;
using FileTransferWeb.Transfer.Infrastructure.Tus;
using tusdotnet.Stores;

namespace FileTransferWeb.Transfer.Infrastructure.Storage;

public sealed class FileSystemTransferCompletedFileMover : ITransferCompletedFileMover
{
    private readonly IStorageRootPathProvider _storageRootPathProvider;
    private readonly FileSystemTusStoreFactory _tusStoreFactory;
    private readonly Action<string, string, bool> _moveFile;

    public FileSystemTransferCompletedFileMover(
        IStorageRootPathProvider storageRootPathProvider,
        FileSystemTusStoreFactory tusStoreFactory)
        : this(storageRootPathProvider, tusStoreFactory, File.Move)
    {
    }

    public FileSystemTransferCompletedFileMover(
        IStorageRootPathProvider storageRootPathProvider,
        FileSystemTusStoreFactory tusStoreFactory,
        Action<string, string, bool> moveFile)
    {
        _storageRootPathProvider = storageRootPathProvider;
        _tusStoreFactory = tusStoreFactory;
        _moveFile = moveFile;
    }

    public async ValueTask<string> MoveCompletedFileAsync(
        string uploadId,
        string targetRelativePath,
        string storedFileName,
        CancellationToken cancellationToken)
    {
        var uploadRoot = _storageRootPathProvider.GetUploadRootPath();
        var pathPolicy = new StoragePathPolicy(uploadRoot, targetRelativePath);

        Directory.CreateDirectory(pathPolicy.FullCurrentPath);

        var safeStoredFileName = Path.GetFileName(storedFileName).Trim();
        if (string.IsNullOrWhiteSpace(safeStoredFileName))
        {
            throw new InvalidOperationException("저장 파일 이름이 올바르지 않습니다.");
        }

        var destinationFullPath = Path.Combine(pathPolicy.FullCurrentPath, safeStoredFileName);

        var store = _tusStoreFactory.CreateStore();
        var sourceFullPath = Path.Combine(_tusStoreFactory.GetTempRootPath(), uploadId);

        if (!File.Exists(sourceFullPath))
        {
            throw new InvalidOperationException("업로드 임시 파일을 찾을 수 없습니다.");
        }

        try
        {
            _moveFile(sourceFullPath, destinationFullPath, false);
            await DeleteTusUploadArtifactsAsync(
                store,
                uploadId,
                cancellationToken,
                ignoreIoExceptions: true);
        }
        catch (Exception ex) when (ex is IOException
                                   or UnauthorizedAccessException
                                   or DirectoryNotFoundException
                                   or PathTooLongException
                                   or NotSupportedException)
        {
            await CopyFromTusStoreAsync(store, uploadId, destinationFullPath, cancellationToken);
            await DeleteTusUploadArtifactsAsync(
                store,
                uploadId,
                cancellationToken,
                ignoreIoExceptions: false);
        }

        return string.IsNullOrEmpty(pathPolicy.CurrentRelativePath)
            ? safeStoredFileName
            : $"{pathPolicy.CurrentRelativePath}/{safeStoredFileName}";
    }

    private static async ValueTask CopyFromTusStoreAsync(
        TusDiskStore store,
        string uploadId,
        string destinationFullPath,
        CancellationToken cancellationToken)
    {
        var tusFile = await store.GetFileAsync(uploadId, cancellationToken);
        if (tusFile is null)
        {
            throw new InvalidOperationException("업로드 임시 파일을 찾을 수 없습니다.");
        }

        await using (var sourceStream = await tusFile.GetContentAsync(cancellationToken))
        await using (var destinationStream = new FileStream(
                         destinationFullPath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 1024 * 80,
                         useAsync: true))
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }
    }

    private static async ValueTask DeleteTusUploadArtifactsAsync(
        TusDiskStore store,
        string uploadId,
        CancellationToken cancellationToken,
        bool ignoreIoExceptions)
    {
        try
        {
            await store.DeleteFileAsync(uploadId, cancellationToken);
        }
        catch (IOException) when (ignoreIoExceptions)
        {
            // 최종 파일은 이미 저장됐으므로 sidecar 정리 중 I/O 예외는 무시한다.
        }
    }
}
