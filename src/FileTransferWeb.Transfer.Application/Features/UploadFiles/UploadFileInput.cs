namespace FileTransferWeb.Transfer.Application.Features.UploadFiles;

public sealed record UploadFileInput(
    string FileName,
    long SizeBytes,
    Func<Stream> OpenReadStream);
