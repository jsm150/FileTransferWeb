namespace FileTransferWeb.Transfer.Domain.Models;

public enum TransferBatchStatus
{
    Collecting = 0,
    Completed = 1,
    PartiallyCompleted = 2,
    Failed = 3
}
