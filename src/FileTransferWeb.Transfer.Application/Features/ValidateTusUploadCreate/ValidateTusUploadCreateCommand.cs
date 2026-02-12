using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.ValidateTusUploadCreate;

public sealed record ValidateTusUploadCreateCommand(
    string TargetPath,
    string FileName) : IRequest;
