using FileTransferWeb.Storage.Application.Abstractions;

namespace FileTransferWeb.Storage.Infrastructure.Storage;

public sealed class FileSystemStorageDirectoryReader : IStorageDirectoryReader
{
    public ValueTask<IReadOnlyList<string>> GetDirectoryNamesAsync(string absolutePath, CancellationToken cancellationToken)
    {
        var directoryNames = Directory
            .EnumerateDirectories(absolutePath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<string>>(directoryNames);
    }
}
