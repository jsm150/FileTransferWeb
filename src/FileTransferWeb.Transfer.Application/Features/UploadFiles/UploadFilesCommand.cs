using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.UploadFiles;

public sealed record UploadFilesCommand(
    string TargetRelativePath,
    IReadOnlyList<UploadFileInput> Files) : IRequest<UploadFilesResult>;
