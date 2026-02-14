namespace FileTransferWeb.Transfer.Infrastructure.Batch;

public sealed class TransferBatchMemoryOptions
{
    public const string SectionName = "Transfer:Batch";

    public int SlidingTtlMinutes { get; set; } = 1440;

    public int CleanupIntervalSeconds { get; set; } = 300;
}
