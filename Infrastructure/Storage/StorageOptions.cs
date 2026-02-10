namespace FileTransferWeb.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string UploadRoot { get; set; } = string.Empty;
}
