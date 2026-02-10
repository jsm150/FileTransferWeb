using FileTransferWeb.Storage.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace FileTransferWeb.Storage.Infrastructure.Storage;

public sealed class StorageRootPathProvider(IOptions<StorageOptions> storageOptions) : IStorageRootPathProvider
{
    private readonly StorageOptions _storageOptions = storageOptions.Value;

    public string GetUploadRootPath() => _storageOptions.UploadRoot;
}
