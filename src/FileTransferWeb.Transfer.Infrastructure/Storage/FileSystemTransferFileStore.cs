using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Policies;
using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Ports;

namespace FileTransferWeb.Transfer.Infrastructure.Storage;

public sealed class FileSystemTransferFileStore(IStorageRootPathProvider storageRootPathProvider)
    : ITransferFileStore
{
    private readonly IStorageRootPathProvider _storageRootPathProvider = storageRootPathProvider;

    public async ValueTask<IReadOnlyList<UploadFileResult>> SaveFilesAsync(
        string targetRelativePath,
        IReadOnlyList<UploadFilePlanItem> plannedFiles,
        CancellationToken cancellationToken)
    {
        var uploadRoot = _storageRootPathProvider.GetUploadRootPath();
        var pathPolicy = new StoragePathPolicy(uploadRoot, targetRelativePath);

        Directory.CreateDirectory(pathPolicy.FullCurrentPath);

        var fileResults = new List<UploadFileResult>(plannedFiles.Count);

        foreach (var plannedFile in plannedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fullDestinationPath = Path.Combine(pathPolicy.FullCurrentPath, plannedFile.StoredFileName);

                await using var sourceStream = plannedFile.OpenReadStream();
                await using var destinationStream = new FileStream(
                    fullDestinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024 * 80,
                    useAsync: true);

                await sourceStream.CopyToAsync(destinationStream, cancellationToken);

                var relativePath = CombineRelativePath(pathPolicy.CurrentRelativePath, plannedFile.StoredFileName);
                fileResults.Add(
                    UploadFileResult.Succeeded(
                        plannedFile.OriginalFileName,
                        plannedFile.StoredFileName,
                        relativePath,
                        plannedFile.SizeBytes));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or NotSupportedException
                                       or PathTooLongException)
            {
                fileResults.Add(
                    UploadFileResult.Failed(
                        plannedFile.OriginalFileName,
                        "파일 저장에 실패했습니다.",
                        plannedFile.SizeBytes));
            }
        }

        return fileResults;
    }

    private static string CombineRelativePath(string currentRelativePath, string fileName)
    {
        if (string.IsNullOrEmpty(currentRelativePath))
        {
            return fileName;
        }

        return $"{currentRelativePath}/{fileName}";
    }
}
