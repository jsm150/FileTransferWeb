using FileTransferWeb.Storage.Domain.Exceptions;
using FileTransferWeb.Storage.Domain.Policies;

namespace FileTransferWeb.Storage.Domain.Models;

public sealed class DirectoryListing
{
    public string CurrentPath { get; }
    public string? ParentPath { get; }
    public IReadOnlyList<DirectoryEntry> Directories { get; }

    public DirectoryListing(StoragePathPolicy policy, IReadOnlyList<string> directoryNames)
    {
        if (policy is null)
        {
            throw new StorageDomainException("디렉터리 정책이 비어 있습니다.");
        }

        if (directoryNames is null)
        {
            throw new StorageDomainException("디렉터리 목록이 비어 있습니다.");
        }

        var orderedNames = directoryNames
            .Select(ValidateDirectoryName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        CurrentPath = policy.CurrentRelativePath;
        ParentPath = policy.ParentRelativePath;
        Directories = orderedNames
            .Select(name => new DirectoryEntry(name, BuildChildRelativePath(CurrentPath, name)))
            .ToArray();
    }

    private static string ValidateDirectoryName(string? directoryName)
    {
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            throw new StorageDomainException("디렉터리 이름이 비어 있습니다.");
        }

        return directoryName.Trim();
    }

    private static string BuildChildRelativePath(string currentRelativePath, string childName)
    {
        var normalizedChildName = childName
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        if (string.IsNullOrEmpty(currentRelativePath))
        {
            return normalizedChildName;
        }

        return $"{currentRelativePath}/{normalizedChildName}";
    }
}
