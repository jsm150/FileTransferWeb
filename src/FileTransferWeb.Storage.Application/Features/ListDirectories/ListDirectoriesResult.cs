using FileTransferWeb.Storage.Domain.Models;

namespace FileTransferWeb.Storage.Application.Features.ListDirectories;

public sealed record ListDirectoryItem(string Name, string RelativePath);

public sealed record ListDirectoriesResult(
    string CurrentPath,
    string? ParentPath,
    IReadOnlyList<ListDirectoryItem> Directories)
{
    public static ListDirectoriesResult FromDomain(DirectoryListing listing)
    {
        var items = listing.Directories
            .Select(directory => new ListDirectoryItem(directory.Name, directory.RelativePath))
            .ToArray();

        return new ListDirectoriesResult(listing.CurrentPath, listing.ParentPath, items);
    }
}
