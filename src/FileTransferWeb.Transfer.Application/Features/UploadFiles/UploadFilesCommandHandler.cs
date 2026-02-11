using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Policies;
using FileTransferWeb.Transfer.Domain.Ports;
using MediatR;

namespace FileTransferWeb.Transfer.Application.Features.UploadFiles;

public sealed class UploadFilesCommandHandler(
    ITransferTargetFileNameReader targetFileNameReader,
    ITransferFileStore transferFileStore)
    : IRequestHandler<UploadFilesCommand, UploadFilesResult>
{
    private readonly ITransferTargetFileNameReader _targetFileNameReader = targetFileNameReader;
    private readonly ITransferFileStore _transferFileStore = transferFileStore;

    public async Task<UploadFilesResult> Handle(
        UploadFilesCommand request,
        CancellationToken cancellationToken)
    {
        var fileNames = request.Files.Select(file => file.FileName).ToArray();
        var uploadJob = UploadJob.Create(request.TargetRelativePath, fileNames);

        var existingFileNames = await _targetFileNameReader.GetExistingFileNamesAsync(
            uploadJob.TargetPath,
            cancellationToken);

        var uploadFileNamePolicy = new UploadFileNamePolicy(existingFileNames);
        var plannedFiles = request.Files
            .Select(file => uploadFileNamePolicy.Reserve(file.FileName, file.SizeBytes, file.OpenReadStream))
            .ToArray();

        var fileResults = await _transferFileStore.SaveFilesAsync(
            uploadJob.TargetPath,
            plannedFiles,
            cancellationToken);

        uploadJob.Complete(fileResults);
        return UploadFilesResult.FromDomain(uploadJob);
    }
}
