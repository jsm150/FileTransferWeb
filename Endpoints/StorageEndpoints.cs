using FileTransferWeb.Storage.Application.Features.ListDirectories;
using MediatR;

namespace FileTransferWeb.Endpoints;

public static class StorageEndpoints
{
    public static IEndpointRouteBuilder MapStorageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/storage/directories",
                async Task<IResult> (string? relativePath, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new ListDirectoriesQuery(relativePath), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("ListStorageDirectories")
            .WithTags("Storage");

        return app;
    }
}
