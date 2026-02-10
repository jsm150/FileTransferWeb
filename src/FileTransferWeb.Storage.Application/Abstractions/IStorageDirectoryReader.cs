namespace FileTransferWeb.Storage.Application.Abstractions;

public interface IStorageDirectoryReader
{
    ValueTask<IReadOnlyList<string>> GetDirectoryNamesAsync(string absolutePath, CancellationToken cancellationToken);
}
