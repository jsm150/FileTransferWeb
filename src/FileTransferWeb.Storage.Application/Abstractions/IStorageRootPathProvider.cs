namespace FileTransferWeb.Storage.Application.Abstractions;

public interface IStorageRootPathProvider
{
    string GetUploadRootPath();
}
