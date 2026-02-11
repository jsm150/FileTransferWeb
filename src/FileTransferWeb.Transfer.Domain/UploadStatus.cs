namespace FileTransferWeb.Transfer.Domain;

public enum UploadStatus
{
    Pending = 0,
    Completed = 1,
    PartiallyCompleted = 2,
    Failed = 3
}
