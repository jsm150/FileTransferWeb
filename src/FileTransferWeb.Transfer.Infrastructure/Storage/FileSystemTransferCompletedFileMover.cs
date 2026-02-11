using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Ports;
using FileTransferWeb.Transfer.Infrastructure.Tus;

namespace FileTransferWeb.Transfer.Infrastructure.Storage;

public sealed class FileSystemTransferCompletedFileMover(
    IStorageRootPathProvider storageRootPathProvider,
    FileSystemTusStoreFactory tusStoreFactory)
    : ITransferCompletedFileMover
{
    private readonly IStorageRootPathProvider _storageRootPathProvider = storageRootPathProvider;
    private readonly FileSystemTusStoreFactory _tusStoreFactory = tusStoreFactory;

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

        await store.DeleteFileAsync(uploadId, cancellationToken);

        return string.IsNullOrEmpty(pathPolicy.CurrentRelativePath)
            ? safeStoredFileName
            : $"{pathPolicy.CurrentRelativePath}/{safeStoredFileName}";
    }
}
