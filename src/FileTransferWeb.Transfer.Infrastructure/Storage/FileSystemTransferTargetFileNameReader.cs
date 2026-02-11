using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Ports;

namespace FileTransferWeb.Transfer.Infrastructure.Storage;

public sealed class FileSystemTransferTargetFileNameReader(IStorageRootPathProvider storageRootPathProvider)
    : ITransferTargetFileNameReader
{
    private readonly IStorageRootPathProvider _storageRootPathProvider = storageRootPathProvider;

    public ValueTask<IReadOnlyList<string>> GetExistingFileNamesAsync(
        string targetRelativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var uploadRoot = _storageRootPathProvider.GetUploadRootPath();
        var pathPolicy = new StoragePathPolicy(uploadRoot, targetRelativePath);

        if (!Directory.Exists(pathPolicy.FullCurrentPath))
        {
            return ValueTask.FromResult<IReadOnlyList<string>>([]);
        }

        var fileNames = Directory.EnumerateFiles(pathPolicy.FullCurrentPath)
            .Select(Path.GetFileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Cast<string>()
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<string>>(fileNames);
    }
}
