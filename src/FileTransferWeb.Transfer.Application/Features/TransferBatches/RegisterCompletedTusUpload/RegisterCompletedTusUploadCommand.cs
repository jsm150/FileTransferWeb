using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.TransferBatches.RegisterCompletedTusUpload;

public sealed record RegisterCompletedTusUploadCommand(
    Guid BatchId,
    string UploadId) : IRequest<RegisterCompletedTusUploadResult>;
