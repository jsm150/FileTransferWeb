using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Ports;

namespace FileTransferWeb.Transfer.Infrastructure.Tus;

public sealed class TransferUploadCreateValidator(
    IStorageRootPathProvider storageRootPathProvider)
    : ITransferUploadCreateValidator
{
    private readonly IStorageRootPathProvider _storageRootPathProvider = storageRootPathProvider;

    public ValueTask ValidateAsync(
        string targetPath,
        string fileName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new TransferDomainException("파일 이름 메타데이터가 비어 있습니다.");
        }

        var uploadRoot = _storageRootPathProvider.GetUploadRootPath();
        _ = new StoragePathPolicy(uploadRoot, targetPath);

        return ValueTask.CompletedTask;
    }
}
