using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Models;
using FileTransferWeb.Storage.Domain.Policies;
using MediatR;

namespace FileTransferWeb.Storage.Application.Features.ListDirectories;

public sealed class ListDirectoriesQueryHandler(
    IStorageRootPathProvider storageRootPathProvider,
    IStorageDirectoryReader storageDirectoryReader)
    : IRequestHandler<ListDirectoriesQuery, ListDirectoriesResult>
{
    private readonly IStorageRootPathProvider _storageRootPathProvider = storageRootPathProvider;
    private readonly IStorageDirectoryReader _storageDirectoryReader = storageDirectoryReader;

    public async Task<ListDirectoriesResult> Handle(
        ListDirectoriesQuery request,
        CancellationToken cancellationToken)
    {
        var uploadRoot = _storageRootPathProvider.GetUploadRootPath();
        var policy = new StoragePathPolicy(uploadRoot, request.RelativePath);

        var directoryNames = await _storageDirectoryReader.GetDirectoryNamesAsync(policy.FullCurrentPath, cancellationToken);
        var listing = new DirectoryListing(policy, directoryNames);

        return ListDirectoriesResult.FromDomain(listing);
    }
}
