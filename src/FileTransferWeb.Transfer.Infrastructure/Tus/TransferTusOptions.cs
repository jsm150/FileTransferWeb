namespace FileTransferWeb.Transfer.Infrastructure.Tus;

public sealed class TransferTusOptions
{
    public const string SectionName = "Transfer:Tus";

    public string TempRoot { get; set; } = string.Empty;

    public int ExpirationHours { get; set; } = 24;

    public long MaxSizeBytes { get; set; } = 1024L * 1024L * 1024L * 10L;
}
