using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.CompleteTusUpload;

public sealed record CompleteTusUploadCommand(
    string UploadId) : IRequest;
