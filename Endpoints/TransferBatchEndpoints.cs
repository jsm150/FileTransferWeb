using FileTransferWeb.Transfer.Application.Features.TransferBatches.CreateTransferBatch;
using FileTransferWeb.Transfer.Application.Features.TransferBatches.FinalizeTransferBatch;
using FileTransferWeb.Transfer.Application.Features.TransferBatches.GetTransferBatchStatus;
using MediatR;

namespace FileTransferWeb.Endpoints;

public static class TransferBatchEndpoints
{
    public static IEndpointRouteBuilder MapTransferBatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/transfer/batches",
                async Task<IResult> (CreateTransferBatchRequest request, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new CreateTransferBatchCommand(request.TargetPath, request.ExpectedFileCount),
                        cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("CreateTransferBatch")
            .WithTags("Transfer");

        app.MapGet(
                "/api/transfer/batches/{batchId:guid}",
                async Task<IResult> (Guid batchId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetTransferBatchStatusQuery(batchId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetTransferBatchStatus")
            .WithTags("Transfer");

        app.MapPost(
                "/api/transfer/batches/{batchId:guid}/complete",
                async Task<IResult> (Guid batchId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new FinalizeTransferBatchCommand(batchId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("FinalizeTransferBatch")
            .WithTags("Transfer");

        return app;
    }

    public sealed record CreateTransferBatchRequest(
        string TargetPath,
        int ExpectedFileCount);
}
