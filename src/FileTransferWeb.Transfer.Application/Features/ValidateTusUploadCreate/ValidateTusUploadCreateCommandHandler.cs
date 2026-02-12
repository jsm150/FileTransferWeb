using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.ValidateTusUploadCreate;

public sealed class ValidateTusUploadCreateCommandHandler(
    ITransferUploadCreateValidator validator)
    : IRequestHandler<ValidateTusUploadCreateCommand>
{
    private readonly ITransferUploadCreateValidator _validator = validator;

    public async Task Handle(ValidateTusUploadCreateCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAsync(request.TargetPath, request.FileName, cancellationToken);
    }
}
