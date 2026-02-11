using FileTransferWeb.Transfer.Application.Features.UploadFiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FileTransferWeb.Endpoints;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/transfer/uploads",
                async Task<IResult> (
                    [FromForm] string? targetPath,
                    [FromForm] IFormFileCollection? files,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var uploadFiles = (files?.ToArray() ?? [])
                        .Select(file => new UploadFileInput(file.FileName, file.Length, file.OpenReadStream))
                        .ToArray();

                    var result = await sender.Send(
                        new UploadFilesCommand(targetPath ?? string.Empty, uploadFiles),
                        cancellationToken);

                    return TypedResults.Ok(result);
                })
            .DisableAntiforgery()
            .WithName("UploadTransferFiles")
            .WithTags("Transfer");

        return app;
    }
}
