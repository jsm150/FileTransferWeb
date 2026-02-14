using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.RegisterCompletedTusUpload;

public sealed record RegisterCompletedTusUploadCommand(
    string UploadId) : IRequest<Unit>;
